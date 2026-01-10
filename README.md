# Interview Helper API

Веб-приложение для проведения тренировочных технических собеседований с использованием ИИ (GigaChat).

## Возможности

- **Регистрация и аутентификация** пользователей (JWT)
- **Создание собеседований** на основе описания вакансии
- **Генерация вопросов** с помощью GigaChat AI
- **Оценка ответов** пользователей с детальным анализом
- **Статистика и рекомендации** по улучшению навыков
- **Управление сессиями** собеседований
- **Темы для изучения** с приоритетами и ресурсами

## Архитектура

```
InterviewHelperAPI/
├── Features/
│   ├── Auth/           # Аутентификация и регистрация
│   │   ├── Auth/       # Вход в систему
│   │   └── Registration/ # Регистрация
│   └── Free/           # Функционал для бесплатного тарифа
│       └── Interview/  # Логика собеседований
├── Service/
│   └── GigaChat/       # Интеграция с GigaChat API
├── Models/             # Сущности базы данных
└── Program.cs          # Конфигурация приложения
```

## Требования

- .NET 9.0 SDK
- Docker & Docker Compose
- MySQL 8.0
- Учетная запись Sber AI для доступа к GigaChat API

## Установка и запуск

### 1. Клонирование репозитория

```bash
git clone <repository-url>
cd InterviewHelperAPI
```

### 2. Настройка окружения

Создайте файл `appsettings.json` в корне проекта:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "server=mysql;port=3306;database=HelperDb;user=root;password=rootpassword"
  },
  "Jwt": {
    "Key": "your-super-secret-key-at-least-32-characters-long",
    "Issuer": "InterviewHelperAPI",
    "Audience": "InterviewHelperClient",
    "ExpireMinutes": 60
  },
  "GigaChat": {
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret",
    "Scope": "GIGACHAT_API_PERS",
    "AuthUrl": "https://ngw.devices.sberbank.ru:9443/api/v2/oauth",
    "ApiUrl": "https://gigachat.devices.sberbank.ru/api/v1/chat/completions",
    "MaxTokens": 2000,
    "Temperature": 0.7
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### 3. Запуск с Docker Compose

```bash
docker-compose up -d
```

Приложение будет доступно по адресу: `http://localhost:8080`

### 4. Запуск без Docker

```bash
# Запуск базы данных
docker run -d --name interview-mysql \
  -e MYSQL_ROOT_PASSWORD=rootpassword \
  -e MYSQL_DATABASE=HelperDb \
  -p 3306:3306 \
  mysql:8.0

# Запуск приложения
cd InterviewHelperAPI
dotnet restore
dotnet run
```

## API Endpoints

### Аутентификация

- `POST /register` - Регистрация нового пользователя
- `POST /auth` - Вход в систему

### Собеседования

- `GET /get/interview` - Получение списка собеседований пользователя
- `POST /interview/start` - Начало нового собеседования
- `POST /interview/{id}/response` - Отправка ответа на вопрос

### Системные

- `GET /` - Проверка работы API
- `GET /api/health` - Проверка здоровья системы
- `GET /api/test` - Тестовый endpoint

## База данных

Структура базы данных включает следующие таблицы:

### Основные таблицы:
- `users` - Пользователи и их подписки
- `interviews` - Собеседования
- `interview_questions` - Вопросы собеседования
- `user_responses` - Ответы пользователей
- `skill_evaluations` - Оценки навыков
- `study_topics` - Темы для изучения
- `user_statistics` - Статистика пользователя

### Индексы:
- Full-text индекс для поиска по ответам
- Индексы для быстрого поиска собеседований по статусу и дате
- Уникальные индексы для предотвращения дублирования

## Настройка GigaChat

1. Получите учетные данные на [Sber AI](https://developers.sber.ru/studio/gigachat)
2. Укажите `ClientId` и `ClientSecret` в настройках
3. Убедитесь, что у вас есть доступ к `GIGACHAT_API_PERS`

## Безопасность

- **JWT токены** с временем жизни 6000 минут
- **Хеширование паролей** с использованием PBKDF2
- **Аутентификация** на всех защищенных endpoint'ах
- **Защита от SQL-инъекций** через Entity Framework

## Тестирование

Для тестирования в режиме разработки используйте:

```bash
curl -X POST http://localhost:8080/register \
  -H "Content-Type: application/json" \
  -d '{"username":"testuser","email":"test@example.com","password":"password123"}'
```

## Отладка

При возникновении проблем:

1. Проверьте логи Docker:
```bash
docker-compose logs -f app
```

2. Убедитесь в доступности базы данных:
```bash
docker exec -it interview-mysql mysql -u root -p
```

3. Проверьте настройки GigaChat в логах приложения

## Примеры запросов

### Регистрация
```json
{
  "username": "john_doe",
  "email": "john@example.com",
  "password": "securepassword123",
  "subscriptionTier": "free"
}
```

### Начало собеседования
```json
{
  "userId": 1,
  "jobTitle": "Backend Developer",
  "jobDescription": "Разработка API на .NET Core, работа с PostgreSQL, опыт с Docker",
  "jobLevel": "middle"
}
```

### Ответ на вопрос
```json
{
  "userAnswer": "Я использую Entity Framework Core для работы с базой данных. Для оптимизации запросов добавляю индексы и использую AsNoTracking() для read-only операций.",
  "questionId": 123
}
```

## Мониторинг

- Логирование всех запросов к GigaChat API
- Отслеживание времени ответов
- Мониторинг использования токенов
- Статистика успешности собеседований

## Вклад в проект

1. Форкните репозиторий
2. Создайте ветку для фичи
3. Внесите изменения
4. Напишите тесты
5. Создайте Pull Request

## Поддержка

При возникновении вопросов:
1. Проверьте документацию
2. Посмотрите Issues на GitHub
3. Создайте новый Issue с описанием проблемы

---

**Примечание**: Для работы в продакшене рекомендуется:
- Использовать HTTPS
- Настроить правильные CORS политики
- Регулярно обновлять зависимости
- Настроить мониторинг и алертинг
