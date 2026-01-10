# EF Core Optimizations

Учебный проект для демонстрации техник оптимизации Entity Framework Core.

---

## Диаграммы архитектуры

### 1. Компоненты системы

```mermaid
graph TB
    subgraph "Твой компьютер"
        Browser[Браузер / curl]
    end

    subgraph "Docker контейнеры"
        API[ef-api<br/>ASP.NET Core API<br/>порт 5000]
        DB[(omt-mssql<br/>SQL Server<br/>порт 1433)]
        SEQ[ef-seq<br/>Seq Logs<br/>порт 5341]
    end

    Browser -->|HTTP запросы| API
    API -->|SQL запросы| DB
    API -->|Логи| SEQ
    Browser -->|Просмотр логов| SEQ

    style API fill:#4CAF50,color:white
    style DB fill:#2196F3,color:white
    style SEQ fill:#FF9800,color:white
```

**Легенда:**
- **ef-api** — наше приложение (принимает HTTP-запросы, отдаёт JSON)
- **omt-mssql** — база данных SQL Server (хранит клиентов, заказы, продукты)
- **ef-seq** — система логирования (показывает все SQL-запросы и ошибки)

---

### 2. Поток выполнения запроса

```mermaid
sequenceDiagram
    participant B as Браузер
    participant S as Swagger UI
    participant C as Controller
    participant D as DbContext
    participant DB as SQL Server
    participant L as Seq (Логи)

    B->>S: 1. Открыть localhost:5000
    S->>B: 2. Показать документацию API

    B->>S: 3. Нажать "Try it out"
    S->>C: 4. GET /api/nplusone/bad

    C->>D: 5. Запрос данных
    D->>DB: 6. SELECT * FROM Customers
    DB-->>D: 7. Вернуть клиентов

    D->>DB: 8. SELECT * FROM Orders (N раз!)
    DB-->>D: 9. Вернуть заказы

    D-->>C: 10. Все данные
    C->>L: 11. Записать в лог
    C-->>S: 12. JSON ответ
    S-->>B: 13. Показать результат
```

**Что происходит:**
1. Ты открываешь браузер на `localhost:5000`
2. Swagger показывает список доступных API-методов
3. Ты нажимаешь "Try it out" на каком-то методе
4. Браузер отправляет HTTP-запрос к Controller
5-9. Controller через DbContext делает SQL-запросы к базе
10-13. Результат возвращается обратно в браузер

---

### 3. Структура кода

```
Program.cs                 ← Точка входа (всё начинается здесь)
    │
    ▼
Controllers/               ← Обработчики HTTP-запросов
    ├── NPlusOneController.cs      /api/nplusone/*
    ├── ProjectionController.cs    /api/projection/*
    ├── TrackingController.cs      /api/tracking/*
    ├── IndexController.cs         /api/index/*
    └── DatabaseController.cs      /api/database/*
    │
    ▼
Data/AppDbContext.cs       ← Работа с базой данных
    │
    ▼
Models/                    ← Структура данных (таблицы)
    ├── Customer.cs        Клиенты
    ├── Order.cs           Заказы
    ├── Product.cs         Продукты
    ├── Category.cs        Категории
    ├── Review.cs          Отзывы
    └── Address.cs         Адреса
```

---

### 4. N+1 проблема (Bad vs Good)

```
┌─────────────────────────────────────────────────────────────────┐
│                    BAD: N+1 проблема                             │
│                                                                  │
│  "Дай 100 клиентов с заказами"                                   │
│                                                                  │
│  Запрос 1:   SELECT * FROM Customers LIMIT 100                   │
│  Запрос 2:   SELECT * FROM Orders WHERE CustomerId = 1          │
│  Запрос 3:   SELECT * FROM Orders WHERE CustomerId = 2          │
│  ...                                                             │
│  Запрос 101: SELECT * FROM Orders WHERE CustomerId = 100        │
│                                                                  │
│  Итого: 101 запрос ❌   Время: ~500ms                            │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                    GOOD: Оптимизированный                        │
│                                                                  │
│  "Дай 100 клиентов с заказами"                                   │
│                                                                  │
│  Запрос 1:   SELECT c.*, o.*                                     │
│              FROM Customers c                                    │
│              LEFT JOIN Orders o ON c.Id = o.CustomerId           │
│              LIMIT 100                                           │
│                                                                  │
│  Итого: 1 запрос ✅   Время: ~50ms                               │
└─────────────────────────────────────────────────────────────────┘
```

---

### 5. Docker-инфраструктура

```
docker-compose up -d
        │
        ▼
┌───────────────────────────────────────────────────────────┐
│                 efcore-network (сеть)                      │
│                                                           │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐       │
│  │  omt-mssql  │  │   ef-seq    │  │   ef-api    │       │
│  │  SQL Server │  │    Logs     │  │     API     │       │
│  │  :1433      │  │   :5341     │  │   :5000     │       │
│  └─────────────┘  └─────────────┘  └─────────────┘       │
│        │                │                │                │
│        └────────────────┴────────────────┘                │
│              Контейнеры общаются по именам                │
│              (api знает mssql как "mssql")                │
└───────────────────────────────────────────────────────────┘
        │
        ▼
┌───────────────────────────────────────────────────────────┐
│                   Твой компьютер                          │
│                                                           │
│  localhost:5000  →  Swagger UI (API)                      │
│  localhost:5341  →  Seq (логи)                            │
│  localhost:1433  →  SQL Server (база)                     │
└───────────────────────────────────────────────────────────┘
```

**Что запускается:**
- **omt-mssql** — база данных с тестовыми данными
- **ef-seq** — веб-интерфейс для просмотра логов и SQL-запросов
- **ef-api** — наше приложение (ждёт, пока база будет готова)
