# Аналіз моноліту NovaGames

| Контролер | Сутності | Можливі Таблиці (БД) | Хто ще їх читає / пише |
| :--- | :--- | :--- | :--- |
| **Auth / Users** (`AuthController`, `UsersController`) | `User`, `UserRole`, `AuditLog` | `Users`, `UserRoles`, `AuditLogs` | Усі інші контролери читають `UserId` та статус авторизації. |
| **Catalog** (`GamesController`, `UploadController`) | `Game`, `DigitalGame`, `PhysicalGame`, `SubscriptionGame` | `Games`, `DigitalGames`, `PhysicalGames` | `CartController`, `LibraryController`, `ReviewsController` — усі читають дані ігор. |
| **Ordering** (`CartController`) | `CartItem`, `Order`, `OrderItem`, `ShippingDetail` | `CartItems`, `Orders`, `OrderItems`, `ShippingDetails` | `WalletController` (для зняття коштів), `LibraryController` (для видачі гри). |
| **Library** (`LibraryController`) | `UserLibrary`, `UserSubscription` | `UserLibraries`, `UserSubscriptions` | Читає `Games` та `Users`. |
| **Social / Finance** (`ReviewsController`, `WalletController`) | `Review`, `SavedCard` | `Reviews`, `SavedCards` | Читає `Users` та `Games`. |

## Як запустити проєкт
Для локального тестування архітектури необхідно запустити два застосунки:

1. **NovaGames.API (Моноліт)**
   - Працює на порту: `http://localhost:5042`
   - Команда для запуску: `dotnet run --project src/Monolith/NovaGames.API/NovaGames.API.csproj`

2. **ApiGateway (Шлюз)**
   - Працює на порту: `http://localhost:5000`
   - Команда для запуску: `dotnet run --project src/ApiGateway/ApiGateway.csproj`

### Приклад запиту через шлюз
Усі запити слід направляти на порт шлюзу (5000). Шлюз самостійно перенаправить їх на моноліт.
`GET http://localhost:5000/api/v1/games`
`GET http://localhost:5000/health`

### Документація
Архітектурні рішення (ADR) та діаграми системи знаходяться у директорії `docs/`.