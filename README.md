### Запуск Airflow

```bash
cd airflow
docker compose up -d
```
БД наполнится автоматически контейнерами *-init

### Запуск API + Frontend + Auth

```bash
docker compose up -d
```

Сервисы:
- **Frontend**: http://localhost:3000
- **API**: http://localhost:5000
- **Keycloak**: http://localhost:8080
## Тестовые пользователи

Создаются автоматически в Keycloak:
- **ivan.petrov** / `password123` (prothetic_user, ID: a1111111-1111-1111-1111-111111111111)
- **maria.sidorova** / `password123` (prothetic_user, ID: b2222222-2222-2222-2222-222222222222)
- **alexey.kozlov** / `password123` (prothetic_user, ID: c3333333-3333-3333-3333-333333333333)
- **admin1** / `admin123` (administrator)

## Проверка авторизации (попытка получить чужой отчёт)

### 1. Получить токен для ivan.petrov

```bash
TOKEN=$(curl -s -X POST http://localhost:8080/realms/reports-realm/protocol/openid-connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "username=ivan.petrov" \
  -d "password=password123" \
  -d "grant_type=password" \
  -d "client_id=reports-frontend" | jq -r '.access_token')
```

### 2. Попробовать получить свой отчёт (должно работать)

```bash
curl -i http://localhost:5000/api/reports/user/a1111111-1111-1111-1111-111111111111/download \
  -H "Authorization: Bearer $TOKEN"
```

Ожидаемый результат: `200 OK` (или `404` если нет отчётов)

### 3. Попробовать получить чужой отчёт (должно быть запрещено)

```bash
curl -i http://localhost:5000/api/reports/user/b2222222-2222-2222-2222-222222222222/download \
  -H "Authorization: Bearer $TOKEN"
```

Ожидаемый результат: `403 Forbidden` - доступ запрещён
