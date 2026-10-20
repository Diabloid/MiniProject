# Діаграма композиції системи NovaGames

```mermaid
graph TD
    Client[Клієнт / Web UI] -->|HTTP/REST :5000| Gateway[API Gateway Ocelot]
    
    Gateway -->|HTTP/REST :5042| Monolith[NovaGames.API Моноліт]
    Gateway -.->|Майбутнє HTTP/REST| Auth[Auth Service]
    Gateway -.->|Майбутнє HTTP/REST| Catalog[Catalog Service]
    
    Monolith -->|TCP| DB[(AppDbContext MySQL)]
    Auth -.->|TCP| AuthDB[(Auth DB)]
    Catalog -.->|TCP| CatalogDB[(Catalog DB)]
    
    classDef future stroke-dasharray: 5 5;
    class Auth,Catalog,AuthDB,CatalogDB future;