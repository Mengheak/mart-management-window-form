# Mart Management System

A C# **Windows Forms** desktop POS and inventory application built on a strict **3-Tier
Architecture** with **ADO.NET** against **SQL Server**, implementing the design set out in
`Mini_Mart_Management_System_Documentation.docx` and the schema in
`MiniMart_Database_Schema.sql`.

---

## 1. Solution layout

```
MiniMartManagementSystem.sln
└── src/
    ├── MiniMart.BusinessLogic/     net9.0        — domain models, services, contracts
    ├── MiniMart.DataAccess/        net9.0        — ADO.NET repository implementations
    └── MiniMart.Presentation/      net9.0-windows — Windows Forms UI
```

### Reference graph

```
  Presentation  ──>  BusinessLogic
       │                  ▲
       └──> DataAccess ───┘
```

* `MiniMart.BusinessLogic` — `Models/` (Product, Category, CartItem, Cart, Sale, SaleDetail,
  User), `Repositories/` (the `I*Repository` contracts), `Services/`, `Discounts/`,
  `Security/`, `Exceptions/`, `Reporting/`. **No ADO.NET, no WinForms.**
* `MiniMart.DataAccess` — `ProductRepository`, `SaleRepository`, `CategoryRepository`,
  `UserRepository`, plus the connection factory. **No WinForms.**
* `MiniMart.Presentation` — forms only. Contains no SQL and no transaction handling.

> **Note on interface placement.** The repository *interfaces* live in `BusinessLogic`
> rather than `DataAccess`. Putting the contracts in `DataAccess` while the domain models
> live in `BusinessLogic` is circular — `SaleService` needs `ISaleRepository`, and
> `SaleRepository` needs `Sale`. This is the standard Dependency Inversion resolution and
> keeps the layer count at three. The Business Logic Layer still depends only on
> abstractions, exactly as Chapter 2.3.2 requires.

---

## 2. Prerequisites

* **.NET SDK 9.0** or later (`dotnet --version`)
* **SQL Server** — any edition, or LocalDB
* Windows (the Presentation layer targets `net9.0-windows`)

---

## 3. Database setup

**The app is currently configured against the SQL Server 2022 Docker container** defined in
`D:\docker\sql server\docker-compose.yml`.

```bash
# Start the stack (SQL Server + dbgate)
cd "D:/docker/sql server" && docker compose up -d
```

| Service | Host endpoint | Credentials |
|---|---|---|
| SQL Server 2022 | `localhost,1434` | `sa` / `Heak020507#` |
| dbgate (web UI) | http://localhost:8080 | connection `sql1` is pre-wired |

> **Why port 1434, not 1433?** A local **SQL Server 2014** instance is installed on this
> machine and already owns 1433. It wins for `localhost` connections, which left the
> container unreachable from Windows even though `docker ps` showed the mapping. The compose
> file now publishes `1434:1433`. dbgate still reaches the server on `sqlserver:1433` over
> the internal `sqlnet` bridge, so it was unaffected.
>
> The compose file's `MSSQL_SA_PASSWORD` was also corrected to `Heak020507#` — the password
> the `sql_data` volume was actually initialised with. Changing that variable does **not**
> reset SA's password on an existing volume, it only feeds the healthcheck, which would
> otherwise fail and block dbgate from starting.

Then run the two scripts in order:

```bash
# 1. Schema (creates MiniMartDB and all five tables)
sqlcmd -S localhost,1434 -U sa -P 'Heak020507#' -b -i MiniMart_Database_Schema.sql

# 2. Sample catalogue — optional but recommended
sqlcmd -S localhost,1434 -U sa -P 'Heak020507#' -b -i MiniMart_SeedData.sql
```

To target a non-Docker instance instead, swap `-S localhost,1434 -U sa -P '…'` for
`-S . -E` (local default instance), `-S .\SQLEXPRESS -E`, or
`-S '(localdb)\MSSQLLocalDB' -E`, and update `App.config` to match (§4).

The seed script adds **6 categories and 24 products** (5 of them deliberately below their
reorder level so the Low Stock screen has content). It creates **no user accounts** — see
*First run* below.

> ⚠️ **Re-running the schema script on an existing database fails.** It drops `Categories`
> before `Products`, and the foreign key blocks that. To reset, drop in dependency order
> first:
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

## 4. Connection string

One central place: **`src/MiniMart.Presentation/App.config`**.

```xml
<connectionStrings>
  <add name="MiniMartDb"
       connectionString="Server=localhost,1434;Database=MiniMartDB;User ID=sa;Password=Heak020507#;TrustServerCertificate=True;..."
       providerName="Microsoft.Data.SqlClient" />
</connectionStrings>
```

| Target | `Server=` value | Auth |
|---|---|---|
| **Docker container (current)** | `localhost,1434` | `User ID=sa;Password=…` |
| Local default instance | `.` | `Trusted_Connection=True` |
| SQL Server Express | `.\SQLEXPRESS` | `Trusted_Connection=True` |
| LocalDB | `(localdb)\MSSQLLocalDB` | `Trusted_Connection=True` |

The previous local-instance connection string is kept commented out directly beneath the
active one in `App.config`, so switching back is a matter of swapping which is commented.

`TrustServerCertificate=True` is required because `Microsoft.Data.SqlClient` encrypts by
default and local instances usually present a self-signed certificate.

After deployment this can be edited in `MiniMart.Presentation.dll.config` beside the
executable — no rebuild needed.

`App.config` also holds the store name/address/phone printed on receipts and the currency
symbol used across the UI.

---

## 5. Build and run

```bash
dotnet build MiniMartManagementSystem.sln
dotnet run --project src/MiniMart.Presentation
```

Or open `MiniMartManagementSystem.sln` in Visual Studio and press F5.

### First run

There is **no default password anywhere in this project.** On a database with no accounts,
the app shows a **First-Time Setup** dialog and creates the initial Admin with a password
you choose. It is stored only as a salted PBKDF2-HMAC-SHA256 hash (100,000 iterations).

To reset the initial admin, delete the row and restart:

```sql
USE MiniMartDB; DELETE FROM dbo.Users WHERE Username = 'admin';
```

(A user who has processed sales cannot be deleted this way — the app deactivates such
accounts instead, so sales history keeps resolving to a real cashier.)

---

## 6. Screens

**Sign-in** routes by role: `Admin` → dashboard, `Cashier` → till.

| Screen | Role | Notes |
|---|---|---|
| Point of Sale | Cashier, Admin | Scan/search, cart, discounts, payment, receipt. `F9` checkout, `F2` search. |
| Dashboard | Admin | Today's sales/revenue, low-stock count, recent sales |
| Products | Admin | CRUD, adjust stock, low-stock highlighting |
| Categories | Admin | CRUD |
| Users | Admin | CRUD for Admin/Cashier accounts |
| Sales History & Reports | Admin | Transactions + line drill-down, daily totals, best sellers |
| Low Stock | Admin | Items at/below reorder level |

At the till: type a code and press **Enter** to add it straight to the cart, or search by
name and double-click a row.

---

## 7. How the specification maps to the code

| Requirement | Where |
|---|---|
| Encapsulation — stock can never go negative | `Product.ReduceStock` / `AdjustStockTo`; `StockQuantity` has no public setter |
| Abstraction — BLL depends on contracts only | `BusinessLogic/Repositories/I*Repository.cs` |
| Polymorphism — discount strategies | `Discounts/DiscountStrategy` + `PercentageDiscount`, `FlatDiscount`, `NoDiscount`; used by `SaleService` |
| Parameterized queries only (Ch. 5) | `SqlRepositoryBase` helpers accept a fixed template + a parameter-binding delegate; no repository ever concatenates or interpolates SQL |
| Transactional checkout (Ch. 6.2) | `SaleRepository.SaveSaleAsync` — one `SqlTransaction`: insert header (`OUTPUT INSERTED.SaleId`), insert each line, then `UPDATE … WHERE StockQuantity >= @Quantity`; zero rows affected ⇒ rollback everything |
| Thin event handlers (Ch. 6.3) | `PosForm` — no SQL, no transactions, no stock arithmetic; even the running total comes from `SaleService.QuoteCart` |
| Business vs. infrastructure errors (Ch. 6.3) | `UiFeedback.ShowError` — `BusinessRuleException` shown verbatim; `SqlException` gets a generic "nothing was saved, please retry" |
| Async data access | Every repository method is `async`; the UI awaits through `AsyncUi.RunAsync` |
| Composition root, no singletons | `AppServices` — the only type that names a concrete repository |

### The race condition guard

Stock is validated twice on purpose. `SaleService` re-reads each product before saving,
which produces a friendly message in the common case. The **authoritative** check is the
guarded `UPDATE` inside the transaction: if two tills sell the last unit simultaneously, one
of them matches zero rows and the entire sale — header, all lines, and any stock already
decremented — is rolled back.

---

## 8. Assumptions made beyond the two documents

1. **.NET 9 / `Microsoft.Data.SqlClient`.** The modern, maintained ADO.NET provider
   (`System.Data.SqlClient` is deprecated). Still ADO.NET: `SqlConnection`, `SqlCommand`,
   `SqlParameter`, `SqlTransaction`, `SqlDataReader`.
2. **Repository interfaces in `BusinessLogic`** — see §1.
3. **Password hashing = PBKDF2-HMAC-SHA256**, 100k iterations, 16-byte random salt, stored
   as `PBKDF2$iterations$salt$hash` (~83 chars, fits `NVARCHAR(256)`). The documents require
   hashing but do not specify an algorithm.
4. **No default credentials**; first-run setup instead.
5. **Admins can open the POS screen.** The documents assign POS to Cashiers; treating Admin
   as a superset lets a manager cover the counter without a second account.
6. **Delete is soft where history exists.** Products and users referenced by sales are
   deactivated rather than deleted, so historical receipts and cashier attribution stay
   intact. The UI says which happened.
7. **Discounts are applied at the cart level, not persisted per-sale.** The schema's `Sales`
   table has no discount column, so `TotalAmount` stores the post-discount figure; the
   receipt derives the discount as `Subtotal − TotalAmount`. No schema change was made.
8. **No `Customers` table.** Chapter 1.3.2 lists customers as *optional* and the schema has
   no such table.
9. **Tax is not applied.** Chapter 1.3.3 mentions tax, but the schema carries no tax column
   and no rate is specified anywhere.
10. **Stored procedures were not used.** Chapter 5.4 presents them as an *additional*
    optional layer; the required defence — parameterization — is applied everywhere.
11. **Layouts are defined in code.** `LoginForm` and `PosForm` use `.Designer.cs` files; the
    remaining forms build their controls in a `BuildUi()` method. Shared styling lives in
    `UiTheme`.

---

## 9. Verification performed

The whole solution builds with **0 warnings, 0 errors**. The data layer was exercised against
a real SQL Server 2014 instance with a temporary harness covering 46 assertions — all passed:

* password hashing/verification, rejection of wrong passwords and duplicate usernames
* SQL-injection payload (`x'; DROP TABLE dbo.Products; --`) treated purely as data
* category/product CRUD, duplicate SKU and duplicate-name rejection
* `Product.ReduceStock` refusing to go negative
* all three discount strategies, including flat-discount clamping at zero
* **a full checkout**: sale persisted, stock decremented 100→97 and 50→48
* **the rollback guarantee**: an oversell threw `InsufficientStockException`, left no sale
  header behind, and rolled back the stock deduction of the *valid* line in the same
  transaction
* reporting: line drill-down, joined names, daily totals not inflated by the line-item join,
  best-seller ranking
* referential guards: category-in-use, soft-delete of products/users with history, and
  refusal to remove the last active administrator

Search behaviour was verified separately, including that `%` and `_` are escaped and match
literally rather than acting as wildcards.

The application was launched and confirmed to start, connect, and render the sign-in,
first-run setup, and Admin dashboard screens.

After being repointed at the **SQL Server 2022 Docker container**, the data layer was
re-verified through `Microsoft.Data.SqlClient` using the exact connection string now
deployed — 13/13 assertions passed, covering connectivity, the seeded catalogue, password
hashing and sign-in, a committed checkout with stock decrement, the oversell rollback, and
the reporting queries. The container database was then reset to a clean seeded state.
"# mart-management-window-form" 
