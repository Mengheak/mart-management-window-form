# Mini Mart Management System — លំហូរ Source Code

ឯកសារនេះតាមដានការងារពី UI ទៅ services និង database។ វាសម្រាប់អ្នកដែលបានយល់ទិដ្ឋភាពទូទៅពី [PROJECT_EXPLAINED.md](PROJECT_EXPLAINED.md) ហើយចង់ដឹងថា class និង method ណាដំណើរការនៅជំហាននីមួយៗ។ Technical terms, class/method names, identifiers និង code/flow examples រក្សាជាភាសាដើម ដើម្បីងាយផ្ទៀងផ្ទាត់ជាមួយ source code។ Line numbers ជា references ពីឯកសារដើម ហើយអាចផ្លាស់ប្ដូរពេល code ត្រូវបានកែ។

## ផែនទីលំហូរសរុប

```text
Program.Main
  → AppServices creates repositories and services
  → LoginForm authenticates User
  → AdminDashboardForm or PosForm opens
  → Form calls a BusinessLogic Service
  → Service validates rules and calls an I*Repository
  → DataAccess Repository executes parameterized SQL
  → Result returns to Service, then to Form
```

សម្រាប់ checkout មានការបន្ថែម transaction៖

```text
PosForm
  → SaleService.CheckoutAsync
  → SaleRepository.SaveSaleAsync
  → BEGIN TRANSACTION
      INSERT Sales
      INSERT SaleDetails
      UPDATE Products stock
    COMMIT or ROLLBACK
  → ReceiptForm
```

## តួនាទីរបស់ component នីមួយៗ

| Component | តួនាទី | មិនគួរធ្វើអ្វី |
|---|---|---|
| Form | ទទួល user input, ហៅ Service និងបង្ហាញ result | មិនគួរសរសេរ SQL ឬអនុវត្ត stock calculation ដោយខ្លួនឯង |
| Model | រក្សា state និង validate rules របស់ object | មិនគួរបើក database connection |
| Service | សម្របសម្រួល business process និង validation | មិនគួរមាន WinForms controls ឬ raw SQL |
| Repository Interface | កំណត់ data-access contract | មិនមាន UI logic |
| Repository Implementation | Execute parameterized SQL និង map rows | មិនគួរបង្ហាញ MessageBox |
| `AppServices` | បង្កើត និងភ្ជាប់ dependencies | មិនមែនជា business workflow |

## 1. Projects ទាំងបី

Solution បែងចែកជា projects បី ដែលមិនមាន circular reference៖

```
MiniMart.Presentation  (WinForms, net9.0-windows, the .exe)
        │  references ↓
MiniMart.DataAccess    (ADO.NET + SQL Server, net9.0)
        │  references ↓
MiniMart.BusinessLogic (plain C#, net9.0, no dependencies at all)
```

`MiniMart.Presentation` ក៏ reference `MiniMart.BusinessLogic` ដោយផ្ទាល់។ `MiniMart.BusinessLogic` មិនពឹងលើ projects ពីរផ្សេងទៀតទេ។ វាមាន models, business rules និង repository interfaces ដូចជា `IProductRepository`, `ISaleRepository`។ `MiniMart.DataAccess` implement interfaces ទាំងនេះដោយ SQL។ `MiniMart.Presentation` បង្ហាញ forms ហើយហៅ services សម្រាប់ការងារអាជីវកម្ម។

គោលបំណងគឺបំបែក business rules ពី SQL និងបំបែក SQL ពី forms។

---

## 2. Startup

ក្នុង `Program.cs:9`, `Main()`៖

1. ហៅ `ApplicationConfiguration.Initialize()` ដើម្បី initialize WinForms។
2. ហៅ `AppServices.CreateFromConfiguration()` ដើម្បីបង្កើត dependencies។ បើ configuration បង្កើតមិនបាន `UiFeedback.ShowError` បង្ហាញ message box ហើយ app ចាកចេញ។
3. ចូល `while (true)` loop ដើម្បីគាំទ្រ Sign Out និង Sign In ម្ដងទៀត។

លំហូរក្នុង loop៖

```
new LoginForm(services).ShowDialog()
  ├─ not OK  → return (app exits)
  └─ OK      → user.IsAdmin ? AdminDashboardForm : PosForm
                 Application.Run(mainForm)
                   ├─ form closed normally           → return (exit)
                   └─ form implements ILogoutAware
                      and LogOutRequested == true    → loop back to login
```

`ILogoutAware` ក្នុង `Common/ILogoutAware.cs` មាន property `bool LogOutRequested`។ `PosForm` និង `AdminDashboardForm` implement interface នេះ។ ពេលចុច **Sign Out** form កំណត់ flag ជា `true` រួចបិទ។ `Main` ពិនិត្យ flag ហើយត្រឡប់ទៅ `LoginForm`។ បើបិទ main form ធម្មតា app ចាកចេញ។

### AppServices — ភ្ជាប់ dependencies

`AppServices.cs` ជា manual dependency injection composition root។ Constructor បង្កើត repositories មុន services៖

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

`Auth`, `Inventory`, `Categories`, `Users` និង `Reporting` ជា instances ដែលបង្កើតម្តងក្នុង `AppServices` ហើយ share ទៅ forms ក្នុង app lifetime នោះ។ វាមិនមែនជា global static Singleton implementation ទេ។

`CreateSaleService()` (`AppServices.cs:51`) បង្កើត `SaleService` ថ្មីរាល់ពេល ព្រោះ service មាន mutable `DiscountStrategy`។ `PosForm` នីមួយៗទទួល instance ផ្ទាល់ខ្លួន ដើម្បីមិនឱ្យ discount state រួមគ្នា។

Connection string មកពី `App.config` តាម `DatabaseSettings.GetConnectionString()` ដែលស្វែងរក `MiniMartDb`។ បើបាត់ entry នេះ វា throw `ConfigurationErrorsException`។ Server មិនអាចភ្ជាប់បានជាបញ្ហានៅ database call មិនចាំបាច់កើតនៅពេល wiring ទេ។

---

## 3. Login flow

`LoginForm_Load` (`LoginForm.cs:22`)៖

1. បង្ហាញ database description ក្នុង label ដូចជា Server / MiniMartDB។
2. ហៅ `OfferFirstRunSetupAsync()` → `Auth.AnyUsersExistAsync()`។
3. បើ `Users` ទទេ បើក `FirstRunSetupForm` សម្រាប់បង្កើត Admin។ បើ cancel នោះ login form បិទ ហើយ app ចាកចេញ។

Return type `bool?` បែងចែកស្ថានភាព៖ `null` មានន័យថាការពិនិត្យបរាជ័យ ដូចជា DB មិនដំណើរការ; `false` មានន័យថាគ្មាន users។ មានតែ `false` ប៉ុណ្ណោះដែលបើក setup។

`SignInButton_Click` ហៅ `AuthService.AuthenticateAsync(username, password)`៖

```
AuthService.AuthenticateAsync
  ├─ blank username/password        → BusinessRuleException
  ├─ _userRepository.GetByUsernameAsync(username.Trim())
  ├─ user null OR hash mismatch     → BusinessRuleException "Invalid username or password."
  ├─ user.IsActive == false         → BusinessRuleException "account deactivated"
  └─ return User
```

ករណី username មិនមាន និង password ខុសប្រើ message ដូចគ្នា ដើម្បីមិនបង្ហាញថា username ណាមានស្រាប់។ Account ដែល inactive ត្រូវបានបដិសេធដែរ។

`Pbkdf2PasswordHasher` verify hash format `PBKDF2$100000$<base64 salt>$<base64 hash>` ដែលមាន 4 parts បំបែកដោយ `$`។ វាយក salt និង iteration count ពី stored string មកគណនា hash ឡើងវិញ ហើយប្រៀបធៀបតាម `CryptographicOperations.FixedTimeEquals`។ Malformed hash ត្រឡប់ `false` ដោយមិន throw ដើម្បីឱ្យ sign-in បរាជ័យជំនួស crash។

`LoginForm` ចាប់ `BusinessRuleException` ហើយបង្ហាញក្នុង red label ដើម្បីឱ្យអ្នកប្រើកែ input ដោយមិនចាំបាច់បិទ dialog។ Exceptions ផ្សេងទៅ `UiFeedback.ShowError`។

---

## 4. POS flow — ដំណើរការសំខាន់របស់ Cashier

`PosForm` រក្សា `_cart` ជា `Cart`, `_saleService` ជា instance ផ្ទាល់ខ្លួន និង `_cashier` ជា signed-in `User`។

### បន្ថែម products

| សកម្មភាព | Method flow |
|---|---|
| វាយក្នុង search box | `SearchTextBox_TextChanged` → `Inventory.SearchAsync` → refresh grid |
| Scan ឬវាយ code ហើយចុច Enter | `SearchTextBox_KeyDown` → `Inventory.GetBySkuAsync` → `AddProductToCart` |
| Double-click row ឬចុច Add | `AddSelectedProductToCart` → `AddProductToCart` |

`AddProductToCart` ហៅ `_cart.AddItem(product, quantity)`។ Rules នៅ `Cart.AddItem` (`Models/Cart.cs:21`)៖

- Quantity ≤ 0 → `BusinessRuleException`។
- Product inactive → `BusinessRuleException`។
- បើ product នៅក្នុង cart រួច ត្រូវពិនិត្យ quantity សរុបមិនលើស stock; បើលើស → `InsufficientStockException`។
- បង្កើត `CartItem` ថ្មី ឬបន្ថែម quantity លើ line ដែលមានស្រាប់។

`CartItem` snapshot `UnitPrice` នៅពេលបន្ថែម។ ដូច្នេះការកែ product price ក្រោយបន្ថែមមិនប្ដូរ cart price ដែលរក្សារួចទេ។

### គណនា totals ឡើងវិញ

`UpdateTotals()` ដំណើរការក្រោយ cart ផ្លាស់ប្ដូរ និងពេល amount paid ផ្លាស់ប្ដូរ៖

```csharp
var quote = _saleService.QuoteCart(_cart, ReadAmountPaid());
```

`SaleService.QuoteCart` គណនាដោយមិន query database និងគ្មាន side effects៖

```
subtotal       = cart.Subtotal                      (sum of line totals, rounded)
total          = DiscountStrategy.Apply(subtotal)
discountAmount = subtotal - total
paid           = round(amountPaid)
sufficient     = paid >= total
changeDue      = sufficient ? paid - total : 0
```

Method ត្រឡប់ `CheckoutQuote` record។ Form បង្ហាញ **Change Due** ពណ៌បៃតងបើប្រាក់គ្រប់ ឬ **Still Owing** ពណ៌ក្រហមដោយប្រើ `quote.AmountOutstanding`។ អាចហៅ `QuoteCart` ញឹកញាប់បាន ព្រោះវាមិនកែ database។

### Discounts — Strategy pattern

`DiscountStrategy` ជា abstract class ដែលមាន `Apply(decimal subtotal)` និង `Description`។ Implementations បី៖

- `NoDiscount` ប្រើ `NoDiscount.Instance` ហើយត្រឡប់ subtotal ដដែល។
- `PercentageDiscount(p)` validate 0–100 ក្នុង constructor ហើយគណនា `subtotal × (1 − p/100)`។
- `FlatDiscount(amount)` validate non-negative amount, ដកចំនួនថេរ ហើយ clamp total ត្រឹម 0។

`ApplyDiscountButton_Click` (`PosForm.cs:209`) បម្លែង combo box index ទៅ strategy ហើយហៅ `_saleService.ApplyDiscount(strategy)`។ `ApplyDiscount(null)` reset ទៅ `NoDiscount.Instance`។ Strategy ថ្មីអាចបន្ថែមជាថ្មី class ដោយរក្សា calculation contract; UI selection/mapping ត្រូវបន្ថែមផងដែរ។

### Checkout — លំហូរសំខាន់

`CheckoutButton_Click` ហៅ `SaleService.CheckoutAsync(cart, cashierId, amountPaid)`៖

1. Cart ទទេ → `BusinessRuleException`។
2. `cashierId <= 0` → `BusinessRuleException`។
3. `ValidateStockAsync` អាន product ថ្មីពី DB សម្រាប់ line នីមួយៗ។ បើ product បាត់ ឬ inactive → `BusinessRuleException`; stock មិនគ្រប់ → `InsufficientStockException`។
4. `QuoteCart` គណនា totals ឡើងវិញ។
5. បើ paid < total → `BusinessRuleException` ដែលប្រាប់ចំនួនប្រាក់ខ្វះ។
6. `new Sale(cashierId, total, paid)` validate និងគណនា `ChangeDue`។
7. `sale.AddLines(cart.Items.Select(SaleDetail.FromCartItem))` បង្កើត sale lines។
8. `_saleRepository.SaveSaleAsync(sale)` រក្សាទុក sale។

ការអាន stock ក្នុងជំហាន 3 ចាំបាច់ ព្រោះ cart អាចត្រូវបានបង្កើតមុន ហើយ stock ផ្លាស់ប្ដូររួច។ វាជាការពិនិត្យមុន save មិនមែនការធានាផ្នែក concurrency ចុងក្រោយទេ។

### Transaction នៅកន្លែងណា?

`SaleRepository.SaveSaleAsync` (`SaleRepository.cs:19`) បើក database transaction សម្រាប់ save sale និងបន្ថយ stock ជាមួយគ្នា៖

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

`AND StockQuantity >= @Quantity` ក្នុង `UPDATE` ជាការធានាថាមិនដកលើស stock នៅពេល write។ បើមានអ្នកផ្សេងលក់ unit ចុងក្រោយមុន update នេះ នោះ rows affected = 0 ហើយ code throw exception។ Catch block ហៅ `SafeRollbackAsync` ដើម្បី rollback header, sale lines និង stock decrements ក្នុង transaction ទាំងអស់។

`SafeRollbackAsync` មិនឱ្យ rollback exception លាក់ original error។ ការបរាជ័យនៃ connection/rollback ត្រូវពិនិត្យតាម transaction state; error message មួយមិនមែនជា audit record សម្រាប់បញ្ជាក់ commit status ទេ។

### ក្រោយ checkout

```
sale returned → ReceiptForm.ShowDialog()
              → ResetForNextCustomer()  (clear cart, reset discount, clear paid box)
              → LoadProductsAsync()     (reload grid so stock numbers are current)
```

`ReceiptForm` ប្រើ `StringBuilder` បង្កើត text receipt ទទឹង 42 columns។ វាមាន store details ពី `App.config`, header, line items, subtotal, discount, total, paid និង change។ បង្ហាញក្នុង read-only Consolas textbox ហើយអាច render ទៅ `PrintDocument` សម្រាប់ print preview។ Printer error ត្រូវរាយការណ៍ដោយមិនចាត់ទុកថា sale ដែល save រួចបរាជ័យ។

Keyboard shortcuts ក្នុង `PosForm_KeyDown`៖ `F9` សម្រាប់ checkout និង `F2` សម្រាប់ទៅ search។

---

## 5. Admin flow

`AdminDashboardForm` បង្កើត UI ក្នុង code ដោយគ្មាន designer file។ Layout មាន header bar, left nav panel, stat tiles ចំនួន 4, recent-sales grid និង status bar។

`RefreshDashboardAsync` ដំណើរការពេល `Load` និងពេល child form បិទ៖

```
Reporting.GetDailyTotalsAsync(today, today)   → sales count + revenue tiles
Inventory.GetLowStockProductsAsync()          → low stock tile
Inventory.GetProductsAsync()                  → active product count tile
Reporting.GetSalesAsync(today-30, today)      → recent sales grid (top 15)
```

Nav buttons បើក forms តាម `ShowDialog`; `ShowChild` refresh dashboard ក្រោយ form បិទ។

| Button | Form | Services |
|---|---|---|
| Products | `ProductListForm` → `ProductEditForm`, `StockAdjustmentForm` | Inventory, Categories |
| Categories | `CategoryListForm` → `TextPromptForm` | Categories |
| Users | `UserListForm` → `UserEditForm` | Users |
| Sales History & Reports | `SalesHistoryForm` | Reporting |
| Low Stock | `LowStockForm` | Inventory |
| Point of Sale | `PosForm` | Inventory និង SaleService; Admin អាចលក់បានដែរ |

`SalesHistoryForm` មាន from/to date range និង tabs បី៖ **Transactions**, **Daily Totals**, **Best Sellers**។ ជ្រើស sale ក្នុង Transactions ដើម្បី load lines ទៅ grid ខាងក្រោម។ `RefreshAllAsync` update ទិន្នន័យក្នុង tabs។

---

## 6. Rules ពីរដែលប្រើជាប្រចាំ

### Delete ក្លាយជា deactivate បើមាន history

`InventoryService.DeleteProductAsync` និង `UserService.DeleteAsync` ប្រើលំនាំដូចគ្នា៖

```csharp
if (await repo.HasSalesHistoryAsync(id)) {
    entity.Deactivate();
    await repo.UpdateAsync(entity);
    return false;          // "soft deleted"
}
await repo.DeleteAsync(id);
return true;               // "hard deleted"
```

Return `bool` ប្រាប់ថា hard delete ឬ soft delete។ UI ប្រើ `bool?` ដើម្បីបែងចែក `null` = operation បរាជ័យ, `false` = deactivated, `true` = deleted។ UI ប្រាប់លទ្ធផលដើម្បីឱ្យអ្នកប្រើយល់ថា row នៅតែមានសម្រាប់ sales history។

`CategoryService.DeleteAsync` ខុសពីនេះ៖ វារាប់ products ក្នុង category ហើយបដិសេធការលុបបើនៅមាន products ដោយប្រាប់ចំនួន។

### រក្សា last active Admin

`UserService` ហៅ `EnsureAnotherActiveAdminExistsAsync` មុន demote, deactivate ឬ delete active Admin។ បើ active admin count ≤ 1 វា throw ដើម្បីមិនឱ្យ app operation នោះទុកគ្មាន active Admin។

---

## 7. Domain models validate state របស់ខ្លួន

Models ប្រើ controlled setters និង methods ដើម្បីអនុវត្ត validation៖

- `Product.UnitPrice` បដិសេធតម្លៃអវិជ្ជមាន និង round ទៅ 2 decimal places។
- `Product.Name` / `Sku` ត្រូវមានតម្លៃ, trim និងកំណត់ maximum length។
- `Product.CategoryId` ត្រូវ > 0។
- `Category.Name` និង `User.Username` មាន validation ស្រដៀងគ្នា។
- `Sale` constructor បដិសេធ negative amounts និង paid < total ហើយគណនា `ChangeDue`។

Models សម្រាប់ database entities មាន static `FromDatabase(...)` factories ដើម្បី map SQL rows ទៅ objects។ វាអាចកំណត់ identity និង fields ដូចជា `CreatedAt`, `CategoryName` ដែល public constructor មិនកំណត់។

`Sale.FromDatabase` (`Models/Sale.cs:71`) ចាប់ផ្ដើមដោយ `new Sale(cashierId, 0m, 0m)` រួចដាក់ stored values តាម object initializer។ វាបំបែកការបង្កើត sale ថ្មីពីការអាន historical data ដែល database រក្សារួច។

---

## 8. ទិន្នន័យចូល និងចេញពី SQL

`SqlRepositoryBase` ផ្ដល់ helpers ដែលធ្វើលំដាប់៖ open connection → build command → bind parameters → execute → dispose។

| Helper | ការប្រើប្រាស់ |
|---|---|
| `ExecuteNonQueryAsync` | INSERT/UPDATE/DELETE; ត្រឡប់ rows affected |
| `ExecuteScalarAsync<T>` | តម្លៃមួយ ដូចជា `COUNT(1)` |
| `QueryAsync<T>` | Rows ច្រើន តាម `Func<SqlDataReader, T>` mapper |
| `QuerySingleAsync<T>` | Row មួយ ឬ null ដោយ `CommandBehavior.SingleRow` |

Repositories ប្រើ static `MapX` methods។ ឧទាហរណ៍ `SaleRepository.MapSale` អាន columns តាម `GetOrdinal` ហើយហៅ `Sale.FromDatabase`។ `ReadNullableString` គ្រប់គ្រង nullable columns ពី joins។

SQL ប្រើ parameters ដូចជា `parameters.Add("@SKU", SqlDbType.NVarChar, 50).Value = ...` ជំនួសភ្ជាប់ user input ទៅ SQL text។ Decimal parameters កំណត់ `Precision = 10, Scale = 2` ឱ្យត្រូវ `DECIMAL(10,2)`។

ចំណុច SQL សំខាន់ពីរ៖

- `ProductRepository.GetBySkuAsync` filter `AND p.IsActive = 1` ដើម្បីមិនឱ្យ scan discontinued product។
- `SaleRepository.GetDailyTotalsAsync` ប្រើ `OUTER APPLY` សម្រាប់ unit count។ បើ join `SaleDetails` ដោយផ្ទាល់មុន aggregate អាចរាប់ sale header ម្ដងទៀតតាម line នីមួយៗ ហើយធ្វើឱ្យ `SaleCount` និង `TotalRevenue` លើស។

---

## 9. Errors និង async helpers

Business layer មាន exceptions ពីរ៖

- `BusinessRuleException` សម្រាប់បញ្ហាដែលអ្នកប្រើអាចកែបាន ដូចជា empty cart ឬ duplicate SKU។
- `InsufficientStockException : BusinessRuleException` ផ្ទុក `ProductName`, `Requested`, `Available`។

`UiFeedback.ShowError` ក្នុង `Common/UiFeedback.cs` ជ្រើស message តាម exception type៖

| Type | លទ្ធផល |
|---|---|
| `BusinessRuleException` | Warning box ជាមួយ message ជាក់លាក់ |
| `ConfigurationErrorsException` | "The application is not configured correctly" |
| `SqlException` | Generic message, "nothing was saved" និង error number |
| `OperationCanceledException` | មិនបង្ហាញ error សម្រាប់ការលុបចោល |
| ប្រភេទផ្សេង | "unexpected problem" និងណែនាំឱ្យទាក់ទង administrator |

`AsyncUi.RunAsync` ក្នុង `Common/AsyncUi.cs` ជា wrapper សម្រាប់ database calls ពី UI៖

```csharp
var products = await AsyncUi.RunAsync(this, "load the product list",
    () => _services.Inventory.SearchAsync(searchTextBox.Text));

if (products is null) return;   // failed, already reported
```

វាកំណត់ wait cursor, await operation, catch exceptions តាម `UiFeedback.ShowError` ហើយស្ដារ cursor ក្នុង `finally`។ វាពិនិត្យ `IsDisposed` ដើម្បីគ្រប់គ្រងករណី form បិទពេលកំពុង await។ Generic overload ត្រឡប់ `default` ពេលបរាជ័យ; សម្រាប់ reference types វាជា `null` ដូច្នេះ caller ពិនិត្យ null ហើយ return ដោយមិនបង្ហាញ error ស្ទួន។

UI calls ប្រើ `.ConfigureAwait(true)` ដើម្បីឱ្យ continuation ត្រឡប់ទៅ UI thread មុនកែ controls។ Business Logic និង Data Access ប្រើ `.ConfigureAwait(false)` ព្រោះមិនប៉ះ UI controls។

---

## 10. Request មួយពីដើមដល់ចប់

Cashier scan SKU ហើយចុច Enter។ POS ហៅ Inventory service, service validate input ហើយ repository query product។ Cart validate និងរក្សាទុក line; UI refresh totals។ ពេលបញ្ចូល cash ហើយចុច F9, SaleService ពិនិត្យ stock/payment ម្ដងទៀត ហើយ SaleRepository save sale និង stock updates ក្នុង transaction។ ក្រោយ COMMIT បង្ហាញ receipt ហើយ reset cart។

លំហូរលម្អិតជាមួយ method names៖

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

Repository boundary ប្រើ interfaces។ Forms ហៅ services ហើយ concrete dependencies ត្រូវបានភ្ជាប់ក្នុង `AppServices`។ Transaction សម្រាប់ checkout ស្ថិតក្នុង `SaveSaleAsync` ដើម្បីគ្រប់គ្រង sale header, lines និង stock ជាឯកតាតែមួយ។
