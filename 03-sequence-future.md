# Діаграма послідовностей (Майбутній стан)
Сценарій: Перегляд каталогу після того, як Catalog винесено в окремий мікросервіс.

```mermaid
sequenceDiagram
    autonumber
    participant C as Клієнт
    participant G as API Gateway (:5000)
    participant Cat as Catalog Service (Майбутній мікросервіс)
    participant D as Catalog DB

    C->>G: GET /api/v1/games (HTTP/REST)
    Note over G: Шлюз бачить за оcelot.json, що шлях йде на Catalog Service
    G->>Cat: GET /api/v1/games (Мережевий виклик HTTP/REST)
    Cat->>D: SQL-запит ( SELECT * FROM Games )
    D-->>Cat: Список ігор
    Cat-->>G: 200 OK + JSON-дані
    G-->>C: 200 OK + JSON-дані