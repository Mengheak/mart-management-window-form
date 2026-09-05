# Mini Mart Management System — Source Code Flow

## 1. The three projects

The solution is split into three projects, and the reference arrows only point one way:

```
MiniMart.Presentation  (WinForms, net9.0-windows, the .exe)
        │  references ↓
MiniMart.DataAccess    (ADO.NET + SQL Server, net9.0)
        │  references ↓
MiniMart.BusinessLogic (plain C#, net9.0, no dependencies at all)
```

`MiniMart.BusinessLogic` references nothing. It holds the models, the rules, and the interfaces for data access (`IProductRepository`, `ISaleRepository`, etc.). `MiniMart.DataAccess` implements those interfaces with real SQL. `MiniMart.Presentation` shows the forms and only talks to business-logic services.

The point of this: business rules never know about SQL, and SQL never knows about forms.

---

## 2. Startup

`Program.cs:9` — `Main()`:

1. `ApplicationConfiguration.Initialize()` — standard WinForms boot.
2. `AppServices.CreateFromConfiguration()` — builds every object the app needs. If this throws (bad connection string), `UiFeedback.ShowError` shows a message box and the app exits.
3. Enters an infinite `while (true)` loop — this loop is what makes "log out and log back in" work.

Inside the loop:

```
new LoginForm(services).ShowDialog()
  ├─ not OK  → return (app exits)
  └─ OK      → user.IsAdmin ? AdminDashboardForm : PosForm
                 Application.Run(mainForm)
                   ├─ form closed normally           → return (exit)
                   └─ form implements ILogoutAware
                      and LogOutRequested == true    → loop back to login
```

`ILogoutAware` (`Common/ILogoutAware.cs`) is a one-property interface — `bool LogOutRequested`. Both `PosForm` and `AdminDashboardForm` implement it. When the user clicks "Sign Out", the form sets the flag to true and closes; `Main` sees the flag and loops back to `LoginForm` instead of exiting.

### AppServices — the wiring

`AppServices.cs` is the manual dependency-injection container. Its constructor does everything in order:

```csharp
// 1. Concrete repositories — the only place the DataAccess classes are named
ICategoryRepository categoryRepository = new CategoryRepository(connectionFactory);
IUserRepository     userRepository     = new UserRepository(connectionFactory);
_productRepository = new ProductRepository(connectionFactory);
_saleRepository    = new SaleRepository(connectionFactory);

// 2. Services get only the interfaces they need
Auth       = new AuthService(userRepository, passwordHasher);
Users      = new UserService(userRepository, passwordHasher);
Categories = new CategoryService(categoryRepository);
Inventory  = new InventoryService(_productRepository, categoryRepository);
Reporting  = new ReportingService(_saleRepository);
```

`Auth`, `Inventory`, `Categories`, `Users`, `Reporting` are singletons — one instance for the whole app life, passed to every form.

`SaleService` is the exception. `CreateSaleService()` (`AppServices.cs:51`) makes a new one each call, because `SaleService` holds mutable state — the currently applied `DiscountStrategy`. Each `PosForm` gets its own so one till's discount doesn't leak into another.

The connection string comes from `App.config` via `DatabaseSettings.GetConnectionString()`, which looks up the `MiniMartDb` entry and throws a `ConfigurationErrorsException` with a helpful message if it's missing.

---

## 3. Login flow

`LoginForm_Load` (`LoginForm.cs:22`):

1. Shows the database description in a label (Server / MiniMartDB).
2. Calls `OfferFirstRunSetupAsync()` → `Auth.AnyUsersExistAsync()`.
   - If the `Users` table is empty, it opens `FirstRunSetupForm` so you can create the first admin. If you cancel that, the login form closes and the app exits.
   - Return type is `bool?` on purpose: `null` means the check itself failed (DB down), which is different from `false` (no users). Only `false` triggers setup.

`SignInButton_Click` → `AuthService.AuthenticateAsync(username, password)`:

```
AuthService.AuthenticateAsync
  ├─ blank username/password        → BusinessRuleException
  ├─ _userRepository.GetByUsernameAsync(username.Trim())
  ├─ user null OR hash mismatch     → BusinessRuleException "Invalid username or password."
  ├─ user.IsActive == false         → BusinessRuleException "account deactivated"
  └─ return User
```

Notice the "user not found" and "wrong password" cases produce the same message, so you can't probe which usernames exist.

Password checking is `Pbkdf2PasswordHasher`. Stored format is `PBKDF2$100000$<base64 salt>$<base64 hash>` — four `$`-separated parts. Verify re-derives the hash with the salt and iteration count read out of the stored string, then compares with `CryptographicOperations.FixedTimeEquals` (constant-time, so timing doesn't leak). A malformed stored hash returns `false` rather than throwing — a bad row is a failed sign-in, not a crash.

Back in `LoginForm`, a `BusinessRuleException` is caught and shown inline in a red label, not a message box — the cashier retypes without dismissing a dialog. Anything else goes to `UiFeedback.ShowError`.

---

## 4. The POS (cashier) flow — the main path

`PosForm` holds three things: `_cart` (a `Cart`), `_saleService` (its own instance), and `_cashier` (the signed-in `User`).

### Adding items

Three ways to add a product, all ending at the same place:

| Trigger | Path |
|---|---|
| Type in search box | `SearchTextBox_TextChanged` → `Inventory.SearchAsync` → refill grid |
| Scan/type code + Enter | `SearchTextBox_KeyDown` → `Inventory.GetBySkuAsync` → `AddProductToCart` |
| Double-click grid row / "Add" button | `AddSelectedProductToCart` → `AddProductToCart` |

`AddProductToCart` calls `_cart.AddItem(product, quantity)`. The rules live in `Cart.AddItem` (`Models/Cart.cs:21`), not in the form:

- quantity ≤ 0 → `BusinessRuleException`
- product inactive → `BusinessRuleException`
- if the product is already in the cart, the combined quantity is checked against stock → `InsufficientStockException`
- otherwise add a new `CartItem`, or bump the existing line's quantity

`CartItem` snapshots `UnitPrice` at the moment of adding, so a price change mid-sale doesn't alter a cart already built.

### Totals — recalculated constantly

`UpdateTotals()` runs after every cart change and on every keystroke in the "amount paid" box. It calls:

```csharp
var quote = _saleService.QuoteCart(_cart, ReadAmountPaid());
```

`SaleService.QuoteCart` is pure arithmetic — no database, no side effects:

```
subtotal       = cart.Subtotal                      (sum of line totals, rounded)
total          = DiscountStrategy.Apply(subtotal)
discountAmount = subtotal - total
paid           = round(amountPaid)
sufficient     = paid >= total
changeDue      = sufficient ? paid - total : 0
```

It returns a `CheckoutQuote` record. The form reads it and either shows green "Change Due" or red "Still Owing" (`quote.AmountOutstanding`).

Because `QuoteCart` has no side effects, the form can call it as often as it likes.

### Discounts — the strategy pattern

`DiscountStrategy` is an abstract class with `Apply(decimal subtotal)` and `Description`. Three implementations:

- `NoDiscount` — singleton (`NoDiscount.Instance`), returns the subtotal unchanged
- `PercentageDiscount(p)` — validates 0–100 in the constructor, returns `subtotal × (1 − p/100)`
- `FlatDiscount(amount)` — validates non-negative, subtracts, clamps at 0 so a big discount never makes the total negative

`ApplyDiscountButton_Click` (`PosForm.cs:209`) maps the combo box index to a strategy and calls `_saleService.ApplyDiscount(strategy)`. `ApplyDiscount(null)` resets to `NoDiscount.Instance`. Adding a new discount type means writing one class — nothing in `SaleService` or the form changes.

### Checkout — the critical path

`CheckoutButton_Click` → `SaleService.CheckoutAsync(cart, cashierId, amountPaid)`:

1. cart empty? → `BusinessRuleException`
2. `cashierId <= 0`? → `BusinessRuleException`
3. `ValidateStockAsync` — for EVERY line, re-fetch the product from the DB:
   - gone → `BusinessRuleException`
   - inactive → `BusinessRuleException`
   - not enough → `InsufficientStockException`
4. `QuoteCart` — recompute totals
5. `paid < total`? → `BusinessRuleException` (tells you how much more to collect)
6. `new Sale(cashierId, total, paid)` — constructor re-validates and computes `ChangeDue`
7. `sale.AddLines(cart.Items.Select(SaleDetail.FromCartItem))`
8. `_saleRepository.SaveSaleAsync(sale)`

Step 3 is a pre-flight check against a fresh read — the cart was built minutes ago and stock may have moved.

### Where the transaction lives

`SaleRepository.SaveSaleAsync` (`SaleRepository.cs:19`) is the only place in the app that opens a database transaction:

```
BEGIN TRANSACTION
  INSERT INTO Sales (...)  OUTPUT INSERTED.SaleId, INSERTED.SaleDate
      → sale.AssignIdentity(saleId)   (also stamps SaleId onto every line)
      → sale.SetSaleDate(saleDate)    (the DB's clock, not the client's)

  for each line:
      INSERT INTO SaleDetails (...)
      UPDATE Products SET StockQuantity = StockQuantity - @Quantity
             WHERE ProductId = @ProductId AND StockQuantity >= @Quantity
      if rowsAffected == 0 → throw InsufficientStockException
COMMIT
```

The `AND StockQuantity >= @Quantity` in the `UPDATE` is the real safety net. Step 3 in `CheckoutAsync` can go stale between the check and the write; this clause cannot. If someone else sold the last unit in between, the `UPDATE` matches zero rows, the code throws, and the catch block calls `SafeRollbackAsync` — the header, all lines, and all stock decrements are undone together. Nothing half-saved.

`SafeRollbackAsync` swallows exceptions from the rollback itself, because if the connection is already broken the server has aborted the transaction anyway.

### After checkout

```
sale returned → ReceiptForm.ShowDialog()
              → ResetForNextCustomer()  (clear cart, reset discount, clear paid box)
              → LoadProductsAsync()     (reload grid so stock numbers are current)
```

`ReceiptForm` builds a fixed-width 42-column text receipt in a `StringBuilder` (store name from `App.config`, header block, one block per line item, subtotal/discount/total/paid/change), shows it in a read-only Consolas textbox, and can render it to a `PrintDocument` for print preview. A missing printer is caught and reported without implying the sale failed.

Keyboard shortcuts: `F9` = checkout, `F2` = jump to search (`PosForm_KeyDown`).

---

## 5. Admin flow

`AdminDashboardForm` builds its whole UI in code (no designer file). Layout: header bar, left nav panel, four stat tiles, recent-sales grid, status bar.

`RefreshDashboardAsync` runs on `Load` and after every child form closes:

```
Reporting.GetDailyTotalsAsync(today, today)   → sales count + revenue tiles
Inventory.GetLowStockProductsAsync()          → low stock tile
Inventory.GetProductsAsync()                  → active product count tile
Reporting.GetSalesAsync(today-30, today)      → recent sales grid (top 15)
```

Nav buttons open child forms with `ShowDialog`, and `ShowChild` refreshes the dashboard when each one closes:

| Button | Form | Services used |
|---|---|---|
| Products | `ProductListForm` → `ProductEditForm`, `StockAdjustmentForm` | Inventory, Categories |
| Categories | `CategoryListForm` → `TextPromptForm` | Categories |
| Users | `UserListForm` → `UserEditForm` | Users |
| Sales History & Reports | `SalesHistoryForm` | Reporting |
| Low Stock | `LowStockForm` | Inventory |
| Point of Sale | `PosForm` | admin can work the till too |

`SalesHistoryForm` has a from/to date range and three tabs — Transactions (selecting a sale loads its line items into the bottom grid), Daily Totals, Best Sellers — all filled from one `RefreshAllAsync`.

---

## 6. Two recurring rules worth calling out

### Delete becomes deactivate when history exists

Both `InventoryService.DeleteProductAsync` and `UserService.DeleteAsync` follow the same shape:

```csharp
if (await repo.HasSalesHistoryAsync(id)) {
    entity.Deactivate();
    await repo.UpdateAsync(entity);
    return false;          // "soft deleted"
}
await repo.DeleteAsync(id);
return true;               // "hard deleted"
```

The `bool` return says which happened. The UI wraps it in `bool?` — `null` means the operation failed, `false` means deactivated, `true` means deleted — and tells the user honestly: "appears in past sales, so it was marked discontinued instead of deleted." This keeps old receipts resolvable.

Categories work differently: `CategoryService.DeleteAsync` counts products in the category and refuses if any exist, telling you how many.

### You can't lock yourself out

`UserService` calls `EnsureAnotherActiveAdminExistsAsync` before demoting, deactivating, or deleting the last active admin. It counts active admins and throws if the count is ≤ 1.

---

## 7. The domain models guard themselves

Models don't have public setters. Validation lives in private property setters, so an invalid object is impossible to construct:

- `Product.UnitPrice` — negative throws; valid values are rounded to 2 dp on the way in
- `Product.Name` / `Sku` — required, trimmed, length-capped
- `Product.CategoryId` — must be > 0
- `Category.Name`, `User.Username` — same pattern
- `Sale` constructor — rejects negative amounts and `paid < total`, computes `ChangeDue` itself

Every model also has a static `FromDatabase(...)` factory. This is the hydration path for rows coming back from SQL, and it can set things the public constructor can't (identity, `CreatedAt`, `CategoryName`).

`Sale.FromDatabase` has a deliberate trick (`Models/Sale.cs:71`): it calls `new Sale(cashierId, 0m, 0m)` to get past the `paid >= total` check, then assigns the real stored values through the object initializer. Historical rows are facts the database already accepted and must round-trip exactly, even if they'd fail today's validation.

---

## 8. How data gets in and out of SQL

`SqlRepositoryBase` gives every repository four helpers, all following the same shape — open connection, build command, bind parameters, execute, dispose:

- `ExecuteNonQueryAsync` — INSERT/UPDATE/DELETE, returns rows affected
- `ExecuteScalarAsync<T>` — single value (`COUNT(1)` checks)
- `QueryAsync<T>` — many rows, with a `Func<SqlDataReader, T>` map delegate
- `QuerySingleAsync<T>` — one row (`CommandBehavior.SingleRow`) or null

Every repository passes a small static `MapX` method as the mapper — e.g. `SaleRepository.MapSale` reads columns by name via `GetOrdinal` and calls `Sale.FromDatabase`. `ReadNullableString` handles nullable columns from joins.

All SQL is parameterised — `parameters.Add("@SKU", SqlDbType.NVarChar, 50).Value = ...`. No string concatenation anywhere, so no SQL injection. Decimal parameters get explicit `Precision = 10, Scale = 2` to match the `DECIMAL(10,2)` columns and avoid silent truncation.

Two SQL details worth noting:

- `ProductRepository.GetBySkuAsync` filters `AND p.IsActive = 1` — the till can't scan a discontinued product.
- `SaleRepository.GetDailyTotalsAsync` uses `OUTER APPLY` for the unit count instead of joining `SaleDetails`. A direct join would repeat each sale header once per line item and inflate both `SaleCount` and `TotalRevenue`.

---

## 9. Errors and async — the same two helpers everywhere

Two exception types come from the business layer:

- `BusinessRuleException` — expected, user-fixable ("cart is empty", "SKU already exists")
- `InsufficientStockException : BusinessRuleException` — carries `ProductName`, `Requested`, `Available`

`UiFeedback.ShowError` (`Common/UiFeedback.cs`) switches on the exception type:

| Type | Result |
|---|---|
| `BusinessRuleException` | Warning box with the exact message |
| `ConfigurationErrorsException` | "The application is not configured correctly" |
| `SqlException` | Generic message + "nothing was saved" + error number |
| `OperationCanceledException` | Silent — the user cancelled |
| anything else | "unexpected problem", tell your administrator |

`AsyncUi.RunAsync` (`Common/AsyncUi.cs`) is the wrapper around every database call from the UI:

```csharp
var products = await AsyncUi.RunAsync(this, "load the product list",
    () => _services.Inventory.SearchAsync(searchTextBox.Text));

if (products is null) return;   // failed, already reported
```

It sets the wait cursor, awaits, catches everything through `UiFeedback.ShowError`, and restores the cursor in `finally` (guarded by `IsDisposed` in case the form closed mid-await). The generic overload returns `default` (i.e. `null`) on failure, so callers check for null and bail — the error has already been shown with the right wording.

Note the `.ConfigureAwait(true)` on UI calls — continuations must resume on the UI thread. Business and data layers use `.ConfigureAwait(false)` throughout since they don't touch controls.

---

## 10. One request, end to end

Scanning a barcode and completing a sale:

```
User types SKU + Enter in PosForm
  → PosForm.SearchTextBox_KeyDown
  → AsyncUi.RunAsync(...)                       [wait cursor on]
  → InventoryService.GetBySkuAsync("MLK-001")   [trims, rejects blank]
  → ProductRepository.GetBySkuAsync
  → SqlRepositoryBase.QuerySingleAsync
  → SqlConnectionFactory.CreateOpenConnectionAsync
  → SELECT ... WHERE p.SKU = @SKU AND p.IsActive = 1
  → MapProduct → Product.FromDatabase
  ← Product bubbles back up
  → Cart.AddItem(product, qty)                  [stock + active checks]
  → PosForm.RefreshCart → UpdateTotals
  → SaleService.QuoteCart → DiscountStrategy.Apply
  → labels updated

User enters cash, presses F9
  → SaleService.CheckoutAsync
  → ValidateStockAsync (fresh DB read per line)
  → new Sale(...) + SaleDetail.FromCartItem per line
  → SaleRepository.SaveSaleAsync
       BEGIN TRAN → INSERT Sales → per line: INSERT SaleDetails + guarded UPDATE Products → COMMIT
  ← Sale with real SaleId and SaleDate
  → ReceiptForm.ShowDialog
  → ResetForNextCustomer + LoadProductsAsync
```

Every layer boundary is crossed through an interface, and the only place the SQL transaction exists is the one method that actually needs it.
