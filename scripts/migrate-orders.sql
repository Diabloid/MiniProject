-- Міграція історичних даних замовлень з моноліту (novagamesdb) до miniproject_ordering
USE miniproject_ordering;

INSERT INTO miniproject_ordering.Orders (Id, UserId, Status, TotalAmount, PaymentMethod, CustomerNotes, IdempotencyKey, CreatedAt)
SELECT 
    o.Id, 
    o.UserId, 
    CASE 
        WHEN o.Status LIKE '%Stripe%' OR o.Status = 'Paid' THEN 'Completed' 
        WHEN o.Status = 'Cancelled' THEN 'Cancelled'
        ELSE 'Pending' 
    END AS Status,
    o.TotalAmount, 
    'Card' AS PaymentMethod, 
    'Мігровано з монолітної БД' AS CustomerNotes, 
    CONCAT('legacy-order-', o.Id) AS IdempotencyKey, 
    o.OrderDate AS CreatedAt
FROM novagamesdb.orders o
ON DUPLICATE KEY UPDATE 
    TotalAmount = VALUES(TotalAmount),
    Status = VALUES(Status);

INSERT INTO miniproject_ordering.OrderItems (Id, OrderId, GameId, Title, UnitPrice, Quantity)
SELECT 
    oi.Id, 
    oi.OrderId, 
    oi.ProductId AS GameId, 
    COALESCE(p.Title, CONCAT('Гра #', oi.ProductId)) AS Title, 
    oi.PriceAtPurchase AS UnitPrice, 
    oi.Quantity
FROM novagamesdb.orderitems oi
LEFT JOIN novagamesdb.products p ON oi.ProductId = p.Id
ON DUPLICATE KEY UPDATE 
    UnitPrice = VALUES(UnitPrice);

-- Підняття автоінкременту вище максимального старого ID (п. 4.3 методички)
ALTER TABLE miniproject_ordering.Orders AUTO_INCREMENT = 100;
ALTER TABLE miniproject_ordering.OrderItems AUTO_INCREMENT = 100;
