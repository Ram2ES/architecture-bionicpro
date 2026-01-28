-- БД эмулирует данные, приходящие с протезов

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Таблица телеметрии протезов
CREATE TABLE IF NOT EXISTS prosthesis_telemetry (
    telemetry_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    prosthesis_id UUID NOT NULL,
    user_id UUID NOT NULL,
    event_timestamp TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),

    -- Данные датчиков
    signal_strength FLOAT NOT NULL CHECK (signal_strength >= 0 AND signal_strength <= 1),
    response_time_ms FLOAT NOT NULL CHECK (response_time_ms >= 0),
    battery_level SMALLINT NOT NULL CHECK (battery_level >= 0 AND battery_level <= 100),
    movement_type VARCHAR(50) NOT NULL,
    grip_force FLOAT CHECK (grip_force >= 0),
    temperature FLOAT CHECK (temperature >= -20 AND temperature <= 80),

    -- Метаданные
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Заполнение таблицы тестовыми данными
-- Пользователь 1: a1111111-1111-1111-1111-111111111111, протез 01111111-1111-1111-1111-111111111111
INSERT INTO prosthesis_telemetry (
    prosthesis_id, user_id, event_timestamp,
    signal_strength, response_time_ms, battery_level, movement_type, grip_force, temperature
)
SELECT
    '01111111-1111-1111-1111-111111111111'::uuid,
    'a1111111-1111-1111-1111-111111111111'::uuid,
    (NOW() - (j || ' days')::INTERVAL) + (random() * INTERVAL '24 hours'),
    0.7 + random() * 0.3,
    50 + random() * 100,
    (50 + random() * 50)::SMALLINT,
    (ARRAY['grip', 'release', 'rotation', 'fine_motor'])[1 + floor(random() * 4)::INT],
    10 + random() * 40,
    35 + random() * 10
FROM generate_series(0, 6) j,
     generate_series(1, 100);

-- Пользователь 2: b2222222-2222-2222-2222-222222222222, протез 02222222-2222-2222-2222-222222222222
INSERT INTO prosthesis_telemetry (
    prosthesis_id, user_id, event_timestamp,
    signal_strength, response_time_ms, battery_level, movement_type, grip_force, temperature
)
SELECT
    '02222222-2222-2222-2222-222222222222'::uuid,
    'b2222222-2222-2222-2222-222222222222'::uuid,
    (NOW() - (j || ' days')::INTERVAL) + (random() * INTERVAL '24 hours'),
    0.7 + random() * 0.3,
    50 + random() * 100,
    (50 + random() * 50)::SMALLINT,
    (ARRAY['grip', 'release', 'rotation', 'fine_motor'])[1 + floor(random() * 4)::INT],
    10 + random() * 40,
    35 + random() * 10
FROM generate_series(0, 6) j,
     generate_series(1, 100);

-- Пользователь 3: c3333333-3333-3333-3333-333333333333, протез 03333333-3333-3333-3333-333333333333
INSERT INTO prosthesis_telemetry (
    prosthesis_id, user_id, event_timestamp,
    signal_strength, response_time_ms, battery_level, movement_type, grip_force, temperature
)
SELECT
    '03333333-3333-3333-3333-333333333333'::uuid,
    'c3333333-3333-3333-3333-333333333333'::uuid,
    (NOW() - (j || ' days')::INTERVAL) + (random() * INTERVAL '24 hours'),
    0.7 + random() * 0.3,
    50 + random() * 100,
    (50 + random() * 50)::SMALLINT,
    (ARRAY['grip', 'release', 'rotation', 'fine_motor'])[1 + floor(random() * 4)::INT],
    10 + random() * 40,
    35 + random() * 10
FROM generate_series(0, 6) j,
     generate_series(1, 100);

-- Вставка аномальных данных для тестирования
INSERT INTO prosthesis_telemetry (
    prosthesis_id, user_id, event_timestamp,
    signal_strength, response_time_ms, battery_level,
    movement_type, grip_force, temperature
) VALUES
    -- Низкий сигнал
    ('01111111-1111-1111-1111-111111111111'::uuid, 'a1111111-1111-1111-1111-111111111111'::uuid,
     NOW() - INTERVAL '1 hour', 0.2, 80, 70, 'grip', 30, 38),
    -- Высокое время отклика
    ('02222222-2222-2222-2222-222222222222'::uuid, 'b2222222-2222-2222-2222-222222222222'::uuid,
     NOW() - INTERVAL '2 hours', 0.9, 600, 80, 'release', 25, 40),
    -- Высокая температура
    ('03333333-3333-3333-3333-333333333333'::uuid, 'c3333333-3333-3333-3333-333333333333'::uuid,
     NOW() - INTERVAL '3 hours', 0.85, 90, 60, 'rotation', 35, 48);

DO $$
DECLARE
    cnt INTEGER;
BEGIN
    SELECT COUNT(*) INTO cnt FROM prosthesis_telemetry;
    RAISE NOTICE 'Total telemetry records created: %', cnt;
END $$;
