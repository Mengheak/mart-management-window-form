
USE MiniMartDB;
GO

/* ----------------------------------------------------------------------------
   Categories
   ---------------------------------------------------------------------------- */

INSERT INTO dbo.Categories (Name)
SELECT c.Name
FROM (VALUES
    (N'Beverages'),
    (N'Snacks & Confectionery'),
    (N'Dairy & Eggs'),
    (N'Bakery'),
    (N'Household'),
    (N'Personal Care')
) AS c (Name)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Categories AS existing WHERE existing.Name = c.Name);
GO

INSERT INTO dbo.Products (Name, SKU, UnitPrice, CostPrice, StockQuantity, ReorderLevel, CategoryId, IsActive)
SELECT p.Name, p.SKU, p.UnitPrice, p.CostPrice, p.StockQuantity, p.ReorderLevel, c.CategoryId, 1
FROM (VALUES
    -- Beverages
    (N'Cola 330ml Can',            N'BEV-001', CAST(1.25 AS DECIMAL(10,2)), CAST(0.70 AS DECIMAL(10,2)), 120,  24, N'Beverages'),
    (N'Orange Juice 1L',           N'BEV-002', CAST(3.40 AS DECIMAL(10,2)), CAST(2.10 AS DECIMAL(10,2)),  45,  12, N'Beverages'),
    (N'Mineral Water 500ml',       N'BEV-003', CAST(0.80 AS DECIMAL(10,2)), CAST(0.35 AS DECIMAL(10,2)), 200,  48, N'Beverages'),
    (N'Iced Tea Lemon 500ml',      N'BEV-004', CAST(1.60 AS DECIMAL(10,2)), CAST(0.95 AS DECIMAL(10,2)),   8,  20, N'Beverages'),
    (N'Instant Coffee 100g',       N'BEV-005', CAST(6.75 AS DECIMAL(10,2)), CAST(4.40 AS DECIMAL(10,2)),  30,  10, N'Beverages'),

    -- Snacks & Confectionery
    (N'Potato Chips Salted 150g',  N'SNK-001', CAST(2.20 AS DECIMAL(10,2)), CAST(1.15 AS DECIMAL(10,2)),  75,  20, N'Snacks & Confectionery'),
    (N'Milk Chocolate Bar 100g',   N'SNK-002', CAST(1.95 AS DECIMAL(10,2)), CAST(1.05 AS DECIMAL(10,2)),  90,  25, N'Snacks & Confectionery'),
    (N'Salted Peanuts 200g',       N'SNK-003', CAST(2.60 AS DECIMAL(10,2)), CAST(1.50 AS DECIMAL(10,2)),  40,  15, N'Snacks & Confectionery'),
    (N'Biscuits Assorted 300g',    N'SNK-004', CAST(3.10 AS DECIMAL(10,2)), CAST(1.80 AS DECIMAL(10,2)),  12,  15, N'Snacks & Confectionery'),

    -- Dairy & Eggs
    (N'Fresh Milk 1L',             N'DRY-001', CAST(1.85 AS DECIMAL(10,2)), CAST(1.20 AS DECIMAL(10,2)),  60,  20, N'Dairy & Eggs'),
    (N'Cheddar Cheese 250g',       N'DRY-002', CAST(4.90 AS DECIMAL(10,2)), CAST(3.30 AS DECIMAL(10,2)),  25,  10, N'Dairy & Eggs'),
    (N'Eggs Large (dozen)',        N'DRY-003', CAST(3.75 AS DECIMAL(10,2)), CAST(2.60 AS DECIMAL(10,2)),  35,  12, N'Dairy & Eggs'),
    (N'Natural Yoghurt 500g',      N'DRY-004', CAST(2.40 AS DECIMAL(10,2)), CAST(1.55 AS DECIMAL(10,2)),   6,  10, N'Dairy & Eggs'),

    -- Bakery
    (N'White Sandwich Loaf',       N'BAK-001', CAST(2.10 AS DECIMAL(10,2)), CAST(1.25 AS DECIMAL(10,2)),  28,  10, N'Bakery'),
    (N'Wholemeal Loaf',            N'BAK-002', CAST(2.45 AS DECIMAL(10,2)), CAST(1.45 AS DECIMAL(10,2)),  18,  10, N'Bakery'),
    (N'Croissant (each)',          N'BAK-003', CAST(1.30 AS DECIMAL(10,2)), CAST(0.65 AS DECIMAL(10,2)),  22,  12, N'Bakery'),

    -- Household
    (N'Dish Soap 750ml',           N'HOM-001', CAST(3.20 AS DECIMAL(10,2)), CAST(1.95 AS DECIMAL(10,2)),  44,  12, N'Household'),
    (N'Kitchen Towels 2-pack',     N'HOM-002', CAST(2.95 AS DECIMAL(10,2)), CAST(1.70 AS DECIMAL(10,2)),  36,  12, N'Household'),
    (N'Laundry Powder 1kg',        N'HOM-003', CAST(7.50 AS DECIMAL(10,2)), CAST(5.10 AS DECIMAL(10,2)),   9,  10, N'Household'),
    (N'Bin Liners 30-pack',        N'HOM-004', CAST(4.25 AS DECIMAL(10,2)), CAST(2.60 AS DECIMAL(10,2)),  27,  10, N'Household'),

    -- Personal Care
    (N'Toothpaste 100ml',          N'PER-001', CAST(2.85 AS DECIMAL(10,2)), CAST(1.60 AS DECIMAL(10,2)),  50,  15, N'Personal Care'),
    (N'Shampoo 400ml',             N'PER-002', CAST(5.40 AS DECIMAL(10,2)), CAST(3.35 AS DECIMAL(10,2)),  31,  10, N'Personal Care'),
    (N'Bar Soap 3-pack',           N'PER-003', CAST(2.30 AS DECIMAL(10,2)), CAST(1.25 AS DECIMAL(10,2)),   5,  12, N'Personal Care'),
    (N'Hand Sanitiser 250ml',      N'PER-004', CAST(3.60 AS DECIMAL(10,2)), CAST(2.05 AS DECIMAL(10,2)),  20,  10, N'Personal Care')
) AS p (Name, SKU, UnitPrice, CostPrice, StockQuantity, ReorderLevel, CategoryName)
INNER JOIN dbo.Categories AS c ON c.Name = p.CategoryName
WHERE NOT EXISTS (SELECT 1 FROM dbo.Products AS existing WHERE existing.SKU = p.SKU);
GO

/* ----------------------------------------------------------------------------
   Summary
   ---------------------------------------------------------------------------- */

SELECT
    (SELECT COUNT(*) FROM dbo.Categories) AS Categories,
    (SELECT COUNT(*) FROM dbo.Products)   AS Products,
    (SELECT COUNT(*) FROM dbo.Products WHERE StockQuantity <= ReorderLevel) AS LowStockItems,
    (SELECT COUNT(*) FROM dbo.Users)      AS Users;
GO
