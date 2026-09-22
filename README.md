# Mart Management System

កម្មវិធី desktop POS និងការគ្រប់គ្រង stock ដែលសរសេរដោយ C# **Windows Forms**។ គម្រោងនេះប្រើ **3-Tier Architecture**, **ADO.NET** និង **SQL Server** ដោយផ្អែកលើការរចនាក្នុង `Mini_Mart_Management_System_Documentation.docx` និង schema ក្នុង `MiniMart_Database_Schema.sql`។

ឯកសារនេះផ្តោតលើការ install, configure និង run កម្មវិធី។

## ចាប់ផ្ដើមប្រើប្រាស់រហ័ស

ត្រូវមាន Windows + [.NET SDK 9](https://dotnet.microsoft.com/download/dotnet/9.0) + Docker។ ដំណើរការ commands ខាងក្រោមពី repository root បន្ទាប់ពី SQL Server រួចរាល់៖

```bash
docker compose up -d
sqlcmd -S localhost,1434 -U sa -P 'Heak020507#' -b -i MiniMart_Database_Schema.sql
sqlcmd -S localhost,1434 -U sa -P 'Heak020507#' -b -i MiniMart_SeedData.sql
dotnet run --project src/MiniMart.Presentation
```

ពេលបើកដំបូង កម្មវិធីនឹងឱ្យអ្នកបង្កើត Admin account។ ផ្នែក §2–§5 មានការណែនាំលម្អិត រួមទាំងការប្រើ SQL Server ដែលមានស្រាប់ និងការប្ដូរ password។ Password ក្នុង command ខាងលើគឺសម្រាប់ development setup ដែលភ្ជាប់មកជាមួយគម្រោង ហើយគួរប្ដូរមុនប្រើប្រាស់ជាក់ស្ដែង។

លំហូរសង្ខេបគឺ៖

```text
Install prerequisites
  → Start SQL Server
  → Run schema script
  → Run seed script (optional)
  → Configure App.config
  → Build and run
  → Create the first Admin account
```

---

## 1. រចនាសម្ព័ន្ធ Solution

```
MiniMartManagementSystem.sln
└── src/
    ├── MiniMart.BusinessLogic/     net9.0        — domain models, services, contracts
    ├── MiniMart.DataAccess/        net9.0        — ADO.NET repository implementations
    └── MiniMart.Presentation/      net9.0-windows — Windows Forms UI
```

### ទំនាក់ទំនងរវាង projects

```
  Presentation  ──>  BusinessLogic
       │                  ▲
       └──> DataAccess ───┘
```

- `MiniMart.BusinessLogic` មាន `Models/` (Product, Category, CartItem, Cart, Sale, SaleDetail, User), `Repositories/` (contracts `I*Repository`), `Services/`, `Discounts/`, `Security/`, `Exceptions/` និង `Reporting/`។ **មិនប្រើ ADO.NET ឬ WinForms ទេ។**
- `MiniMart.DataAccess` មាន `ProductRepository`, `SaleRepository`, `CategoryRepository`, `UserRepository` និង connection factory។ **មិនមាន WinForms ទេ។**
- `MiniMart.Presentation` មាន forms សម្រាប់ UI។ មិនសរសេរ SQL ឬគ្រប់គ្រង transaction ក្នុង forms ទេ។

> **ហេតុអ្វី repository interfaces នៅក្នុង `BusinessLogic`?** `SaleService` ត្រូវការ `ISaleRepository` ហើយ `SaleRepository` ត្រូវការ `Sale`។ បើដាក់ interfaces ក្នុង `DataAccess` នឹងបង្កើត circular dependency។ ការដាក់ contracts ក្នុង `BusinessLogic` ជាវិធី **Dependency Inversion** ដែលរក្សា 3 layers ហើយឱ្យ Business Logic Layer ពឹងផ្អែកតែលើ abstractions ស្របតាម Chapter 2.3.2។

---

## 2. ដំឡើង prerequisites

| អ្វីដែលត្រូវមាន | គោលបំណង | របៀបពិនិត្យ |
|---|---|---|
| **Windows 10/11** | Presentation ប្រើ `net9.0-windows` និង Windows Forms; កម្មវិធីនេះមិនដំណើរការលើ Linux ឬ macOS ទេ | — |
| **.NET SDK 9.0** ឬ SDK ដែលអាច build target នេះបាន — [Download](https://dotnet.microsoft.com/download/dotnet/9.0) | Build និង run projects ទាំងបី | `dotnet --version` |
| **SQL Server** — Docker, Express, Developer ឬ LocalDB | រក្សាទុកទិន្នន័យ | មើល §3 |
| **Git** (មិនចាំបាច់) | Clone repository | `git --version` |
| **`sqlcmd`** (មិនចាំបាច់) | ដំណើរការ `.sql` scripts ពី terminal; អាចប្រើ GUI ក្នុង §3.3 ជំនួស | `sqlcmd -?` |

អាចប្រើ Visual Studio 2022 (17.12+) ជាមួយ workload **.NET desktop development**។ បើមិនប្រើ Visual Studio ទេ `dotnet` CLI គ្រប់គ្រាន់សម្រាប់ជំហានខាងក្រោម។

### 2.1 ទាញយក source code

```bash
git clone <repository-url> "Mart Management System"
```

ជំនួស `<repository-url>` ដោយ URL របស់ repository រួចប្រើ `cd` ចូល folder ដែលមាន `MiniMartManagementSystem.sln`។ Commands ក្នុង README នេះដំណើរការពី folder នោះ។ អ្នកក៏អាច Download និង extract ZIP បានដែរ។

---

## 3. រៀបចំ database

ជ្រើសរើស **មួយ** ក្នុងចំណោមជម្រើសខាងក្រោម រួចបន្តទៅ §3.3 ដើម្បីបង្កើត tables។

### 3.1 ជម្រើស A — SQL Server ក្នុង Docker

[docker-compose.yml](docker-compose.yml) នៅ repository root រៀបចំ SQL Server 2022 និង **dbgate** ដែលជា database client ប្រើតាម browser។ ត្រូវឱ្យ Docker ដំណើរការជាមុន។

```bash
docker compose up -d
```

រង់ចាំឱ្យ server មាន status `healthy`។ ការចាប់ផ្ដើមដំបូងអាចចំណាយប្រហែល 30 វិនាទី ឬយូរជាងនេះ៖

```bash
docker compose ps
```

| Service | Host endpoint | Credentials |
|---|---|---|
| SQL Server 2022 | `localhost,1434` | `sa` / តម្លៃ `MSSQL_SA_PASSWORD` ក្នុង compose file |
| dbgate (web UI) | http://localhost:3033 | connection `sql1` បានរៀបចំរួច |

> **ប្ដូរ SA password មុនប្រើប្រាស់ជាក់ស្ដែង។** Compose file មាន development password ជា plain text។ កំណត់ `MSSQL_SA_PASSWORD` ក្នុង `docker-compose.yml` **មុន `docker compose up` លើកដំបូង** ហើយប្រើ password ដូចគ្នាក្នុង `App.config` (§4) និង dbgate connection configuration។ Password ត្រូវបំពេញ complexity rules របស់ SQL Server; password ខ្លី ឬសាមញ្ញពេកអាចធ្វើឱ្យ container មិនចាប់ផ្ដើម។
>
> ការប្ដូរ environment variable នេះក្រោយពេលបង្កើត `sql_data` volume មិន reset password ដែលមានស្រាប់ទេ។ `docker compose down -v` រួច `docker compose up -d` អាចចាប់ផ្ដើមថ្មី ប៉ុន្តែ **វាលុបទិន្នន័យក្នុង volumes**។

> **ហេតុអ្វីប្រើ port 1434?** Setup ដើមមាន local SQL Server 2014 ដែលប្រើ port 1433 រួច។ Mapping `1434:1433` ជៀសវាងការប៉ះទង្គិចនេះ។ បើ port 1433 ទំនេរ អាចប្ដូរទៅ `"1433:1433"` ហើយប្រើ `Server=localhost` ក្នុង `App.config`។ dbgate នៅតែភ្ជាប់ទៅ `sqlserver:1433` តាម internal `sqlnet` bridge។

### 3.2 ជម្រើស B — SQL Server instance ដែលមានស្រាប់

បើមាន SQL Server, LocalDB ឬ Express រួច អាចរំលង Docker ហើយប្រើ server name ដែលសមស្រប៖

| Setup | Server name |
|---|---|
| Local default instance | `.` ឬ `localhost` |
| SQL Server Express | `.\SQLEXPRESS` |
| LocalDB ជាមួយ Visual Studio | `(localdb)\MSSQLLocalDB` |
| Server ក្នុង network | `hostname,port` |

Windows account ដែលប្រើត្រូវមាន permission បង្កើត database។

### 3.3 បង្កើត tables និងបញ្ចូល sample data

ដំណើរការ scripts **តាមលំដាប់នេះ**៖ `MiniMart_Database_Schema.sql` បង្កើត `MiniMartDB` និង tables ចំនួន 5; `MiniMart_SeedData.sql` បញ្ចូល sample catalogue។

**ប្រើ `sqlcmd` ជាមួយ Docker (ជម្រើស A)៖** ជំនួស `YourPassword` ដោយ password ដែលបានកំណត់។

```bash
sqlcmd -S localhost,1434 -U sa -P 'YourPassword' -b -i MiniMart_Database_Schema.sql
```

```bash
sqlcmd -S localhost,1434 -U sa -P 'YourPassword' -b -i MiniMart_SeedData.sql
```

**ប្រើ local instance ជាមួយ Windows authentication (ជម្រើស B)៖**

```bash
sqlcmd -S . -E -b -i MiniMart_Database_Schema.sql
```

```bash
sqlcmd -S . -E -b -i MiniMart_SeedData.sql
```

ប្ដូរ `-S .` ទៅ `-S .\SQLEXPRESS` ឬ `-S '(localdb)\MSSQLLocalDB'` តាម setup។

**បើមិនមាន `sqlcmd`៖** បើក dbgate នៅ http://localhost:3033 ឬ SQL Server Management Studio (SSMS) រួចបើក និង execute `MiniMart_Database_Schema.sql` មុន `MiniMart_SeedData.sql`។

**ពិនិត្យលទ្ធផល៖** Query នេះគួរបង្ហាញ tables ចំនួន 5។

```bash
sqlcmd -S localhost,1434 -U sa -P 'YourPassword' -Q "USE MiniMartDB; SELECT name FROM sys.tables ORDER BY name;"
```

Seed script ជាជម្រើសបន្ថែម៖ វាបញ្ចូល **6 categories និង 24 products**។ ក្នុងនោះ 5 products មាន stock ទាបជាង reorder level ដើម្បីបង្ហាញនៅ Low Stock។ អាច run seed script ម្ដងទៀតបាន ព្រោះ insert នីមួយៗមាន `NOT EXISTS`។ វា **មិនបង្កើត user accounts** ទេ; អ្នកបង្កើត Admin ពេលបើកកម្មវិធីដំបូង (§5.1)។

> **កុំ run schema script ឡើងវិញលើ database ដែលមានទិន្នន័យ ដោយមិនមាន backup។** Script ព្យាយាម drop `Categories` មុន `Products` ដូច្នេះ foreign key នឹងរារាំង។ បើចង់ reset ពិតប្រាកដ ត្រូវ drop តាម dependency order ខាងក្រោមសិន។ **វាលុបទិន្នន័យទាំងអស់ក្នុង tables ទាំងនេះ។**
>
> ```sql
> USE MiniMartDB;
> IF OBJECT_ID('dbo.SaleDetails','U') IS NOT NULL DROP TABLE dbo.SaleDetails;
> IF OBJECT_ID('dbo.Sales','U')       IS NOT NULL DROP TABLE dbo.Sales;
> IF OBJECT_ID('dbo.Products','U')    IS NOT NULL DROP TABLE dbo.Products;
> IF OBJECT_ID('dbo.Users','U')       IS NOT NULL DROP TABLE dbo.Users;
> IF OBJECT_ID('dbo.Categories','U')  IS NOT NULL DROP TABLE dbo.Categories;
> ```

---

## 4. កំណត់ database connection របស់កម្មវិធី

កែ `MiniMartDb` connection string ក្នុង **[src/MiniMart.Presentation/App.config](src/MiniMart.Presentation/App.config)** ឱ្យត្រូវនឹង database setup របស់អ្នក៖

```xml
<connectionStrings>
  <add name="MiniMartDb"
       connectionString="Server=localhost,1434;Database=MiniMartDB;User ID=sa;Password=YourPassword;TrustServerCertificate=True;Application Name=MiniMartManagementSystem;Connect Timeout=15"
       providerName="Microsoft.Data.SqlClient" />
</connectionStrings>
```

តម្លៃ `Server=` និង authentication ផ្លាស់ប្ដូរតាម setup៖

| Target | តម្លៃ `Server=` | Authentication |
|---|---|---|
| Docker container របស់គម្រោង | `localhost,1434` | `User ID=sa;Password=YourPassword` |
| Local default instance | `.` | `Trusted_Connection=True` |
| SQL Server Express | `.\SQLEXPRESS` | `Trusted_Connection=True` |
| LocalDB | `(localdb)\MSSQLLocalDB` | `Trusted_Connection=True` |

`App.config` មាន connection string សម្រាប់ local instance ជា comment ខាងក្រោម active connection។ អាចប្ដូរមួយណាដែល active តាម setup របស់អ្នក។

`TrustServerCertificate=True` អនុញ្ញាតឱ្យ local/container setup ប្រើ self-signed certificate ខណៈ `Microsoft.Data.SqlClient` ប្រើ encrypted connection។

បើ configuration បាត់ ឬមិនត្រឹមត្រូវ កម្មវិធីបង្ហាញ error message។ កំហុស connection ទៅ server អាចកើតឡើងនៅពេល database call ដំបូង។ ក្រោយ deployment តម្លៃនេះនៅក្នុង `MiniMart.Presentation.dll.config` ក្បែរ executable ហើយអាចកែ server ដោយមិន rebuild។

ក្នុង `App.config` ក៏អាចកែ store name, address, phone លើ receipt និង currency symbol ក្នុង UI បានដែរ។

---

## 5. Build និង run

```bash
dotnet build MiniMartManagementSystem.sln
```

```bash
dotnet run --project src/MiniMart.Presentation
```

ឬបើក `MiniMartManagementSystem.sln` ក្នុង Visual Studio កំណត់ **MiniMart.Presentation** ជា startup project ហើយចុច <kbd>F5</kbd>។

Sign-in window គួរបង្ហាញជាមួយ server name។ ដើម្បី publish សម្រាប់ Windows x64៖

```bash
dotnet publish src/MiniMart.Presentation -c Release -r win-x64 --self-contained false -o publish
```

Command នេះប្រើ `--self-contained false` ដូច្នេះម៉ាស៊ីនគោលដៅត្រូវមាន .NET Desktop Runtime ដែលសមស្រប។

### 5.1 បើកដំបូង — បង្កើត Admin

កម្មវិធី **មិនមាន default application account** ទេ។ បើ `Users` table ទទេ វាបើក **First-Time Setup** ឱ្យអ្នកកំណត់ username និង password របស់ Admin។ នេះខុសពី SA credentials របស់ database ក្នុង compose file។

Password របស់ app រក្សាជា salted **PBKDF2-HMAC-SHA256 hash** ជាមួយ 100,000 iterations។ ប្រព័ន្ធអាច verify password ប៉ុន្តែមិនអាចអាន password ដើមពី hash បាន។

Sign in ជា Admin ដើម្បីចូល dashboard។ បើក **Users** ដើម្បីបង្កើត Cashier accounts។ Cashier ចូលទៅ POS ដោយផ្ទាល់។

ឧទាហរណ៍ខាងក្រោមលុប account ឈ្មោះ `admin`៖

```sql
USE MiniMartDB; DELETE FROM dbo.Users WHERE Username = 'admin';
```

**First-Time Setup បើកតែពេល `Users` ទទេទាំងស្រុង។** ការលុប Admin មួយមិនបើក setup ឡើងវិញទេ បើនៅមាន users ផ្សេង។ បើ account នោះមាន sales history នោះ foreign key នឹងរារាំងការលុប។ សម្រាប់ការគ្រប់គ្រង accounts ជាប្រចាំ សូមប្រើ Users screen។

### 5.2 ដោះស្រាយបញ្ហា

| បញ្ហា | មូលហេតុ និងដំណោះស្រាយ |
|---|---|
| "The application is not configured correctly" | បាត់ `MiniMartDb` ក្នុង `App.config`; មើល §4 |
| Network/instance error របស់ SQL Server | ពិនិត្យ `docker compose ps`, SQL Server service, server name និង TCP/IP configuration |
| "Login failed for user 'sa'" | Password ក្នុង `App.config` មិនត្រូវនឹង password ក្នុង database volume; ប្រើ password ត្រឹមត្រូវ។ `docker compose down -v` លុបទិន្នន័យទាំងអស់ក្នុង volumes |
| "Cannot open database 'MiniMartDB'" | មិនទាន់ run schema script; មើល §3.3 |
| Certificate/trust error | ពិនិត្យ `TrustServerCertificate=True` សម្រាប់ local setup |
| Container ចាប់ផ្ដើមហើយបិទ | ពិនិត្យ password complexity និង `docker compose logs sqlserver` |
| `sqlcmd: command not found` | ប្រើ dbgate ឬ SSMS ឬដំឡើង SQL Server command-line tools |
| Port 1434 ត្រូវបានប្រើរួច | ប្ដូរខាងឆ្វេងនៃ `"1434:1433"` ហើយកែ port ក្នុង `App.config` ឱ្យត្រូវគ្នា |
| `dotnet build` បរាជ័យលើ `net9.0-windows` | ពិនិត្យ Windows និង SDK ដោយ `dotnet --list-sdks` |
| Low Stock ទទេ | អាចមិនមាន products ដែល stock ដល់ reorder level ឬមិនទាន់បញ្ចូល seed data |

---

## 6. Screens

**Sign-in** បែងចែកតាម role៖ `Admin` → dashboard; `Cashier` → POS។

| Screen | Role | មុខងារ |
|---|---|---|
| Point of Sale | Cashier, Admin | Scan/search, cart, discounts, payment, receipt; `F9` checkout, `F2` search |
| Dashboard | Admin | Sales/revenue ថ្ងៃនេះ, low-stock count, recent sales |
| Products | Admin | CRUD, Adjust Stock និងសម្គាល់ low stock |
| Categories | Admin | CRUD |
| Users | Admin | CRUD សម្រាប់ Admin/Cashier accounts |
| Sales History & Reports | Admin | Transactions, sale lines, daily totals, best sellers |
| Low Stock | Admin | Products ដែល stock តិចជាង ឬស្មើ reorder level |

នៅ POS វាយ code ហើយចុច **Enter** ដើម្បីបន្ថែមទៅ cart ឬ search តាម name ហើយ double-click row។

---

## 7. ទំនាក់ទំនងរវាង specification និង code

| Requirement | កន្លែងអនុវត្ត |
|---|---|
| Encapsulation — stock មិនអវិជ្ជមាន | `Product.ReduceStock` / `AdjustStockTo`; `StockQuantity` គ្មាន public setter |
| Abstraction — BLL ពឹងលើ contracts | `BusinessLogic/Repositories/I*Repository.cs` |
| Polymorphism — discount strategies | `Discounts/DiscountStrategy`, `PercentageDiscount`, `FlatDiscount`, `NoDiscount` ប្រើដោយ `SaleService` |
| Parameterized queries (Ch. 5) | `SqlRepositoryBase` ប្រើ SQL template និង parameter-binding delegate |
| Transactional checkout (Ch. 6.2) | `SaleRepository.SaveSaleAsync`: insert header, insert lines, guarded stock UPDATE ក្នុង `SqlTransaction`; 0 rows affected → rollback |
| Thin event handlers (Ch. 6.3) | `PosForm` ហៅ services; total មកពី `SaleService.QuoteCart` |
| Business និង infrastructure errors (Ch. 6.3) | `UiFeedback.ShowError` បង្ហាញ `BusinessRuleException` តាម message និង `SqlException` ជា generic message |
| Async data access | Repository methods ផ្ដល់ async operations; UI await តាម `AsyncUi.RunAsync` |
| Composition root | `AppServices` បង្កើត concrete repositories និងភ្ជាប់ dependencies |

### ការទប់ស្កាត់ race condition

`SaleService` អាន stock ម្ដងទៀតមុន save ដើម្បីផ្ដល់ error message ងាយយល់។ ការធានាចុងក្រោយគឺ guarded `UPDATE` ក្នុង transaction។ បើ POS ពីរលក់ unit ចុងក្រោយពេលដំណាលគ្នា មួយអាចបាន 0 rows affected ហើយ sale នោះនឹង rollback ទាំង header, lines និង stock ដែលបានដក។

---

## 8. ការសម្រេចចិត្តបន្ថែមពី specification

1. ប្រើ **.NET 9 / `Microsoft.Data.SqlClient`** ជាមួយ ADO.NET: `SqlConnection`, `SqlCommand`, `SqlParameter`, `SqlTransaction`, `SqlDataReader`។
2. ដាក់ repository interfaces ក្នុង `BusinessLogic` ដើម្បីជៀសវាង circular dependency (§1)។
3. Password hashing ប្រើ **PBKDF2-HMAC-SHA256**, 100k iterations, 16-byte random salt និង format `PBKDF2$iterations$salt$hash` ប្រហែល 83 characters ដែលអាចដាក់ក្នុង `NVARCHAR(256)`។
4. ប្រើ first-run setup សម្រាប់ application accounts ជំនួស default credentials។
5. Admin អាចបើក POS ដើម្បីជួយ Cashier ដោយមិនត្រូវការ account ទីពីរ។
6. Products និង users ដែលមាន sales history ប្រើ soft delete ដើម្បីរក្សាទំនាក់ទំនងក្នុង receipts។
7. Discount អនុវត្តលើ cart; `Sales` គ្មាន discount column។ `TotalAmount` រក្សាតម្លៃក្រោយ discount ហើយ receipt គណនា discount ពី `Subtotal − TotalAmount`។
8. គ្មាន `Customers` table ព្រោះ specification កំណត់ជាជម្រើស ហើយ schema មិនមាន។
9. មិនគណនា tax ព្រោះគ្មាន tax column ឬ rate ដែលបានកំណត់។
10. មិនប្រើ stored procedures; ប្រើ parameterization នៅ data access។
11. `LoginForm` និង `PosForm` ប្រើ `.Designer.cs`; forms ផ្សេងបង្កើត controls ក្នុង `BuildUi()` ហើយប្រើ `UiTheme` រួម។

---

## 9. កំណត់ត្រា verification ពីមុន

ឯកសារដើមកត់ត្រាថា solution build បាន **0 warnings, 0 errors** ហើយ temporary harness លើ SQL Server 2014 ឆ្លងកាត់ **46 assertions**៖

- Password hashing/verification និងការបដិសេធ wrong password ឬ duplicate username។
- SQL-injection payload (`x'; DROP TABLE dbo.Products; --`) ត្រូវបានចាត់ទុកជា data។
- Category/product CRUD និងការបដិសេធ duplicate SKU/name។
- `Product.ReduceStock` រារាំង stock អវិជ្ជមាន។
- Discount strategies ទាំងបី រួមទាំង FlatDiscount ដែលមិនឱ្យ total ក្រោម 0។
- Checkout រក្សា sale ហើយបន្ថយ stock 100→97 និង 50→48។
- Oversell បង្កើត `InsufficientStockException` ហើយ rollback header និង stock updates ទាំងអស់ក្នុង transaction។
- Reporting, line drill-down, daily totals និង best-seller ranking។
- Category-in-use, soft delete និងការរក្សា last active Admin។

កំណត់ត្រាដើមក៏បញ្ជាក់ការសាកល្បង search ដែល escape `%` និង `_` និងការបើក sign-in, first-run setup និង Admin dashboard។ ក្រោយប្ដូរទៅ SQL Server 2022 Docker container មាន **13/13 assertions** ឆ្លងកាត់សម្រាប់ connection, seed catalogue, authentication, checkout, rollback និង reporting មុន reset ទៅ clean seeded state។

នេះជាកំណត់ត្រា verification ដែលមានស្រាប់ មិនមែនជាការរត់ tests ថ្មីក្នុងពេលបកប្រែឯកសារនេះទេ។
