CREATE DATABASE IF NOT EXISTS bionic_reports;


-- Staging таблицы

CREATE TABLE IF NOT EXISTS bionic_reports.stg_telemetry (
    telemetry_id UUID,
    prosthesis_id UUID,
    user_id UUID,
    event_timestamp DateTime64(3),


    signal_strength Float32,
    response_time_ms Float32,
    battery_level UInt8,
    movement_type String,
    grip_force Float32,
    temperature Float32,

    _loaded_at DateTime DEFAULT now(),
    _source String DEFAULT 'postgresql'
) ENGINE = MergeTree()
PARTITION BY toYYYYMM(event_timestamp)
ORDER BY (user_id, prosthesis_id, event_timestamp)
TTL toDateTime(event_timestamp) + INTERVAL 90 DAY;

-- Данные клиентов из CRM
CREATE TABLE IF NOT EXISTS bionic_reports.stg_crm_customers (
    customer_id UUID,
    user_id UUID,

    first_name String,
    last_name String,
    email String,
    phone String,

    prosthesis_id UUID,
    prosthesis_model String,
    prosthesis_serial_number String,
    purchase_date Date,
    warranty_end_date Date,

    last_service_date Nullable(Date),
    service_center String,

    _loaded_at DateTime DEFAULT now(),
    _source String DEFAULT 'crm_oracle'
) ENGINE = ReplacingMergeTree(_loaded_at)
ORDER BY (user_id, prosthesis_id);


-- Основная витрина
CREATE TABLE IF NOT EXISTS bionic_reports.dm_user_prosthesis_reports (

    user_id UUID,
    prosthesis_id UUID,
    report_date Date,

    user_full_name String,
    user_email String,

    prosthesis_model String,
    prosthesis_serial_number String,
    purchase_date Date,
    warranty_status String,  -- 'active', 'expired', 'extended'

    total_usage_hours Float32,
    total_movements UInt32,

    grip_movements UInt32,
    release_movements UInt32,
    rotation_movements UInt32,
    fine_motor_movements UInt32,

    avg_signal_strength Float32,
    min_signal_strength Float32,
    max_signal_strength Float32,

    avg_response_time_ms Float32,
    p95_response_time_ms Float32,

    avg_grip_force Float32,
    max_grip_force Float32,

    avg_battery_level Float32,
    min_battery_level Float32,
    battery_cycles UInt16,

    avg_temperature Float32,
    max_temperature Float32,
    overheating_events UInt16,

    anomaly_count UInt16,
    warning_count UInt16,

    health_score Float32,

    created_at DateTime DEFAULT now(),
    updated_at DateTime DEFAULT now()

) ENGINE = ReplacingMergeTree(updated_at)
PARTITION BY toYYYYMM(report_date)
ORDER BY (user_id, report_date, prosthesis_id)
TTL report_date + INTERVAL 2 YEAR;


-- Проекция для быстрого поиска по user_id
ALTER TABLE bionic_reports.dm_user_prosthesis_reports
ADD PROJECTION IF NOT EXISTS prj_by_user (
    SELECT *
    ORDER BY user_id, report_date
);

-- Проекция для аналитики по моделям протезов
ALTER TABLE bionic_reports.dm_user_prosthesis_reports
ADD PROJECTION IF NOT EXISTS prj_by_model (
    SELECT
        prosthesis_model,
        report_date,
        avg(health_score) as avg_health_score,
        avg(avg_response_time_ms) as avg_response_time,
        count() as reports_count
    GROUP BY prosthesis_model, report_date
);

CREATE MATERIALIZED VIEW IF NOT EXISTS bionic_reports.mv_daily_telemetry_agg
TO bionic_reports.dm_user_prosthesis_reports
AS
SELECT
    t.user_id,
    t.prosthesis_id,
    toDate(t.event_timestamp) as report_date,

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

    -- аномалии
    toUInt16(countIf(t.signal_strength < 0.3 OR t.response_time_ms > 500)) as anomaly_count,
    toUInt16(countIf(t.battery_level < 10 OR t.temperature > 40)) as warning_count,

    -- Health Score
    greatest(0, least(100,
        100.0
        - (100 - avg(t.signal_strength) * 100) * 0.3
        - least(50, avg(t.response_time_ms) / 10) * 0.3
        - countIf(t.signal_strength < 0.3 OR t.response_time_ms > 500) * 0.5
        - countIf(t.temperature > 45) * 2
    )) as health_score,

    now() as created_at,
    now() as updated_at

FROM bionic_reports.stg_telemetry t
LEFT JOIN bionic_reports.stg_crm_customers c
    ON t.user_id = c.user_id AND t.prosthesis_id = c.prosthesis_id
GROUP BY t.user_id, t.prosthesis_id, toDate(t.event_timestamp);

-- Представление для получения последнего отчёта пользователя
CREATE VIEW IF NOT EXISTS bionic_reports.v_latest_user_reports AS
SELECT *
FROM bionic_reports.dm_user_prosthesis_reports
WHERE (user_id, prosthesis_id, report_date) IN (
    SELECT user_id, prosthesis_id, max(report_date)
    FROM bionic_reports.dm_user_prosthesis_reports
    GROUP BY user_id, prosthesis_id
);

-- Представление для трендов за последние 30 дней
CREATE VIEW IF NOT EXISTS bionic_reports.v_user_trends_30d AS
SELECT
    user_id,
    prosthesis_id,
    user_full_name,
    prosthesis_model,

    avg(health_score) as avg_health_score_30d,
    avg(total_usage_hours) as avg_daily_usage_30d,
    avg(avg_response_time_ms) as avg_response_time_30d,

    avgIf(health_score, report_date >= today() - 7) -
    avgIf(health_score, report_date < today() - 7) as health_score_trend,

    sum(anomaly_count) as total_anomalies_30d,
    sum(warning_count) as total_warnings_30d,

    count() as days_with_data

FROM bionic_reports.dm_user_prosthesis_reports
WHERE report_date >= today() - 30
GROUP BY user_id, prosthesis_id, user_full_name, prosthesis_model;
