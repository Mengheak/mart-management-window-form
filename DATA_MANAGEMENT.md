# ការគ្រប់គ្រងទិន្នន័យក្នុង Database

របៀបបន្ថែម, កែ, ពិនិត្យ, backup និង reset ទិន្នន័យក្នុង `MiniMartDB`។ ឯកសារនេះសម្រាប់ Admin, developer ឬអ្នកថែទាំ database។ ការពន្យល់ប្រើភាសាខ្មែរ ដោយរក្សា technical terms, UI labels, SQL និង commands ដដែល។

> ឯកសារពាក់ព័ន្ធ៖ [README.md](README.md) សម្រាប់ដំឡើង និង run · [PROJECT_EXPLAINED.md](PROJECT_EXPLAINED.md) ពន្យល់ប្រព័ន្ធងាយយល់ · [SYSTEM_FLOW.md](SYSTEM_FLOW.md) បង្ហាញលំហូរ code។

មានពីរវិធី៖ **ប្រើ app សម្រាប់ការងារប្រចាំថ្ងៃ** ដើម្បីឱ្យ business rules ត្រូវបានអនុវត្ត; ប្រើ **direct SQL** សម្រាប់ queries បន្ថែម, bulk edits, maintenance និង backup ដែល app មិនផ្ដល់។

## ជ្រើសរើសវិធីត្រឹមត្រូវ

| ការងារ | វិធីដែលណែនាំ | ហេតុផល |
|---|---|---|
| បន្ថែម ឬកែ Product, Category, User | App | Service validation និង business rules ដំណើរការ |
| កែ stock របស់ Product មួយ | **Adjust Stock** ក្នុង App | ពិនិត្យ product និង quantity មុន save |
| មើល Sales និង Reports | App | Query និងការបង្ហាញបានរៀបចំរួច |
| Report ពិសេស ឬ read-only analysis | SQL query | App មិនមាន report គ្រប់ប្រភេទ |
| Bulk update | SQL ដោយមាន backup និង SELECT ពិនិត្យជាមុន | លឿនជាងកែម្ដងមួយ ប៉ុន្តែរំលង Service validation |
| Void/repair Sale | Maintenance SQL ដែលបានពិនិត្យ | App មិនមាន void/refund workflow |
| Backup/Restore | SQL Server backup tools | រក្សាទិន្នន័យទាំង database និងអាចស្ដារឡើងវិញ |
| Reset database | Scripts/Docker តែបន្ទាប់ពី backup | ជាសកម្មភាពលុបទិន្នន័យ |

> **ច្បាប់ងាយចាំ៖** បើ app អាចធ្វើការងារនោះបាន សូមប្រើ app។ ប្រើ direct SQL ពេលមានហេតុផលជាក់លាក់ និងត្រូវពិនិត្យ target rows មុន `UPDATE` ឬ `DELETE`។

---

## 1. គ្រប់គ្រងតាម app

Sign in ជា **Admin** ហើយជ្រើស nav buttons នៅ dashboard។

| ទិន្នន័យ | Screen | អ្វីដែលអាចធ្វើបាន |
|---|---|---|
| Products | Products | បង្កើត, កែ name, SKU, unit price, cost price, reorder level, category; លុប និង **Adjust Stock** |
| Categories | Categories | បង្កើត, ប្ដូរឈ្មោះ និងលុប |
| Users | Users | បង្កើត Admin/Cashier accounts, ប្ដូរ username/role, reset password, deactivate និងលុប |
| Sales | Sales History & Reports | **Read only**: filter តាម date range, មើល receipt lines, daily totals និង best sellers |
| Low Stock | Low Stock | មើល products ដែល stock តិចជាង ឬស្មើ reorder level |
| Sales ថ្មី | Point of Sale | បង្កើត sale តាម checkout ក្នុង app |

ពេល child window បិទ dashboard refresh ទិន្នន័យម្ដងទៀត។ វាមិនមែនជា live update ជាបន្តបន្ទាប់ពីគ្រប់ម៉ាស៊ីនទេ។

### 1.1 ឥរិយាបថសំខាន់ប្រាំ

**Adjust Stock កំណត់ចំនួនសរុបថ្មី។** បញ្ចូលចំនួនដែលមានពិតលើធ្នើ។ `InventoryService.AdjustStockAsync` ហៅ `Product.AdjustStockTo`។ បើមាន 20 ហើយទទួលថ្មី 12 ត្រូវបញ្ចូល 32; មុខងារនេះមិនបូក 12 ឱ្យដោយស្វ័យប្រវត្តិទេ។

**Stock មិនអាចអវិជ្ជមាន។** មានការការពារនៅ model (`Product.ReduceStock` / `AdjustStockTo`), checkout SQL (`WHERE StockQuantity >= @Quantity`) និង database (`CHECK (StockQuantity >= 0)`)។ Database constraint នៅតែអនុវត្តសម្រាប់ direct SQL។

**Low Stock មានន័យថា `StockQuantity <= ReorderLevel`។** កំណត់ `ReorderLevel` ក្នុង Products screen ដើម្បីគ្រប់គ្រងលទ្ធផល Low Stock និង dashboard count។

**Delete ប្រែជា deactivate ពេលមាន sales history។** Product ឬ user ដែលមានក្នុង sale ត្រូវរក្សាទុកសម្រាប់ receipt references។ App ប្រាប់ថាបាន delete ឬ deactivate។ Category ដែលនៅមាន products មិនអាចលុបបាន ហើយ app ប្រាប់ចំនួន products ដែលនៅសល់។

**មិនអាចដក last active Admin បាន។** App រារាំង demote, deactivate ឬ delete បើនឹងធ្វើឱ្យ active Admin នៅសល់ 0។

### 1.2 មុខងារមិនទាន់មាន៖ void និង refund

`ISaleRepository` មាន write method តែ `SaveSaleAsync`។ App មិនមាន void, refund, កែ sale lines ឬ delete receipt តាម UI ទេ។ Sales ត្រូវបានបន្ថែមហើយអានតាម app។

ការកែ sale ដែល scan ខុសត្រូវការការងារ maintenance ជាក់លាក់។ មើល [§4.1](#41-voiding-a-sale-takes-two-steps) សម្រាប់ឧទាហរណ៍ SQL និងផលប៉ះពាល់។

### 1.3 Password rules

Application password ត្រូវមានយ៉ាងហោចណាស់ **6 characters** (`User.MinimumPasswordLength`)។ App មិនកំណត់ complexity rule បន្ថែមទេ។ នេះជាច្បាប់របស់ app account មិនមែន SA account របស់ SQL Server។

Password រក្សាជា salted **PBKDF2-HMAC-SHA256 hash**, 100,000 iterations ក្នុង format `PBKDF2$100000$<base64 salt>$<base64 hash>`។ អាច verify ប៉ុន្តែមិនអាចទាញ password ដើមវិញ។ ក្នុង Users edit dialog បញ្ចូល password ថ្មីដើម្បី reset; ទុក field ទទេដើម្បីរក្សា password ចាស់។

---

## 2. ចូល database ដោយផ្ទាល់

### 2.1 dbgate ក្នុង Docker stack

ពេល Docker stack ដំណើរការ **dbgate** មាន connection ទៅ SQL Server រៀបចំរួច៖

```
http://localhost:3033
```

អាច browse tables, កែ rows ក្នុង grid, run SQL និង export results។ ត្រូវជ្រើស database `MiniMartDB`។

### 2.2 ប្រើ SSMS ឬ database client

ភ្ជាប់ទៅ `localhost,1434` ដោយ SQL Server authentication, user `sa` និង password ក្នុង `docker-compose.yml`។ សម្រាប់ local setup ជ្រើស **Trust server certificate** តាម configuration។ បើមិនប្រើ Docker ប្រើ server name ក្នុង README §3.2 និង authentication ដែលបានកំណត់។

### 2.3 ប្រើ sqlcmd ពី terminal

```bash
sqlcmd -S localhost,1434 -U sa -P 'Heak020507#' -d MiniMartDB -Q "SELECT TOP 20 * FROM dbo.Sales ORDER BY SaleId DESC;"
```

ជំនួស development password ក្នុង examples ដោយ password របស់អ្នក។ Flags: `-s","` កំណត់ column separator, `-W` ដក whitespace នៅចុង, `-h-1` ដក headers និង `-o file.csv` សរសេរទៅ file៖

```bash
sqlcmd -S localhost,1434 -U sa -P 'Heak020507#' -d MiniMartDB -s"," -W -h-1 -Q "SELECT SaleId, SaleDate, TotalAmount FROM dbo.Sales;" -o sales.csv
```

នេះជា text export សាមញ្ញ។ សម្រាប់ data ដែលមាន comma, quote ឬ newline ត្រូវប្រើ CSV exporter ដែលគាំទ្រ escaping ត្រឹមត្រូវ។

---

## 3. Queries បន្ថែមដែល app មិនផ្ដល់

### 3.1 គណនាតម្លៃ inventory

Query នេះគណនា stock តាម cost price, retail price និង potential margin សម្រាប់ active products៖

```sql
SELECT SUM(StockQuantity * CostPrice) AS AtCost,
       SUM(StockQuantity * UnitPrice) AS AtRetail,
       SUM(StockQuantity * (UnitPrice - CostPrice)) AS PotentialMargin
FROM dbo.Products
WHERE IsActive = 1;
```

### 3.2 គណនា margin ប្រហែលតាម product

App បង្ហាញ revenue ប៉ុន្តែមិនមាន margin report។ `CostPrice` មានរក្សាទុក ហើយអាចប្រើគណនាបាន៖

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

**នេះមិនមែនជា historical net profit ពិតប្រាកដទេ។** `sd.UnitPrice` ជាតម្លៃពេលលក់ ខណៈ `p.CostPrice` ជាតម្លៃ cost បច្ចុប្បន្ន។ Query ក៏មិនដក cart-level discounts ឬចំណាយផ្សេងទៀត។ ដូច្នេះ `GrossProfit` ក្នុងលទ្ធផលគឺការប៉ាន់ស្មានមុន discount ដោយប្រើ current cost។

### 3.3 ចំណូលតាម Cashier

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

### 3.4 Sales តាមម៉ោងសម្រាប់រៀបចំបុគ្គលិក

```sql
SELECT DATEPART(HOUR, SaleDate) AS Hour,
       COUNT(*)                 AS Sales,
       SUM(TotalAmount)         AS Revenue
FROM dbo.Sales
GROUP BY DATEPART(HOUR, SaleDate)
ORDER BY Hour;
```

Query ប្រមូលតាមម៉ោងនៅគ្រប់ថ្ងៃក្នុងទិន្នន័យ។ បើចង់ពិនិត្យរយៈពេលជាក់លាក់ បន្ថែម date filter។

### 3.5 Products ដែលមិនធ្លាប់លក់

```sql
SELECT p.Name, p.SKU, p.StockQuantity
FROM dbo.Products p
WHERE p.IsActive = 1
  AND NOT EXISTS (SELECT 1 FROM dbo.SaleDetails sd WHERE sd.ProductId = p.ProductId);
```

### 3.6 ទិន្នន័យ receipt មួយ

ឧទាហរណ៍សម្រាប់ `SaleId = 57`; ប្ដូរ ID តាម sale ដែលត្រូវពិនិត្យ៖

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

បង្កើន unit price 5% សម្រាប់ category មួយ៖

```sql
UPDATE p
SET UnitPrice = ROUND(p.UnitPrice * 1.05, 2)
FROM dbo.Products p
JOIN dbo.Categories c ON c.CategoryId = p.CategoryId
WHERE c.Name = N'Beverages';
```

កំណត់ reorder level ទៅ 10 សម្រាប់ products ដែលនៅមានតម្លៃ 5៖

```sql
UPDATE dbo.Products SET ReorderLevel = 10 WHERE ReorderLevel = 5;
```

មុន execute bulk UPDATE ត្រូវពិនិត្យ rows ដោយ SELECT និង filter ដូចគ្នា។ Bulk import ពី staging table ឬ `VALUES` list ត្រូវគោរព `UQ_Products_SKU` និងប្រើ `CategoryId` ដែលមានពិត។

---

## 4. កែទិន្នន័យដោយដៃ៖ ចំណុចត្រូវយល់

<a id="41-voiding-a-sale-takes-two-steps"></a>

### 4.1 Void sale ត្រូវដោះស្រាយទាំង stock និង receipt

ការលុប row ក្នុង `Sales` នឹងលុប `SaleDetails` តាម `ON DELETE CASCADE` ប៉ុន្តែ **មិនបន្ថែម stock ត្រឡប់វិញ**។ បើការកែនេះមានន័យថាទំនិញត្រឡប់ចូល stock វាត្រូវធ្វើទាំងពីរក្នុង transaction។ ឧទាហរណ៍ដើមខាងក្រោមសម្រាប់ maintenance ដែលគ្មាន checkout ឬការកែទិន្នន័យដំណាលគ្នា និងមានមួយ line ក្នុងមួយ product៖

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

ពិនិត្យ `SaleId`, quantities និងទំនិញដែលត្រឡប់ពិតប្រាកដជាមុន។ រក្សា backup និងកំណត់ត្រាការកែ ព្រោះ schema គ្មាន audit table ហើយ deleted sale មិននៅក្នុង history ទៀត។ ឧទាហរណ៍នេះមិនមែនជា refund workflow ពេញលេញ ឬ script សម្រាប់ multi-user production maintenance ទេ។

ការរក្សា history ដោយប្រើ reversal/refund record ត្រូវការការរចនាបន្ថែម។ Schema បច្ចុប្បន្នមាន `CHECK (Quantity > 0)` ដូច្នេះមិនអាចបញ្ចូល negative-quantity compensating sale ដោយផ្ទាល់បានទេ។

### 4.2 កុំវាយ password ទៅក្នុង `Users.PasswordHash`

`PasswordHash` ត្រូវមាន format ដែល `Pbkdf2PasswordHasher` អាច verify បាន។ Hash ដែលខូចអាចធ្វើឱ្យ `Verify` ត្រឡប់ `false` ហើយ account sign in មិនបាន។ ប្រើ **Users** screen ដើម្បី reset password។

ឧទាហរណ៍ដើមខាងក្រោមគឺការលុប user row ឈ្មោះ `admin` ប៉ុណ្ណោះ៖

```sql
DELETE FROM dbo.Users WHERE Username = 'admin';
```

**កុំចាត់ទុកវាជាវិធី recovery ទូទៅ។** First-Time Setup បើកតែពេល `Users` table ទទេទាំងស្រុង។ បើនៅមាន users ផ្សេង ការលុប ឬ deactivate Admin មិនបើក setup ទេ។ បើ Admin មាន sales history foreign key នឹងរារាំងការលុប។ ប្រើ active Admin ផ្សេងដើម្បី reset password; បើគ្មាន ត្រូវការការងារ recovery ដែលបង្កើត valid hash ត្រឹមត្រូវ ឬ restore backup ដែលអាចប្រើបាន ដោយរក្សា user IDs និង sales references។

### 4.3 Direct stock edit រំលង model validation

Database `CHECK (StockQuantity >= 0)` នៅតែដំណើរការ ប៉ុន្តែ `Product.AdjustStockTo` មិនត្រូវបានហៅទេ។ ប្រើ **Adjust Stock** សម្រាប់ការងារប្រចាំថ្ងៃ និង SQL សម្រាប់ bulk corrections ដែលបានពិនិត្យ។

### 4.4 Deactivate និង delete ខុសគ្នា

`IsActive = 0` ធ្វើឱ្យ product មិនអាច scan នៅ POS (`GetBySkuAsync` មាន `AND p.IsActive = 1`) និងមិនរាប់ក្នុង active product count។ Row នៅតែអាច reference ក្នុង sales history។ Products ដែលធ្លាប់លក់មិនអាច hard delete ព្រោះមាន foreign key references។

---

## 5. Backup និង restore

Docker រក្សាទិន្នន័យក្នុង `sql_data` volume។ `docker compose down` និង `docker compose restart` មិនលុបវា ប៉ុន្តែ **`docker compose down -v` លុប volumes និងទិន្នន័យក្នុងនោះ**។ Volume មិនមែនជា backup ជំនួសទេ។

### 5.1 បង្កើត backup

```bash
sqlcmd -S localhost,1434 -U sa -P 'Heak020507#' -Q "BACKUP DATABASE MiniMartDB TO DISK='/var/opt/mssql/data/MiniMartDB.bak' WITH INIT, COMPRESSION;"
```

Path ខាងលើនៅ **ក្នុង container**។ បង្កើត folder `backups` លើ host ជាមុន រួច copy file ចេញ៖

```bash
docker cp sql1:/var/opt/mssql/data/MiniMartDB.bak ./backups/MiniMartDB.bak
```

សម្រាប់ non-Docker instance ប្រើ path ដែល SQL Server service account មាន permission សរសេរ ដូចជា `TO DISK='C:\Backups\MiniMartDB.bak'`។ `WITH INIT` សរសេរជំនួស backup file ដូច្នេះគួររក្សា copies មានកាលបរិច្ឆេទ ដើម្បីមានច្រើន versions។

### 5.2 Restore ពី backup

Copy backup ចូល container៖

```bash
docker cp ./backups/MiniMartDB.bak sql1:/var/opt/mssql/data/MiniMartDB.bak
```

បន្ទាប់មក restore លើ database ដែលមានស្រាប់៖

```bash
sqlcmd -S localhost,1434 -U sa -P 'Heak020507#' -Q "ALTER DATABASE MiniMartDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE; RESTORE DATABASE MiniMartDB FROM DISK='/var/opt/mssql/data/MiniMartDB.bak' WITH REPLACE; ALTER DATABASE MiniMartDB SET MULTI_USER;"
```

បិទ app និង connections ផ្សេងជាមុន។ `SINGLE_USER WITH ROLLBACK IMMEDIATE` បញ្ចប់ connections/transactions ដែលកំពុងប្រើ database។ **`WITH REPLACE` សរសេរជំនួស database បច្ចុប្បន្ន** ដូច្នេះការផ្លាស់ប្ដូរក្រោយ backup នឹងមិននៅសល់។ បើ restore បរាជ័យ ពិនិត្យ database state និងស្ដារ `MULTI_USER` តាមសមស្រប។

### 5.3 ត្រូវ backup អ្វី និងពេលណា?

Backup **database ទាំងមូល** រួមទាំង tables ទាំង 5។ Sample seed script មិនអាចស្ដារ products ដែលអ្នកបន្ថែម, prices ដែលកែ, stock ពិត ឬ user accounts ដែលមានស្រាប់បានទេ។

អាចចាប់ផ្ដើមពី daily full backup ហើយកែ frequency តាមបរិមាណទិន្នន័យដែលអាចទទួលយកការបាត់បង់បាន។ រក្សា copy នៅទីតាំងដាច់ពីម៉ាស៊ីន database និងសាកល្បង restore ទៅ test database។

---

## 6. Reset ទិន្នន័យ

**បន្ថែម sample catalogue ដែលខ្វះ ដោយមិនលុប data ផ្សេង៖** Seed script ប្រើ `NOT EXISTS` ដើម្បីជៀស duplicate inserts។ វាមិន reset prices ឬ stock របស់ rows ដែលមានស្រាប់ទេ។

```bash
sqlcmd -S localhost,1434 -U sa -P 'Heak020507#' -b -i MiniMart_SeedData.sql
```

**លុប sales history ដោយរក្សា products និង users៖**

```sql
DELETE FROM dbo.Sales;   -- SaleDetails cascade
```

វាលុប sale lines តាម cascade ហើយ **មិនស្ដារ stock**។ ក្រោយលុបត្រូវធ្វើ stock-take និងកែ quantities ដោយចេតនា។ ការ run seed script ម្ដងទៀតមិន reset existing stock ទេ។

**Reset tables ទាំងអស់៖** Schema script មាន DROP/CREATE ហើយនឹងបាត់ទិន្នន័យ។ ដោយសារវា drop `Categories` មុន `Products` ត្រូវ drop តាម foreign-key order ជាមុន៖

```sql
USE MiniMartDB;
IF OBJECT_ID('dbo.SaleDetails','U') IS NOT NULL DROP TABLE dbo.SaleDetails;
IF OBJECT_ID('dbo.Sales','U')       IS NOT NULL DROP TABLE dbo.Sales;
IF OBJECT_ID('dbo.Products','U')    IS NOT NULL DROP TABLE dbo.Products;
IF OBJECT_ID('dbo.Users','U')       IS NOT NULL DROP TABLE dbo.Users;
IF OBJECT_ID('dbo.Categories','U')  IS NOT NULL DROP TABLE dbo.Categories;
```

រួច run schema script, run seed script និងបើក app ដើម្បីបង្កើត Admin ថ្មី។

**Reset Docker volumes ទាំងស្រុង៖** `docker compose down -v` រួច `docker compose up -d` បង្កើត server ទទេថ្មី។ វាលុប named volumes របស់ stack រួមទាំង database និង dbgate data។ ត្រូវ run scripts ឡើងវិញ។

---

## 7. Constraints ដែល database អនុវត្តជានិច្ច

Constraints ទាំងនេះអនុវត្តទោះកែពី app, SQL ឬ dbgate grid ក៏ដោយ៖

| Rule | Constraint |
|---|---|
| Category names ត្រូវ unique | `UQ_Categories_Name` |
| SKUs ត្រូវ unique | `UQ_Products_SKU` |
| Usernames ត្រូវ unique | `UQ_Users_Username` |
| Role ជា `Admin` ឬ `Cashier` | `CK_Users_Role` |
| Stock, prices, totals, paid និង change មិនអវិជ្ជមាន | `CK_Products_*`, `CK_Sales_*` |
| Sale line quantity ត្រូវ > 0 | `CK_SaleDetails_Quantity` |
| Product ត្រូវ reference category ដែលមានពិត | `FK_Products_Categories` |
| Sale ត្រូវ reference user ដែលមានពិត | `FK_Sales_Users` |
| លុប sale នឹងលុប lines | `FK_SaleDetails_Sales ... ON DELETE CASCADE` |
| Product ដែលមាន sale line reference មិនអាចលុប | `FK_SaleDetails_Products` គ្មាន cascade |

មើល definitions ពេញលេញក្នុង [MiniMart_Database_Schema.sql](MiniMart_Database_Schema.sql)។
