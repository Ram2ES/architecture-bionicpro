CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Таблица клиентов
CREATE TABLE IF NOT EXISTS customers (
    customer_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL UNIQUE,  -- ID для связи с системой аутентификации
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    email VARCHAR(255) NOT NULL UNIQUE,
    phone VARCHAR(50),
    birth_date DATE,
    address TEXT,
    city VARCHAR(100),
    country VARCHAR(100) DEFAULT 'Russia',
    registration_date TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    status VARCHAR(20) DEFAULT 'active' CHECK (status IN ('active', 'inactive', 'suspended')),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Таблица моделей протезов (справочник)
CREATE TABLE IF NOT EXISTS prosthesis_models (
    model_id SERIAL PRIMARY KEY,
    model_name VARCHAR(100) NOT NULL,
    model_code VARCHAR(50) NOT NULL UNIQUE,
    category VARCHAR(50) NOT NULL,  -- 'arm', 'leg', 'hand', 'finger'
    description TEXT,
    base_price DECIMAL(12, 2),
    warranty_months INTEGER DEFAULT 24,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Таблица протезов клиентов
CREATE TABLE IF NOT EXISTS customer_prostheses (
    id SERIAL PRIMARY KEY,
    customer_id UUID NOT NULL REFERENCES customers(customer_id),
    prosthesis_id UUID NOT NULL UNIQUE DEFAULT uuid_generate_v4(),
    model_id INTEGER NOT NULL REFERENCES prosthesis_models(model_id),
    serial_number VARCHAR(100) NOT NULL UNIQUE,
    purchase_date DATE NOT NULL,
    warranty_end_date DATE NOT NULL,
    status VARCHAR(20) DEFAULT 'active' CHECK (status IN ('active', 'inactive', 'returned', 'replaced')),
    notes TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Таблица сервисных центров
CREATE TABLE IF NOT EXISTS service_centers (
    center_id SERIAL PRIMARY KEY,
    center_name VARCHAR(200) NOT NULL,
    city VARCHAR(100) NOT NULL,
    address TEXT,
    phone VARCHAR(50),
    email VARCHAR(255),
    is_active BOOLEAN DEFAULT TRUE
);

-- Таблица истории обслуживания
CREATE TABLE IF NOT EXISTS service_history (
    service_id SERIAL PRIMARY KEY,
    prosthesis_id UUID NOT NULL,
    center_id INTEGER REFERENCES service_centers(center_id),
    service_date DATE NOT NULL,
    service_type VARCHAR(50) NOT NULL,  -- 'maintenance', 'repair', 'calibration', 'replacement'
    description TEXT,
    cost DECIMAL(12, 2),
    technician_name VARCHAR(200),
    status VARCHAR(20) DEFAULT 'completed',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Таблица заказов
CREATE TABLE IF NOT EXISTS orders (
    order_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    customer_id UUID NOT NULL REFERENCES customers(customer_id),
    order_date TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    total_amount DECIMAL(12, 2) NOT NULL,
    status VARCHAR(20) DEFAULT 'pending',
    payment_status VARCHAR(20) DEFAULT 'pending',
    shipping_address TEXT,
    notes TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Модели протезов
INSERT INTO prosthesis_models (model_name, model_code, category, description, base_price, warranty_months) VALUES
('Bionic Arm Pro v2', 'BAP-V2', 'arm', 'Продвинутый бионический протез руки с 16 степенями свободы', 850000.00, 24),
('Bionic Arm Lite', 'BAL-V1', 'arm', 'Базовый бионический протез руки', 450000.00, 18),
('Bionic Hand Elite', 'BHE-V3', 'hand', 'Элитный протез кисти с тактильной обратной связью', 650000.00, 24),
('Bionic Hand Standard', 'BHS-V2', 'hand', 'Стандартный протез кисти', 350000.00, 18),
('Bionic Finger Set', 'BFS-V1', 'finger', 'Набор бионических пальцев', 180000.00, 12),
('Bionic Leg Advanced', 'BLA-V2', 'leg', 'Продвинутый протез ноги с адаптивной походкой', 950000.00, 24),
('Bionic Leg Basic', 'BLB-V1', 'leg', 'Базовый протез ноги', 550000.00, 18);

-- Сервисные центры
INSERT INTO service_centers (center_name, city, address, phone, email) VALUES
('BionicPRO Сервис Москва', 'Москва', 'ул. Инновационная, д. 15', '+7 (495) 123-45-67', 'service.msk@bionicpro.ru'),
('BionicPRO Сервис СПб', 'Санкт-Петербург', 'Невский пр., д. 100', '+7 (812) 234-56-78', 'service.spb@bionicpro.ru'),
('BionicPRO Сервис Казань', 'Казань', 'ул. Технопарковая, д. 8', '+7 (843) 345-67-89', 'service.kzn@bionicpro.ru'),
('BionicPRO Сервис Новосибирск', 'Новосибирск', 'ул. Академическая, д. 25', '+7 (383) 456-78-90', 'service.nsk@bionicpro.ru');

-- Заполнение тестовыми данными клиентов
INSERT INTO customers (customer_id, user_id, first_name, last_name, email, phone, birth_date, city, registration_date) VALUES
('c1111111-1111-1111-1111-111111111111'::uuid, 'a1111111-1111-1111-1111-111111111111'::uuid,
 'Иван', 'Петров', 'ivan.petrov@example.com', '+7 (916) 111-11-11', '1985-03-15', 'Москва', '2024-01-15'),
('c2222222-2222-2222-2222-222222222222'::uuid, 'b2222222-2222-2222-2222-222222222222'::uuid,
 'Мария', 'Сидорова', 'maria.sidorova@example.com', '+7 (921) 222-22-22', '1990-07-22', 'Санкт-Петербург', '2024-02-20'),
('c3333333-3333-3333-3333-333333333333'::uuid, 'c3333333-3333-3333-3333-333333333333'::uuid,
 'Алексей', 'Козлов', 'alexey.kozlov@example.com', '+7 (927) 333-33-33', '1978-11-08', 'Казань', '2024-03-10');

INSERT INTO customers (user_id, first_name, last_name, email, phone, birth_date, city, registration_date) VALUES
(uuid_generate_v4(), 'Елена', 'Новикова', 'elena.novikova@example.com', '+7 (905) 444-44-44', '1992-05-30', 'Новосибирск', '2024-04-05'),
(uuid_generate_v4(), 'Дмитрий', 'Волков', 'dmitry.volkov@example.com', '+7 (903) 555-55-55', '1988-09-12', 'Екатеринбург', '2024-05-18'),
(uuid_generate_v4(), 'Анна', 'Морозова', 'anna.morozova@example.com', '+7 (919) 666-66-66', '1995-01-25', 'Москва', '2024-06-22');


INSERT INTO customer_prostheses (customer_id, prosthesis_id, model_id, serial_number, purchase_date, warranty_end_date, status) VALUES
('c1111111-1111-1111-1111-111111111111'::uuid, '01111111-1111-1111-1111-111111111111'::uuid, 1, 'BAP-V2-2024-001', '2024-01-20', '2026-01-20', 'active'),
('c2222222-2222-2222-2222-222222222222'::uuid, '02222222-2222-2222-2222-222222222222'::uuid, 3, 'BHE-V3-2024-015', '2024-02-25', '2026-02-25', 'active'),
('c3333333-3333-3333-3333-333333333333'::uuid, '03333333-3333-3333-3333-333333333333'::uuid, 6, 'BLA-V2-2024-008', '2024-03-15', '2026-03-15', 'active');

INSERT INTO customer_prostheses (customer_id, model_id, serial_number, purchase_date, warranty_end_date, status)
SELECT
    c.customer_id,
    (SELECT model_id FROM prosthesis_models ORDER BY RANDOM() LIMIT 1),
    'BIO-' || EXTRACT(YEAR FROM NOW())::TEXT || '-' || LPAD((ROW_NUMBER() OVER())::TEXT, 4, '0'),
    NOW() - (RANDOM() * 365 || ' days')::INTERVAL,
    NOW() + ((365 + RANDOM() * 365) || ' days')::INTERVAL,
    'active'
FROM customers c
WHERE c.customer_id NOT IN ('c1111111-1111-1111-1111-111111111111'::uuid, 'c2222222-2222-2222-2222-222222222222'::uuid, 'c3333333-3333-3333-3333-333333333333'::uuid);


INSERT INTO service_history (prosthesis_id, center_id, service_date, service_type, description, cost, technician_name, status) VALUES
-- Обслуживание протеза Ивана Петрова
('01111111-1111-1111-1111-111111111111'::uuid, 1, '2024-04-15', 'maintenance', 'Плановое ТО, калибровка датчиков', 5000.00, 'Смирнов А.В.', 'completed'),
('01111111-1111-1111-1111-111111111111'::uuid, 1, '2024-07-20', 'calibration', 'Подстройка чувствительности миодатчиков', 3000.00, 'Смирнов А.В.', 'completed'),
('01111111-1111-1111-1111-111111111111'::uuid, 1, '2024-10-10', 'maintenance', 'Плановое ТО, замена батареи', 8000.00, 'Кузнецов И.П.', 'completed'),

-- Обслуживание протеза Марии Сидоровой
('02222222-2222-2222-2222-222222222222'::uuid, 2, '2024-05-10', 'maintenance', 'Плановое ТО', 4500.00, 'Иванова Е.С.', 'completed'),
('02222222-2222-2222-2222-222222222222'::uuid, 2, '2024-08-25', 'repair', 'Замена серводвигателя указательного пальца', 15000.00, 'Иванова Е.С.', 'completed'),

-- Обслуживание протеза Алексея Козлова
('03333333-3333-3333-3333-333333333333'::uuid, 3, '2024-06-01', 'maintenance', 'Плановое ТО, обновление прошивки', 4000.00, 'Федоров М.К.', 'completed'),
('03333333-3333-3333-3333-333333333333'::uuid, 3, '2024-09-15', 'calibration', 'Калибровка походки', 6000.00, 'Федоров М.К.', 'completed');



INSERT INTO orders (customer_id, order_date, total_amount, status, payment_status, shipping_address) VALUES
('c1111111-1111-1111-1111-111111111111'::uuid, '2024-01-15', 850000.00, 'completed', 'paid', 'Москва, ул. Примерная, д. 10, кв. 5'),
('c2222222-2222-2222-2222-222222222222'::uuid, '2024-02-20', 650000.00, 'completed', 'paid', 'Санкт-Петербург, Невский пр., д. 50, кв. 12'),
('c3333333-3333-3333-3333-333333333333'::uuid, '2024-03-10', 950000.00, 'completed', 'paid', 'Казань, ул. Баумана, д. 25, кв. 8');

-- Представление для ETL
CREATE OR REPLACE VIEW v_customer_prostheses_full AS
SELECT
    c.customer_id,
    c.user_id,
    c.first_name,
    c.last_name,
    c.email,
    c.phone,
    cp.prosthesis_id,
    pm.model_name AS prosthesis_model,
    cp.serial_number AS prosthesis_serial_number,
    cp.purchase_date,
    cp.warranty_end_date,
    sh.last_service_date,
    sh.service_center_name
FROM customers c
INNER JOIN customer_prostheses cp ON c.customer_id = cp.customer_id
INNER JOIN prosthesis_models pm ON cp.model_id = pm.model_id
LEFT JOIN (
    SELECT DISTINCT ON (prosthesis_id)
        prosthesis_id,
        service_date AS last_service_date,
        sc.center_name AS service_center_name
    FROM service_history sh
    JOIN service_centers sc ON sh.center_id = sc.center_id
    ORDER BY prosthesis_id, service_date DESC
) sh ON cp.prosthesis_id = sh.prosthesis_id
WHERE c.status = 'active'
  AND cp.status = 'active';

DO $$
DECLARE
    customers_cnt INTEGER;
    prostheses_cnt INTEGER;
    services_cnt INTEGER;
BEGIN
    SELECT COUNT(*) INTO customers_cnt FROM customers;
    SELECT COUNT(*) INTO prostheses_cnt FROM customer_prostheses;
    SELECT COUNT(*) INTO services_cnt FROM service_history;

    RAISE NOTICE 'CRM Database initialized:';
    RAISE NOTICE '  - Customers: %', customers_cnt;
    RAISE NOTICE '  - Prostheses: %', prostheses_cnt;
    RAISE NOTICE '  - Service records: %', services_cnt;
END $$;
