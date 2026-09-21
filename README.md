# Mart Management System

A C# **Windows Forms** desktop POS and inventory application built on a strict **3-Tier
Architecture** with **ADO.NET** against **SQL Server**, implementing the design set out in
`Mini_Mart_Management_System_Documentation.docx` and the schema in
`MiniMart_Database_Schema.sql`.

**Documentation:** this file covers installation and setup · [PROJECT_EXPLAINED.md](PROJECT_EXPLAINED.md)
explains the system in plain language · [SYSTEM_FLOW.md](SYSTEM_FLOW.md) traces the code
file by file.

### Quick start

Windows + [.NET SDK 9](https://dotnet.microsoft.com/download/dotnet/9.0) + Docker, from the
repository root:

```bash
docker compose up -d
sqlcmd -S localhost,1434 -U sa -P 'Heak020507#' -b -i MiniMart_Database_Schema.sql
sqlcmd -S localhost,1434 -U sa -P 'Heak020507#' -b -i MiniMart_SeedData.sql
dotnet run --project src/MiniMart.Presentation
```

The app asks you to create the first admin account on first launch. Full instructions,
including how to use an existing SQL Server instead of Docker and how to change that
password, are in §2–§5.

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

## 2. Install the prerequisites

| You need | Why | Check it is installed |
|---|---|---|
| **Windows 10/11** | The Presentation layer targets `net9.0-windows` (Windows Forms). The app will not run on Linux or macOS. | — |
| **.NET SDK 9.0** or later — [download](https://dotnet.microsoft.com/download/dotnet/9.0) | Builds and runs all three projects | `dotnet --version` |
| **SQL Server** — Docker, Express, Developer, or LocalDB | Stores everything | see §3 |
| **Git** (optional) | To clone the repository | `git --version` |
| **`sqlcmd`** (optional) | Runs the two `.sql` scripts from the terminal. If you'd rather use a GUI, see §3.3. | `sqlcmd -?` |

Visual Studio 2022 (17.12+) with the *.NET desktop development* workload is optional — the
`dotnet` CLI is enough for everything below.

### 2.1 Get the code

```bash
git clone <repository-url> "Mart Management System"
```

Then `cd` into the folder. Every command in this README is run from that folder — the one
containing `MiniMartManagementSystem.sln`. (Downloading and extracting the ZIP works too.)

---

## 3. Set up the database

Pick **one** of the two options below, then continue to §3.3 to create the tables.

### 3.1 Option A — SQL Server in Docker (recommended, self-contained)

A ready-made [docker-compose.yml](docker-compose.yml) sits in the repository root. It brings
up SQL Server 2022 plus **dbgate**, a browser-based database client, so you don't have to
install anything else.

```bash
docker compose up -d
```

Wait until the server reports healthy (about 30 seconds on first start):

```bash
docker compose ps
```

| Service | Host endpoint | Credentials |
|---|---|---|
| SQL Server 2022 | `localhost,1434` | `sa` / the `MSSQL_SA_PASSWORD` in the compose file |
| dbgate (web UI) | http://localhost:3033 | connection `sql1` is pre-wired — no setup needed |

> 🔐 **Change the SA password before using this anywhere real.** The compose file ships with
> a development password in plain text. Set your own `MSSQL_SA_PASSWORD` in
> `docker-compose.yml` **before the first `docker compose up`**, and put the same password in
> `App.config` (§4). It must be at least 8 characters with upper case, lower case, and a
> digit or symbol, or the container will refuse to start.
>
> Changing that variable **after** the first start does *not* reset the password — it is
> baked into the `sql_data` volume. To start over:
> `docker compose down -v` (this deletes the database), then `docker compose up -d`.

> **Why is it published on port 1434 instead of the usual 1433?** Because this machine also
> has a local SQL Server 2014 instance that already owns 1433 — it wins for `localhost`
> connections, which made the container unreachable from Windows even though `docker ps`
> showed the mapping. If **you** have nothing on 1433, you can change the mapping to
> `"1433:1433"` and use `Server=localhost` (no comma) in `App.config`. dbgate is unaffected
> either way: it reaches the server on `sqlserver:1433` over the internal `sqlnet` bridge.

### 3.2 Option B — an existing SQL Server instance

If you already have SQL Server, LocalDB, or Express installed, skip Docker entirely. You
only need to know how to address your instance:

| Your setup | Use this server name |
|---|---|
| Local default instance | `.` or `localhost` |
| SQL Server Express | `.\SQLEXPRESS` |
| LocalDB (ships with Visual Studio) | `(localdb)\MSSQLLocalDB` |
| A server on your network | `hostname,port` |

Your Windows account needs permission to create a database.

### 3.3 Create the tables and (optionally) load sample data

Run the two scripts **in this order**. `MiniMart_Database_Schema.sql` creates the
`MiniMartDB` database and all five tables; `MiniMart_SeedData.sql` fills the catalogue.

**With `sqlcmd` — Docker (Option A):**

```bash
sqlcmd -S localhost,1434 -U sa -P 'YourPassword' -b -i MiniMart_Database_Schema.sql
```

```bash
sqlcmd -S localhost,1434 -U sa -P 'YourPassword' -b -i MiniMart_SeedData.sql
```

**With `sqlcmd` — a local instance using Windows authentication (Option B):**

```bash
sqlcmd -S . -E -b -i MiniMart_Database_Schema.sql
```

```bash
sqlcmd -S . -E -b -i MiniMart_SeedData.sql
```

Replace `-S .` with `-S .\SQLEXPRESS` or `-S '(localdb)\MSSQLLocalDB'` as needed.

**Without `sqlcmd` — use a GUI instead:** open dbgate (http://localhost:3033), SQL Server
Management Studio, or Azure Data Studio; open each `.sql` file; execute
`MiniMart_Database_Schema.sql` first, then `MiniMart_SeedData.sql`.

**Verify it worked** — this should list five tables:

```bash
sqlcmd -S localhost,1434 -U sa -P 'YourPassword' -Q "USE MiniMartDB; SELECT name FROM sys.tables ORDER BY name;"
```

The seed script is optional but recommended: it adds **6 categories and 24 products**, 5 of
them deliberately below their reorder level so the Low Stock screen has content. It is safe
to re-run — every insert is guarded by a `NOT EXISTS` check. It creates **no user
accounts**; you create the first one on first run (§5.1).

> ⚠️ **Re-running the *schema* script on an existing database fails.** It drops `Categories`
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

## 4. Point the app at your database

One central place: **[`src/MiniMart.Presentation/App.config`](src/MiniMart.Presentation/App.config)**.
Edit the `MiniMartDb` connection string so it matches the database you just set up.

```xml
<connectionStrings>
  <add name="MiniMartDb"
       connectionString="Server=localhost,1434;Database=MiniMartDB;User ID=sa;Password=YourPassword;TrustServerCertificate=True;Application Name=MiniMartManagementSystem;Connect Timeout=15"
       providerName="Microsoft.Data.SqlClient" />
</connectionStrings>
```

Only the `Server=` part and the authentication change between setups:

| Target | `Server=` value | Authentication |
|---|---|---|
| **Docker container (as shipped)** | `localhost,1434` | `User ID=sa;Password=YourPassword` |
| Local default instance | `.` | `Trusted_Connection=True` |
| SQL Server Express | `.\SQLEXPRESS` | `Trusted_Connection=True` |
| LocalDB | `(localdb)\MSSQLLocalDB` | `Trusted_Connection=True` |

A local-instance connection string is kept commented out directly beneath the active one in
`App.config`, so switching between the two is a matter of swapping which one is commented.

`TrustServerCertificate=True` is required because `Microsoft.Data.SqlClient` encrypts
connections by default and local/containerised instances present a self-signed certificate.

If the connection string is missing or wrong, the app shows one clear message box on startup
and exits rather than failing screen by screen.

After deployment this same setting lives in `MiniMart.Presentation.dll.config` beside the
executable, so a site can be repointed at a different server **without a rebuild**.

`App.config` also holds the store name/address/phone printed on receipts and the currency
symbol used across the UI — edit those too if you like.

---

## 5. Build and run

```bash
dotnet build MiniMartManagementSystem.sln
```

```bash
dotnet run --project src/MiniMart.Presentation
```

Or open `MiniMartManagementSystem.sln` in Visual Studio, make **MiniMart.Presentation** the
startup project, and press <kbd>F5</kbd>.

The sign-in window should appear with your server name shown beneath the title. To produce a
self-contained build for another machine:

```bash
dotnet publish src/MiniMart.Presentation -c Release -r win-x64 --self-contained false -o publish
```

### 5.1 First run — create the administrator

There is **no default password anywhere in this project.** When the app finds a database with
no accounts, it opens a **First-Time Setup** dialog and creates the initial Admin with a
username and password you choose. The password is stored only as a salted
PBKDF2-HMAC-SHA256 hash (100,000 iterations) — it is never recoverable, only verifiable.

Sign in with that account and you land on the Admin dashboard. From **Users** you can create
the cashier accounts; a cashier signing in goes straight to the till instead (§6).

To start the account setup over, delete the row and restart the app:

```sql
USE MiniMartDB; DELETE FROM dbo.Users WHERE Username = 'admin';
```

(A user who has already processed sales cannot be deleted this way — the app deactivates
such accounts instead, so sales history keeps resolving to a real cashier.)

### 5.2 If something goes wrong

| Symptom | Cause and fix |
|---|---|
| "The application is not configured correctly" | The `MiniMartDb` entry is missing from `App.config`. See §4. |
| A network/instance error naming SQL Server | The server isn't reachable. Docker: `docker compose ps` — is it healthy? Local: is the SQL Server service running, and is TCP/IP enabled in SQL Server Configuration Manager? |
| "Login failed for user 'sa'" | The password in `App.config` doesn't match the one the container volume was created with. Either use the original password or reset with `docker compose down -v` (deletes all data). |
| "Cannot open database 'MiniMartDB'" | The schema script hasn't been run yet. See §3.3. |
| A certificate/trust error | Add `TrustServerCertificate=True` to the connection string. |
| Container starts then exits | The SA password doesn't meet SQL Server's complexity rules. Check `docker compose logs sqlserver`. |
| `sqlcmd: command not found` | Use dbgate or SSMS instead (§3.3), or install the SQL Server command-line tools. |
| Port 1434 already in use | Change the left side of `"1434:1433"` in `docker-compose.yml`, then match it in `App.config`. |
| `dotnet build` fails on `net9.0-windows` | You're not on Windows, or the .NET 9 SDK isn't installed. Check `dotnet --list-sdks`. |
| The Low Stock screen is empty | The seed data wasn't loaded. Run `MiniMart_SeedData.sql` (§3.3). |

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
