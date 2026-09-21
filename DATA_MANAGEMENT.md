# Managing the Data in the Database

How to add, correct, inspect, back up, and reset the data held in `MiniMartDB`.

> Companion documents: [README.md](README.md) = install and run ·
> [PROJECT_EXPLAINED.md](PROJECT_EXPLAINED.md) = the system in plain language ·
> [SYSTEM_FLOW.md](SYSTEM_FLOW.md) = the code traced file by file.

There are two paths, and they are not equal. **Path 1 — the app — is the one to use for
everything routine,** because the business rules run there. Path 2 — direct SQL — is for
reporting the app doesn't cover, bulk edits, corrections it deliberately can't make, and
backups.

---

## 1. Through the app — the normal way

Sign in as **Admin**. Every routine operation is behind a nav button on the dashboard.

| Data | Screen | What you can do |
|---|---|---|
| **Products** | Products | Create, edit (name, SKU, unit price, cost price, reorder level, category), delete, and **Adjust Stock** |
| **Categories** | Categories | Create, rename, delete |
| **Users** | Users | Create Admin/Cashier accounts, rename, change role, reset password, deactivate, delete |
| **Sales** | Sales History & Reports | **Read only** — filter by date range, drill into a receipt's lines, daily totals, best sellers |
| **Low Stock** | Low Stock | Everything at or below its reorder level |
| *(new sales)* | Point of Sale | The only way a `Sales` row is ever created |

The dashboard refreshes its tiles every time one of these windows closes, so the numbers you
see are never stale.

### 1.1 Five behaviours that are deliberate

**Adjust Stock sets an absolute number, not a delta.** It is a stock-take: you type what is
actually on the shelf and the app stores that. (`InventoryService.AdjustStockAsync` →
`Product.AdjustStockTo`.) It is not "add 12 received units" — work out the new total
yourself.

**Stock can never go negative.** Guarded in three independent places: the model
(`Product.ReduceStock` / `AdjustStockTo` throw), the checkout SQL
(`WHERE StockQuantity >= @Quantity`), and the database itself
(`CHECK (StockQuantity >= 0)`). Even hand-written SQL cannot get past the last one.

**"Low stock" means `StockQuantity <= ReorderLevel`** — at the level, not just below it.
Set `ReorderLevel` per product on the Products screen; that is what drives the Low Stock
screen and the dashboard tile.

**Delete silently becomes deactivate** for any product or user that already appears in a
sale, so old receipts stay resolvable. The app tells you which of the two happened.
Categories behave differently again: a category still holding products refuses to be
deleted and tells you how many are in it.

**You cannot remove the last administrator.** Demoting, deactivating, or deleting an admin
is blocked when it would leave zero active admins.

### 1.2 The one thing the app cannot do: void or refund a sale

`ISaleRepository` has exactly one write method — `SaveSaleAsync`. There is no void, no
refund, no line correction, and no way to delete a receipt from any screen. Sales are
**append-only** by design.

So a mis-scanned sale has to be corrected in SQL — see [§4.1](#41-voiding-a-sale-takes-two-steps).
Plan for that: it is a genuine gap, not something hidden in a menu.

### 1.3 Password rules

Minimum **6 characters** (`User.MinimumPasswordLength`), with no complexity requirement.
Passwords are stored only as a salted **PBKDF2-HMAC-SHA256** hash, 100,000 iterations, in
the form `PBKDF2$100000$<base64 salt>$<base64 hash>`. Nothing in the system can recover a
password — only verify one. Reset from the Users screen by typing a new password into the
edit dialog; leaving that field blank leaves the existing password untouched.

---

## 2. Direct database access

### 2.1 dbgate — already running, no setup

The Docker stack includes **dbgate**, a browser-based database client, pre-wired to the
server:

```
http://localhost:3033
```

Browse the five tables, edit rows in a grid, run SQL, export results to CSV/JSON/Excel.

### 2.2 SSMS or Azure Data Studio

Connect to `localhost,1434`, SQL Server authentication, user `sa`, with the password from
`docker-compose.yml`. Tick *Trust server certificate*. For a non-Docker instance use the
server name from README §3.2 with Windows authentication.

### 2.3 sqlcmd — one-off queries from the terminal

```bash
sqlcmd -S localhost,1434 -U sa -P 'Heak020507#' -d MiniMartDB -Q "SELECT TOP 20 * FROM dbo.Sales ORDER BY SaleId DESC;"
```

Useful flags: `-s","` sets the column separator, `-W` trims whitespace, `-h-1` removes
headers, `-o file.csv` writes to a file. Together they make a passable CSV export:

```bash
sqlcmd -S localhost,1434 -U sa -P 'Heak020507#' -d MiniMartDB -s"," -W -h-1 -Q "SELECT SaleId, SaleDate, TotalAmount FROM dbo.Sales;" -o sales.csv
```

---

## 3. Queries the app does not give you

### 3.1 Inventory valuation

```sql
SELECT SUM(StockQuantity * CostPrice) AS AtCost,
       SUM(StockQuantity * UnitPrice) AS AtRetail,
       SUM(StockQuantity * (UnitPrice - CostPrice)) AS PotentialMargin
FROM dbo.Products
WHERE IsActive = 1;
```

### 3.2 Profit per product

The app reports **revenue** but never **margin** — `CostPrice` is stored and maintained but
no screen uses it. This is where to get it:

```sql
SELECT p.Name,
       SUM(sd.Quantity)                                AS UnitsSold,
       SUM(sd.Quantity * sd.UnitPrice)                 AS Revenue,
       SUM(sd.Quantity * (sd.UnitPrice - p.CostPrice)) AS GrossProfit
FROM dbo.SaleDetails sd
JOIN dbo.Products p ON p.ProductId = sd.ProductId
GROUP BY p.Name
ORDER BY GrossProfit DESC;
```

Note this uses `sd.UnitPrice` (the price *at the time of sale*) against `p.CostPrice` (the
cost *now*) — accurate as long as cost prices are reasonably stable.

### 3.3 Takings per cashier

```sql
SELECT u.Username,
       COUNT(*)            AS Sales,
       SUM(s.TotalAmount)  AS Revenue,
       AVG(s.TotalAmount)  AS AverageBasket
FROM dbo.Sales s
JOIN dbo.Users u ON u.UserId = s.CashierId
GROUP BY u.Username
ORDER BY Revenue DESC;
```

### 3.4 Sales by hour of day — for staffing decisions

```sql
SELECT DATEPART(HOUR, SaleDate) AS Hour,
       COUNT(*)                 AS Sales,
       SUM(TotalAmount)         AS Revenue
FROM dbo.Sales
GROUP BY DATEPART(HOUR, SaleDate)
ORDER BY Hour;
```

### 3.5 Products that have never sold

```sql
SELECT p.Name, p.SKU, p.StockQuantity
FROM dbo.Products p
WHERE p.IsActive = 1
  AND NOT EXISTS (SELECT 1 FROM dbo.SaleDetails sd WHERE sd.ProductId = p.ProductId);
```

### 3.6 Reprint data for one receipt

```sql
SELECT s.SaleId, s.SaleDate, u.Username AS Cashier,
       p.Name AS Product, sd.Quantity, sd.UnitPrice,
       sd.Quantity * sd.UnitPrice AS LineTotal,
       s.TotalAmount, s.AmountPaid, s.ChangeDue
FROM dbo.Sales s
JOIN dbo.Users u        ON u.UserId    = s.CashierId
JOIN dbo.SaleDetails sd ON sd.SaleId   = s.SaleId
JOIN dbo.Products p     ON p.ProductId = sd.ProductId
WHERE s.SaleId = 57;
```

### 3.7 Bulk edits

A 5% price rise across one category — tedious one product at a time in the UI:

```sql
UPDATE p
SET UnitPrice = ROUND(p.UnitPrice * 1.05, 2)
FROM dbo.Products p
JOIN dbo.Categories c ON c.CategoryId = p.CategoryId
WHERE c.Name = N'Beverages';
```

Set a sensible reorder level everywhere it was left at the default:

```sql
UPDATE dbo.Products SET ReorderLevel = 10 WHERE ReorderLevel = 5;
```

Bulk-import products from a staging table or `VALUES` list is fine too — just respect
`UQ_Products_SKU` and supply a valid `CategoryId`.

---

## 4. Correcting data by hand — four cautions

### 4.1 Voiding a sale takes two steps

Deleting the `Sales` row cascades to its `SaleDetails` (the foreign key is
`ON DELETE CASCADE`), **but it does not put the stock back.** Nothing in the schema reverses
the decrement. Do both, in one transaction, or your stock figures drift:

```sql
BEGIN TRAN;

  -- 1. return the goods to stock
  UPDATE p
  SET StockQuantity = p.StockQuantity + sd.Quantity
  FROM dbo.Products p
  JOIN dbo.SaleDetails sd ON sd.ProductId = p.ProductId
  WHERE sd.SaleId = 57;

  -- 2. remove the receipt (lines cascade automatically)
  DELETE FROM dbo.Sales WHERE SaleId = 57;

COMMIT;
```

Check it first with `SELECT` and the same `WHERE`, and keep a note of what you voided — the
schema has no audit table, so a deleted sale leaves no trace.

An alternative that keeps history intact is to record a **compensating sale** rather than
delete: it keeps the audit trail honest, but the schema has no negative-quantity support
(`CHECK (Quantity > 0)`), so this needs a schema change to do properly. Deleting is the
practical option today.

### 4.2 Never hand-edit `Users.PasswordHash`

Only `Pbkdf2PasswordHasher` can produce a valid value. A malformed hash does not raise an
error — `Verify` returns `false` for anything it cannot parse, so the effect is simply that
every sign-in for that account fails, with no clue why.

Reset passwords through the **Users** screen. To recover a completely locked-out system,
delete the admin row and let first-run setup recreate it:

```sql
DELETE FROM dbo.Users WHERE Username = 'admin';
```

If that user has processed sales the foreign key on `Sales.CashierId` will block the delete —
in that case set `IsActive = 0`, create a fresh admin, or restore from a backup.

### 4.3 Editing stock directly bypasses the model's guards

The database `CHECK (StockQuantity >= 0)` still protects you, but `Product.AdjustStockTo`'s
validation does not run. Use the **Adjust Stock** screen for anything routine; reserve SQL
for bulk corrections.

### 4.4 Deactivating vs. deleting — know which you want

`IsActive = 0` on a product means it disappears from the till (`GetBySkuAsync` filters
`AND p.IsActive = 1`) and from the dashboard's active-product count, but stays fully
resolvable in sales history. That is almost always what you want for a discontinued line.
Actual deletion is only possible for products that have never sold.

---

## 5. Backup and restore

The data lives in the `sql_data` Docker volume. It survives `docker compose down` and
`docker compose restart` — but **`docker compose down -v` destroys it permanently.** Take
real backups.

### 5.1 Back up

```bash
sqlcmd -S localhost,1434 -U sa -P 'Heak020507#' -Q "BACKUP DATABASE MiniMartDB TO DISK='/var/opt/mssql/data/MiniMartDB.bak' WITH INIT, COMPRESSION;"
```

The path is *inside the container*. Copy it out to the host so it survives the volume:

```bash
docker cp sql1:/var/opt/mssql/data/MiniMartDB.bak ./backups/MiniMartDB.bak
```

For a non-Docker instance, back up to any local path the SQL Server service account can
write, e.g. `TO DISK='C:\Backups\MiniMartDB.bak'`.

### 5.2 Restore

Copy the file back into the container, then restore over the existing database:

```bash
docker cp ./backups/MiniMartDB.bak sql1:/var/opt/mssql/data/MiniMartDB.bak
```

```bash
sqlcmd -S localhost,1434 -U sa -P 'Heak020507#' -Q "ALTER DATABASE MiniMartDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE; RESTORE DATABASE MiniMartDB FROM DISK='/var/opt/mssql/data/MiniMartDB.bak' WITH REPLACE; ALTER DATABASE MiniMartDB SET MULTI_USER;"
```

Close the app first — `SET SINGLE_USER` kicks off any open connection, and `RESTORE` fails
outright if one is still attached.

### 5.3 What to back up, and when

The whole database is small (five tables, no blobs), so there is no reason to do anything
clever. A daily full backup kept off the machine is sufficient. `Sales` and `SaleDetails`
are the only truly irreplaceable tables — products and categories can be rebuilt from the
seed script, and users can be recreated by hand.

---

## 6. Resetting to a clean state

**Top up the catalogue, keep everything else.** `MiniMart_SeedData.sql` is safe to re-run at
any time — every insert is guarded by `NOT EXISTS`, so it adds what is missing and
duplicates nothing:

```bash
sqlcmd -S localhost,1434 -U sa -P 'Heak020507#' -b -i MiniMart_SeedData.sql
```

**Clear the sales history, keep products and users:**

```sql
DELETE FROM dbo.Sales;   -- SaleDetails cascade
```

(This does **not** restore stock — see §4.1. After wiping sales, re-run a stock-take or
reset quantities with the seed values.)

**Full wipe.** `MiniMart_Database_Schema.sql` drops and recreates all five tables, losing
everything. It only works if you drop in foreign-key order first — the script drops
`Categories` before `Products`, which the FK blocks:

```sql
USE MiniMartDB;
IF OBJECT_ID('dbo.SaleDetails','U') IS NOT NULL DROP TABLE dbo.SaleDetails;
IF OBJECT_ID('dbo.Sales','U')       IS NOT NULL DROP TABLE dbo.Sales;
IF OBJECT_ID('dbo.Products','U')    IS NOT NULL DROP TABLE dbo.Products;
IF OBJECT_ID('dbo.Users','U')       IS NOT NULL DROP TABLE dbo.Users;
IF OBJECT_ID('dbo.Categories','U')  IS NOT NULL DROP TABLE dbo.Categories;
```

Then run the schema script, then the seed script, then launch the app — it will ask you to
create the first admin again.

**Nuclear option** (Docker only): `docker compose down -v && docker compose up -d` throws
away the volume and gives you a brand-new empty server. You then re-run both scripts.

---

## 7. What the database enforces regardless of how you edit it

Worth knowing, because these hold even for hand-written SQL and dbgate grid edits:

| Rule | Constraint |
|---|---|
| Category names are unique | `UQ_Categories_Name` |
| SKUs are unique | `UQ_Products_SKU` |
| Usernames are unique | `UQ_Users_Username` |
| A role is only `Admin` or `Cashier` | `CK_Users_Role` |
| Stock, prices, totals, paid, change are never negative | `CK_Products_*`, `CK_Sales_*` |
| A sale line quantity is always > 0 | `CK_SaleDetails_Quantity` |
| A product must belong to a real category | `FK_Products_Categories` |
| A sale must belong to a real user | `FK_Sales_Users` |
| Deleting a sale removes its lines | `FK_SaleDetails_Sales ... ON DELETE CASCADE` |
| A product in any sale line cannot be deleted | `FK_SaleDetails_Products` (no cascade — this is why soft delete exists) |

Full definitions in [MiniMart_Database_Schema.sql](MiniMart_Database_Schema.sql).
