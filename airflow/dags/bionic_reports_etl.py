from datetime import datetime, timedelta
from typing import Any

from airflow import DAG
from airflow.operators.python import PythonOperator
from airflow.operators.empty import EmptyOperator
from airflow.providers.postgres.hooks.postgres import PostgresHook
from airflow.providers.common.sql.operators.sql import SQLExecuteQueryOperator
from airflow.utils.task_group import TaskGroup
from airflow.models import Variable

import logging
import json

CLICKHOUSE_CONN_ID = 'clickhouse_default'
POSTGRES_SENSORS_CONN_ID = 'postgres_sensors'
POSTGRES_CRM_CONN_ID = 'postgres_crm'

default_args = {
    'owner': 'bionic_data_team',
    'depends_on_past': False,
    'email': ['data-alerts@bionicpro.com'],
    'email_on_failure': True,
    'email_on_retry': False,
    'retries': 3,
    'retry_delay': timedelta(minutes=5),
    'retry_exponential_backoff': True,
    'max_retry_delay': timedelta(minutes=30),
}


def extract_telemetry_data(**context) -> dict:
    logger = logging.getLogger(__name__)

    last_watermark = Variable.get(
        'telemetry_last_watermark',
        default_var='1970-01-01 00:00:00'
    )

    execution_date = context['execution_date']

    logger.info(f"Extracting telemetry data since {last_watermark}")

    pg_hook = PostgresHook(postgres_conn_id=POSTGRES_SENSORS_CONN_ID)


    query = """
        SELECT
            telemetry_id::text,
            prosthesis_id::text,
            user_id::text,
            event_timestamp,
            signal_strength,
            response_time_ms,
            battery_level,
            movement_type,
            grip_force,
            temperature
        FROM prosthesis_telemetry
        WHERE event_timestamp > %(last_watermark)s
          AND event_timestamp <= %(execution_date)s
        ORDER BY event_timestamp
        LIMIT 1000000
    """

    records = pg_hook.get_records(
        sql=query,
        parameters={
            'last_watermark': last_watermark,
            'execution_date': execution_date.isoformat()
        }
    )

    logger.info(f"Extracted {len(records)} telemetry records")

    context['ti'].xcom_push(key='telemetry_data', value=records)
    context['ti'].xcom_push(key='telemetry_count', value=len(records))

    if records:
        new_watermark = max(r[3] for r in records)
        Variable.set('telemetry_last_watermark', str(new_watermark))

    return {'records_count': len(records)}

def extract_crm_data(**context) -> dict:
    """
    Извлечение данных клиентов из CRM (PostgreSQL).
    Используется представление v_customer_prostheses_full.
    """
    logger = logging.getLogger(__name__)
    logger.info("Extracting CRM customer data")

    pg_hook = PostgresHook(postgres_conn_id=POSTGRES_CRM_CONN_ID)

    
    query = """
        SELECT
            customer_id::text,
            user_id::text,
            first_name,
            last_name,
            email,
            phone,
            prosthesis_id::text,
            prosthesis_model,
            prosthesis_serial_number,
            purchase_date,
            warranty_end_date,
            last_service_date,
            service_center_name
        FROM v_customer_prostheses_full
    """

    records = pg_hook.get_records(sql=query)

    logger.info(f"Extracted {len(records)} CRM customer records")

    context['ti'].xcom_push(key='crm_data', value=records)
    context['ti'].xcom_push(key='crm_count', value=len(records))

    return {'records_count': len(records)}


def load_telemetry_to_clickhouse(**context) -> dict:
    """
    Загрузка данных телеметрии в staging таблицу ClickHouse.
    """
    logger = logging.getLogger(__name__)

    telemetry_data = context['ti'].xcom_pull(
        task_ids='extract.extract_telemetry',
        key='telemetry_data'
    )

    if not telemetry_data:
        logger.info("No telemetry data to load")
        return {'loaded_count': 0}

    logger.info(f"Loading {len(telemetry_data)} telemetry records to ClickHouse")


    from clickhouse_driver import Client

    ch_client = Client(
        host=Variable.get('clickhouse_host', default_var='localhost'),
        port=int(Variable.get('clickhouse_port', default_var='9000')),
        user=Variable.get('clickhouse_user', default_var='default'),
        password=Variable.get('clickhouse_password', default_var=''),
        database='bionic_reports'
    )

    
    insert_query = """
        INSERT INTO stg_telemetry (
            telemetry_id, prosthesis_id, user_id, event_timestamp,
            signal_strength, response_time_ms, battery_level,
            movement_type, grip_force, temperature
        ) VALUES
    """

    ch_client.execute(insert_query, telemetry_data)

    logger.info(f"Successfully loaded {len(telemetry_data)} telemetry records")

    return {'loaded_count': len(telemetry_data)}


def load_crm_to_clickhouse(**context) -> dict:
    """
    Загрузка данных CRM в staging таблицу ClickHouse.
    Используется ReplacingMergeTree для дедупликации.
    """
    logger = logging.getLogger(__name__)

    crm_data = context['ti'].xcom_pull(
        task_ids='extract.extract_crm',
        key='crm_data'
    )

    if not crm_data:
        logger.info("No CRM data to load")
        return {'loaded_count': 0}

    logger.info(f"Loading {len(crm_data)} CRM records to ClickHouse")

    from clickhouse_driver import Client

    ch_client = Client(
        host=Variable.get('clickhouse_host', default_var='localhost'),
        port=int(Variable.get('clickhouse_port', default_var='9000')),
        user=Variable.get('clickhouse_user', default_var='default'),
        password=Variable.get('clickhouse_password', default_var=''),
        database='bionic_reports'
    )

    processed_data = []
    for row in crm_data:
        processed_row = list(row)
        for idx in [2, 3, 4, 5, 7, 8, 12]:
            if processed_row[idx] is None:
                processed_row[idx] = ''
        processed_data.append(tuple(processed_row))

    insert_query = """
        INSERT INTO stg_crm_customers (
            customer_id, user_id, first_name, last_name, email, phone,
            prosthesis_id, prosthesis_model, prosthesis_serial_number,
            purchase_date, warranty_end_date, last_service_date, service_center
        ) VALUES
    """

    ch_client.execute(insert_query, processed_data)

    logger.info(f"Successfully loaded {len(crm_data)} CRM records")

    return {'loaded_count': len(crm_data)}


def refresh_data_mart(**context) -> dict:
    logger = logging.getLogger(__name__)
    logger.info("Refreshing data mart")

    from clickhouse_driver import Client

    ch_client = Client(
        host=Variable.get('clickhouse_host', default_var='localhost'),
        port=int(Variable.get('clickhouse_port', default_var='9000')),
        user=Variable.get('clickhouse_user', default_var='default'),
        password=Variable.get('clickhouse_password', default_var=''),
        database='bionic_reports'
    )


    ch_client.execute("OPTIMIZE TABLE stg_crm_customers FINAL")
    logger.info("Optimized stg_crm_customers")

    ch_client.execute("OPTIMIZE TABLE dm_user_prosthesis_reports FINAL")
    logger.info("Optimized dm_user_prosthesis_reports")

    result = ch_client.execute(
        "SELECT count() FROM dm_user_prosthesis_reports"
    )
    total_reports = result[0][0]

    logger.info(f"Data mart refreshed. Total reports: {total_reports}")

    return {'total_reports': total_reports}


def validate_data_quality(**context) -> dict:
    logger = logging.getLogger(__name__)
    logger.info("Validating data quality")

    from clickhouse_driver import Client

    ch_client = Client(
        host=Variable.get('clickhouse_host', default_var='localhost'),
        port=int(Variable.get('clickhouse_port', default_var='9000')),
        user=Variable.get('clickhouse_user', default_var='default'),
        password=Variable.get('clickhouse_password', default_var=''),
        database='bionic_reports'
    )

    checks = {
        'null_user_ids': """
            SELECT count() FROM dm_user_prosthesis_reports
            WHERE user_id = toUUID('00000000-0000-0000-0000-000000000000')
        """,
        'invalid_health_scores': """
            SELECT count() FROM dm_user_prosthesis_reports
            WHERE health_score < 0 OR health_score > 100
        """,
        'future_dates': """
            SELECT count() FROM dm_user_prosthesis_reports
            WHERE report_date > today()
        """,
        'orphan_records': """
            SELECT count() FROM dm_user_prosthesis_reports r
            LEFT JOIN stg_crm_customers c ON c.user_id = r.user_id
            WHERE c.user_id IS NULL
        """
    }

    issues = {}
    for check_name, query in checks.items():
        result = ch_client.execute(query)
        count = result[0][0]
        if count > 0:
            issues[check_name] = count
            logger.warning(f"Data quality issue: {check_name} = {count}")

    if issues:
        logger.warning(f"Data quality issues found: {issues}")
    else:
        logger.info("All data quality checks passed")

    return {'issues': issues, 'passed': len(issues) == 0}


def send_completion_notification(**context) -> None:
    """
    Отправка уведомления о завершении ETL.
    """
    logger = logging.getLogger(__name__)

    telemetry_count = context['ti'].xcom_pull(
        task_ids='extract.extract_telemetry',
        key='telemetry_count'
    ) or 0

    crm_count = context['ti'].xcom_pull(
        task_ids='extract.extract_crm',
        key='crm_count'
    ) or 0

    execution_date = context['execution_date']

    message = f"""
    BionicPRO ETL Pipeline Completed

    Execution Date: {execution_date}
    Telemetry Records Processed: {telemetry_count}
    CRM Records Processed: {crm_count}

    Dashboard: http://airflow.bionicpro.internal/dags/bionic_reports_etl
    """

    logger.info(message)

with DAG(
    dag_id='bionic_reports_etl',
    default_args=default_args,
    description='ETL pipeline for BionicPRO user reports data mart',
    schedule_interval='0 * * * *',
    start_date=datetime(2025, 1, 1),
    catchup=False,
    max_active_runs=1,
    tags=['bionic', 'etl', 'reports', 'clickhouse'],
    doc_md=__doc__
) as dag:


    start = EmptyOperator(task_id='start')

    with TaskGroup(group_id='extract') as extract_group:
        extract_telemetry = PythonOperator(
            task_id='extract_telemetry',
            python_callable=extract_telemetry_data,
            provide_context=True
        )

        extract_crm = PythonOperator(
            task_id='extract_crm',
            python_callable=extract_crm_data,
            provide_context=True
        )

    with TaskGroup(group_id='load') as load_group:
        load_telemetry = PythonOperator(
            task_id='load_telemetry',
            python_callable=load_telemetry_to_clickhouse,
            provide_context=True
        )

        load_crm = PythonOperator(
            task_id='load_crm',
            python_callable=load_crm_to_clickhouse,
            provide_context=True
        )


    refresh_mart = PythonOperator(
        task_id='refresh_data_mart',
        python_callable=refresh_data_mart,
        provide_context=True
    )

    end = EmptyOperator(task_id='end')


    start >> extract_group

    extract_telemetry >> load_telemetry
    extract_crm >> load_crm

    [load_telemetry, load_crm] >> refresh_mart >> end


with DAG(
    dag_id='bionic_reports_daily_aggregation',
    default_args=default_args,
    description='Daily full aggregation for BionicPRO reports',
    schedule_interval='0 2 * * *', 
    start_date=datetime(2025, 1, 1),
    catchup=False,
    max_active_runs=1,
    tags=['bionic', 'etl', 'reports', 'daily']
) as daily_dag:

    def run_daily_aggregation(**context):
        logger = logging.getLogger(__name__)
        execution_date = context['execution_date']
        report_date = (execution_date - timedelta(days=1)).strftime('%Y-%m-%d')

        logger.info(f"Running daily aggregation for {report_date}")

        from clickhouse_driver import Client

        ch_client = Client(
            host=Variable.get('clickhouse_host', default_var='localhost'),
            port=int(Variable.get('clickhouse_port', default_var='9000')),
            user=Variable.get('clickhouse_user', default_var='default'),
            password=Variable.get('clickhouse_password', default_var=''),
            database='bionic_reports'
        )

        aggregation_query = f"""
            INSERT INTO dm_user_prosthesis_reports
            SELECT
                t.user_id,
                t.prosthesis_id,
                toDate('{report_date}') as report_date,

                anyLast(c.first_name || ' ' || c.last_name) as user_full_name,
                anyLast(c.email) as user_email,
                anyLast(c.prosthesis_model) as prosthesis_model,
                anyLast(c.prosthesis_serial_number) as prosthesis_serial_number,
                anyLast(c.purchase_date) as purchase_date,
                anyLast(
                    CASE
                        WHEN c.warranty_end_date >= today() THEN 'active'
                        ELSE 'expired'
                    END
                ) as warranty_status,

                count() / 3600.0 as total_usage_hours,
                count() as total_movements,

                countIf(t.movement_type = 'grip') as grip_movements,
                countIf(t.movement_type = 'release') as release_movements,
                countIf(t.movement_type = 'rotation') as rotation_movements,
                countIf(t.movement_type = 'fine_motor') as fine_motor_movements,

                avg(t.signal_strength) as avg_signal_strength,
                min(t.signal_strength) as min_signal_strength,
                max(t.signal_strength) as max_signal_strength,

                avg(t.response_time_ms) as avg_response_time_ms,
                quantile(0.95)(t.response_time_ms) as p95_response_time_ms,

                avg(t.grip_force) as avg_grip_force,
                max(t.grip_force) as max_grip_force,

                avg(t.battery_level) as avg_battery_level,
                min(t.battery_level) as min_battery_level,
                toUInt16(countIf(t.battery_level < 20)) as battery_cycles,

                avg(t.temperature) as avg_temperature,
                max(t.temperature) as max_temperature,
                toUInt16(countIf(t.temperature > 45)) as overheating_events,

                toUInt16(countIf(t.signal_strength < 0.3 OR t.response_time_ms > 500)) as anomaly_count,
                toUInt16(countIf(t.battery_level < 10 OR t.temperature > 40)) as warning_count,

                greatest(0, least(100,
                    100.0
                    - (100 - avg(t.signal_strength) * 100) * 0.3
                    - least(50, avg(t.response_time_ms) / 10) * 0.3
                    - countIf(t.signal_strength < 0.3 OR t.response_time_ms > 500) * 0.5
                    - countIf(t.temperature > 45) * 2
                )) as health_score,

                now() as created_at,
                now() as updated_at

            FROM stg_telemetry t
            LEFT JOIN stg_crm_customers c
                ON t.user_id = c.user_id AND t.prosthesis_id = c.prosthesis_id
            WHERE toDate(t.event_timestamp) = toDate('{report_date}')
            GROUP BY t.user_id, t.prosthesis_id
        """

        ch_client.execute(aggregation_query)


        ch_client.execute("OPTIMIZE TABLE dm_user_prosthesis_reports FINAL")

        result = ch_client.execute(f"""
            SELECT count() FROM dm_user_prosthesis_reports
            WHERE report_date = toDate('{report_date}')
        """)

        records_count = result[0][0]
        logger.info(f"Daily aggregation complete. Records for {report_date}: {records_count}")

        return {'report_date': report_date, 'records_count': records_count}

    def cleanup_old_staging_data(**context):
        logger = logging.getLogger(__name__)
        logger.info("Cleaning up old staging data")

        from clickhouse_driver import Client

        ch_client = Client(
            host=Variable.get('clickhouse_host', default_var='localhost'),
            port=int(Variable.get('clickhouse_port', default_var='9000')),
            user=Variable.get('clickhouse_user', default_var='default'),
            password=Variable.get('clickhouse_password', default_var=''),
            database='bionic_reports'
        )

        ch_client.execute("""
            ALTER TABLE stg_telemetry
            DELETE WHERE event_timestamp < now() - INTERVAL 90 DAY
        """)

        logger.info("Staging data cleanup complete")

    start_daily = EmptyOperator(task_id='start')

    daily_agg = PythonOperator(
        task_id='daily_aggregation',
        python_callable=run_daily_aggregation,
        provide_context=True
    )

    cleanup = PythonOperator(
        task_id='cleanup_staging',
        python_callable=cleanup_old_staging_data,
        provide_context=True
    )

    end_daily = EmptyOperator(task_id='end')

    start_daily >> daily_agg >> cleanup >> end_daily
