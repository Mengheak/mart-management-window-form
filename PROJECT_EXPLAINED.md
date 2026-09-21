# Mini Mart Management System — Explained Simply

A plain-language guide to what this program is, how the code is organised, and what happens
step by step when someone uses it.

> Companion documents:
> [README.md](README.md) = how to install and run it.
> [DATA_MANAGEMENT.md](DATA_MANAGEMENT.md) = how to edit, query, back up and reset the data.
> [SYSTEM_FLOW.md](SYSTEM_FLOW.md) = the same story with every file and method named.
> **This file** = the short, simple version you can explain out loud.

---

## 1. What is it, in one paragraph?

It is a **cash register + stockroom notebook for a small shop**, running as a Windows
desktop program. A **cashier** signs in, scans or searches products, builds a basket, takes
the customer's money, and prints a receipt. An **admin** signs in and gets a manager's view
instead: add and edit products, categories and staff accounts, see today's takings, see
what is running low, and look at sales reports. Everything is stored in a **SQL Server**
database, so the numbers survive after the program closes.

**The one sentence version:** it sells things, and every sale automatically takes the items
out of stock.

---

## 2. The big idea: three layers

The whole codebase is built on one rule — **separate what the user sees, from the rules of
the business, from the database.** That is called *3-tier architecture*.

Think of a restaurant:

| Layer | Restaurant equivalent | In this project | What it may contain |
|---|---|---|---|
| **Presentation** | The waiter | `MiniMart.Presentation` — the Windows Forms screens | Buttons, grids, labels. **No SQL.** |
| **Business Logic** | The chef and the recipes | `MiniMart.BusinessLogic` — models + services | Rules, maths, validation. **No SQL, no forms.** |
| **Data Access** | The pantry | `MiniMart.DataAccess` — repositories | SQL queries only. **No rules, no forms.** |

The waiter never cooks, and the chef never walks into the dining room.

### Who is allowed to talk to whom

```
Presentation  ──────>  BusinessLogic
   (forms)                 ▲  (rules)
      │                    │
      └──>  DataAccess ────┘
               (SQL)
```

Presentation calls business logic. Business logic asks for data through an **interface**
(a promise like "something can give me a product by its barcode") without knowing that SQL
Server exists. DataAccess is the class that actually keeps that promise.

**Why bother?** Because you could swap SQL Server for MySQL by rewriting only
`MiniMart.DataAccess`, and nothing in the rules or the screens would change. It also means
a bug has one obvious home: wrong total → business logic; ugly layout → presentation;
wrong data returned → data access.

### Size of each layer

| Project | Files | Lines | Framework |
|---|---|---|---|
| `MiniMart.BusinessLogic` | 29 | ~1,660 | `net9.0` (plain C#, zero dependencies) |
| `MiniMart.DataAccess` | 8 | ~1,045 | `net9.0` (ADO.NET / `Microsoft.Data.SqlClient`) |
| `MiniMart.Presentation` | 23 | ~4,285 | `net9.0-windows` (Windows Forms — this is the `.exe`) |

---

## 3. The five things the database stores

Five tables ([MiniMart_Database_Schema.sql](MiniMart_Database_Schema.sql)):

```
Categories ──< Products ──< SaleDetails >── Sales >── Users
 (Drinks,      (Milk 1L,     (2 × Milk      (receipt   (admin,
  Snacks…)      $1.20…)       @ $1.20)       #57)       cashier)
```

| Table | Holds | Note |
|---|---|---|
| `Categories` | Product groups | Name must be unique |
| `Products` | Name, SKU (barcode), price, cost, stock, reorder level | Stock can never go below 0 (database `CHECK`) |
| `Users` | Username, password **hash**, role, active flag | Role is only `Admin` or `Cashier` |
| `Sales` | One row per receipt: date, total, paid, change, cashier | The receipt *header* |
| `SaleDetails` | One row per line on the receipt: product, qty, unit price | The receipt *lines* |

**Why two tables for one sale?** A single receipt has many lines. The header answers "how
much was this sale?", the lines answer "what was in it?". `SaleDetails` stores the price
*at the time of sale*, so raising a price tomorrow does not rewrite yesterday's receipts.

---

## 4. Walking through the program

### 4.1 Startup

`Program.cs` does three things:

1. Start Windows Forms.
2. Build every object the app needs — `AppServices.CreateFromConfiguration()`.
3. Enter a loop: show login → open the right main screen → if the user signed out, loop
   back to login instead of quitting.

**`AppServices` is the wiring board.** It is the *only* file that names the concrete
database classes. It creates the four repositories once, hands them to the services, and
then every screen just receives `services` and asks for `services.Inventory`,
`services.Auth`, and so on.

```csharp
// The only place DataAccess is named:
_productRepository = new ProductRepository(connectionFactory);
// Services only ever see the interface:
Inventory = new InventoryService(_productRepository, categoryRepository);
```

One exception: `CreateSaleService()` returns a **new** `SaleService` each time, because a
`SaleService` remembers the discount currently applied — each till needs its own so one
cashier's discount cannot leak into another's sale.

### 4.2 Signing in

- If the `Users` table is **empty**, the app opens a *First-Time Setup* window and you
  create the first admin. **There is no default password anywhere in this project.**
- Passwords are never stored. Only a **PBKDF2-HMAC-SHA256 hash** (100,000 iterations,
  random salt per user) is saved, in the form `PBKDF2$100000$<salt>$<hash>`. To check a
  password, the app re-computes the hash and compares — it can never read the original.
- Wrong username and wrong password give the **same** error message, so nobody can use the
  login screen to discover which usernames exist.
- After a successful sign-in: `Admin` → dashboard, `Cashier` → the till.

### 4.3 The cashier's screen (POS) — the main path

**Adding items.** Three ways in, all ending in the same method:

- type a name in the search box → the grid filters,
- type a SKU and press **Enter** → straight into the basket,
- double-click a row / click Add → into the basket.

The basket itself is a `Cart` object, and the rules live *in* the cart, not in the form:
quantity must be positive, the product must be active, and the combined quantity must fit
in available stock — otherwise `InsufficientStockException`.

**Totals.** After every keystroke the form asks `SaleService.QuoteCart(...)`, which is pure
arithmetic with no database access:

```
subtotal  = sum of the lines
total     = discount applied to subtotal
change    = paid − total     (if paid is enough)
```

Because it touches nothing, the form can call it as often as it wants.

**Discounts — one small class each.** `DiscountStrategy` is an abstract class with one
method, `Apply(subtotal)`. There are three versions: `NoDiscount`, `PercentageDiscount`,
`FlatDiscount` (clamped at zero so a large discount can never make a total negative). The
POS screen just picks one. **To add a new kind of discount you write one new class and
change nothing else** — that is polymorphism doing real work.

### 4.4 Checkout — the part worth explaining carefully

When the cashier presses **F9**:

1. Is the cart empty? Is a cashier signed in? → refuse with a clear message.
2. **Re-read every product from the database.** The cart may have been built ten minutes
   ago; stock may have moved since.
3. Recompute the total, and refuse if the money paid is less than the total.
4. Build the `Sale` object and its lines.
5. Save it — and this is the only place in the whole app that opens a **database
   transaction**:

```
BEGIN TRANSACTION
    INSERT INTO Sales ...            → get the new SaleId back
    for each line:
        INSERT INTO SaleDetails ...
        UPDATE Products
           SET StockQuantity = StockQuantity - @Quantity
         WHERE ProductId = @ProductId
           AND StockQuantity >= @Quantity      ←— the safety net
        if 0 rows changed → throw, and ROLLBACK everything
COMMIT
```

**Why stock is checked twice.** Step 2 gives a *friendly* message in the normal case. The
`AND StockQuantity >= @Quantity` line is the *guarantee*. If two tills sell the last carton
of milk at the same instant, one of the two `UPDATE`s matches zero rows, the whole
transaction is rolled back, and nothing is half-saved — no receipt, no lines, no stock
change. "All or nothing" is exactly what a transaction means.

6. The receipt window opens (a plain 42-column text receipt that can also be printed), the
   cart resets, and the product grid reloads so the stock numbers on screen are current.

### 4.5 The admin's screen

The dashboard shows four tiles (sales today, revenue today, low-stock count, active
products) and a recent-sales grid. Every tile comes from a reporting or inventory service
call — the form does no maths of its own. The left nav opens child windows, and the
dashboard refreshes itself each time one closes:

| Button | What it does |
|---|---|
| Products | Add / edit / delete products, adjust stock, low stock highlighted |
| Categories | Add / rename / delete categories |
| Users | Add / edit / deactivate staff accounts |
| Sales History & Reports | Date range, transaction list with line drill-down, daily totals, best sellers |
| Low Stock | Everything at or below its reorder level |
| Point of Sale | An admin can work the till too |

---

## 5. Five rules that show up everywhere

**1. Objects protect themselves.** Models have no public setters. `Product.StockQuantity`
can only change through `ReduceStock` / `AdjustStockTo`, which refuse to go negative. An
invalid `Product` cannot be created in the first place. *(Encapsulation.)*

**2. All SQL is parameterised.** Never string-joined:

```csharp
parameters.Add("@SKU", SqlDbType.NVarChar, 50).Value = sku;
```

Typing `x'; DROP TABLE Products; --` into the search box searches for that literal text.
SQL injection is structurally impossible here, not merely filtered out.

**3. Deleting becomes deactivating when history exists.** If a product or user appears in
a past sale, deleting it would break old receipts — so the app marks it inactive instead,
and tells the user which of the two happened. Categories are different: a category still
holding products simply refuses to be deleted, and says how many.

**4. You cannot lock yourself out.** Before demoting, deactivating or deleting an admin,
the app counts the remaining active admins and refuses if that would leave zero.

**5. Two kinds of error, two kinds of message.**

| Error type | What the user sees |
|---|---|
| `BusinessRuleException` (expected: "cart is empty", "SKU already exists") | The exact message, verbatim |
| `SqlException` (infrastructure) | A general "something went wrong, **nothing was saved**, please retry" |

Every database call from a form goes through one helper, `AsyncUi.RunAsync`, which shows
the wait cursor, catches everything, reports it with the right wording, and returns `null`
so the calling code just stops. That is why no form contains a `try/catch` pyramid.

---

## 6. One sale, end to end (the story to tell)

```
Cashier types "MLK-001" and presses Enter
  → the POS form asks the Inventory service for that SKU        [Presentation]
  → the service validates the input and asks the repository     [Business Logic]
  → SELECT … WHERE SKU = @SKU AND IsActive = 1                  [Data Access]
  ← a Product object comes back up
  → Cart.AddItem checks it is active and in stock               [Business Logic]
  → totals recalculated, labels updated                         [Presentation]

Cashier enters the cash and presses F9
  → SaleService.CheckoutAsync
       re-reads stock for every line
       recomputes the total, checks the money is enough
       builds the Sale + its lines
  → SaleRepository.SaveSaleAsync
       BEGIN TRAN → insert header → per line: insert + guarded stock update → COMMIT
  ← the saved Sale, with its real database ID and timestamp
  → receipt window, cart cleared, product grid reloaded
```

Every arrow that crosses a layer crosses it through an interface. The only transaction in
the codebase lives in the only method that actually needs one.

---

## 7. Answers to the questions you'll be asked

**"Why three projects instead of one?"**
So the compiler enforces the layering. `MiniMart.BusinessLogic` has no reference to SQL
Server or to Windows Forms, so it *cannot* accidentally contain SQL or a message box.

**"Why are the repository interfaces in BusinessLogic, not DataAccess?"**
Because `SaleService` needs `ISaleRepository`, and `SaleRepository` needs the `Sale` model.
If the interfaces lived in DataAccess the two projects would reference each other in a
circle. Putting the *contracts* next to the *models* and having DataAccess implement them
is the standard fix — dependency inversion. Business logic still depends only on
abstractions.

**"Where is the OOP?"**
Encapsulation → models with private setters and guarded stock. Abstraction → the
`I*Repository` interfaces. Polymorphism → the three `DiscountStrategy` subclasses, called
through one abstract method. Inheritance → `InsufficientStockException` extends
`BusinessRuleException`, so a generic catch still handles it.

**"What stops two cashiers overselling the same item?"**
The `WHERE StockQuantity >= @Quantity` clause inside the transaction's `UPDATE`. Zero rows
affected means someone got there first, and the whole sale rolls back.

**"Is the connection string hard-coded?"**
No — it is in `App.config` under the name `MiniMartDb`, and after deployment it can be
edited beside the `.exe` without rebuilding. The same file holds the store name, address
and phone printed on receipts, and the currency symbol.

**"Why is everything `async`?"**
Every repository method is asynchronous, so the UI thread is never blocked while SQL Server
is answering — the window does not freeze. The UI awaits with `ConfigureAwait(true)`
(it must resume on the UI thread to touch controls); the business and data layers use
`ConfigureAwait(false)` because they never touch a control.

**"What was deliberately left out, and why?"**
No customers table, no tax, no stored procedures, and discounts are not stored per sale —
in each case because the agreed schema has no column for it and the specification lists it
as optional. `Sales.TotalAmount` stores the post-discount figure, and the receipt derives
the discount as `Subtotal − TotalAmount`. Full list in [README.md](README.md) §8.

---

## 8. Where to look in the code

| If you want to see… | Open |
|---|---|
| The app's entry point and the login/logout loop | `src/MiniMart.Presentation/Program.cs` |
| How every object is wired together | `src/MiniMart.Presentation/AppServices.cs` |
| The till | `src/MiniMart.Presentation/Forms/PosForm.cs` |
| Basket rules | `src/MiniMart.BusinessLogic/Models/Cart.cs` |
| Checkout rules | `src/MiniMart.BusinessLogic/Services/SaleService.cs` |
| **The transaction** | `src/MiniMart.DataAccess/Repositories/SaleRepository.cs` |
| The shared SQL plumbing | `src/MiniMart.DataAccess/Infrastructure/SqlRepositoryBase.cs` |
| Password hashing | `src/MiniMart.BusinessLogic/Security/Pbkdf2PasswordHasher.cs` |
| Discounts | `src/MiniMart.BusinessLogic/Discounts/` |
| The tables | `MiniMart_Database_Schema.sql` |

---

## 9. Running it, in four lines

```bash
sqlcmd -S localhost,1434 -U sa -P 'Heak020507#' -b -i MiniMart_Database_Schema.sql
sqlcmd -S localhost,1434 -U sa -P 'Heak020507#' -b -i MiniMart_SeedData.sql
dotnet build MiniMartManagementSystem.sln
dotnet run --project src/MiniMart.Presentation
```

The seed script adds 6 categories and 24 products (5 deliberately below their reorder level
so the Low Stock screen has something to show) and **no user accounts** — the first run
asks you to create the admin. Full setup notes, including how to point at a non-Docker SQL
Server, are in [README.md](README.md) §3–§5.
