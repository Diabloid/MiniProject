# Діаграма послідовностей (Поточний стан)
Сценарій: Перегляд списку ігор каталогу через шлюз.

```mermaid
sequenceDiagram
    autonumber
    participant C as Клієнт
    participant G as API Gateway (:5000)
    participant M as Моноліт NovaGames.API (:5042)
    participant D as БД (MySQL/SQLServer)

    C->>G: GET /api/v1/games (HTTP/REST)
    G->>M: Проксіювання запиту (HTTP/REST)
    M->>D: SQL-запит ( SELECT * FROM Products WHERE IsPublished = 1 )
    D-->>M: Список ігор
    M-->>G: 200 OK + JSON-дані
    G-->>C: 200 OK + JSON-дані