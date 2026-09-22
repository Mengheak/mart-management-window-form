# Mini Mart Management System — ការពន្យល់ងាយយល់

ឯកសារនេះពន្យល់ថាកម្មវិធីធ្វើអ្វី, source code រៀបចំយ៉ាងដូចម្ដេច និងអ្វីកើតឡើងពីពេល Sign in រហូតដល់ Checkout។ វាសម្រាប់អ្នកអានដែលមិនទាន់ស្គាល់ codebase ឬត្រូវពន្យល់គម្រោងក្នុង presentation។

> អាន [README.md](README.md) សម្រាប់ install/run, [SYSTEM_FLOW.md](SYSTEM_FLOW.md) សម្រាប់ class និង method លម្អិត និង [DATA_MANAGEMENT.md](DATA_MANAGEMENT.md) សម្រាប់គ្រប់គ្រង database។ Technical terms, identifiers, ឈ្មោះ UI និង code examples រក្សាភាសាដើម ដើម្បីឱ្យផ្គូផ្គងជាមួយ source code។

## របៀបអានឯកសារនេះ

- ផ្នែក 1–3 ពន្យល់គោលបំណង, architecture និង database។
- ផ្នែក 4 ពន្យល់ user journey ពី Startup, Sign in, POS, Checkout និង Admin dashboard។
- ផ្នែក 5–7 ពន្យល់ rules, OOP និងសំណួរដែលគេសួរញឹកញាប់។
- ផ្នែក 8–9 បង្ហាញ file ដែលត្រូវអាន និង commands សម្រាប់ run។

## ពាក្យសំខាន់ៗ

| Technical term | អត្ថន័យក្នុងគម្រោងនេះ |
|---|---|
| **POS (Point of Sale)** | Screen ដែល Cashier ប្រើ search/scan product, គណនា total និង checkout |
| **Model** | Object ដែលតំណាងឱ្យ data និងការពារ rules របស់វា ដូចជា `Product`, `Cart`, `Sale` |
| **Service** | Class ដែលអនុវត្ត business process ដូចជា authentication, inventory និង checkout |
| **Repository** | Class ឬ interface សម្រាប់អាន/សរសេរទិន្នន័យទៅ SQL Server |
| **Interface** | Contract ដែលកំណត់ថា class ត្រូវផ្ដល់ methods អ្វី ដោយមិនបញ្ជាក់ implementation |
| **Transaction** | ក្រុម database operations ដែលត្រូវជោគជ័យទាំងអស់ ឬ rollback ទាំងអស់ |
| **Dependency Injection** | ការប្រគល់ dependency ទៅ class ជំនួសឱ្យ class បង្កើត dependency ដោយខ្លួនឯង |
| **Hash** | តម្លៃដែលគណនាពី password សម្រាប់ verify; មិនមែន password ដើម និងមិនអាច decode ត្រឡប់ធម្មតា |
| **Soft delete** | កំណត់ `IsActive = 0` ជំនួសការលុប row ដើម្បីរក្សា sales history |
| **CRUD** | Create, Read, Update, Delete |

---

## 1. តើកម្មវិធីនេះជាអ្វី?

នេះជាកម្មវិធី **POS និងគ្រប់គ្រង stock សម្រាប់ហាងតូច** ដែលដំណើរការលើ Windows desktop។ Cashier sign in រួច scan ឬ search products, បន្ថែមទៅ cart, ទទួលប្រាក់ និង print receipt។ Admin អាចបន្ថែម ឬកែ products, categories និង staff accounts ព្រមទាំងមើលចំណូល, low stock និង sales reports។ ទិន្នន័យរក្សាក្នុង **SQL Server database** ដូច្នេះមិនបាត់ពេលបិទកម្មវិធីទេ។

**ពន្យល់ត្រឹមមួយប្រយោគ៖** កម្មវិធីជួយលក់ទំនិញ ហើយបន្ថយ stock ដោយស្វ័យប្រវត្តិរាល់ពេល checkout បានជោគជ័យ។

---

## 2. គំនិតសំខាន់៖ បែងចែកជា 3 layers

**3-Tier Architecture** បំបែក UI, business rules និង database access ជាផ្នែកដាច់ពីគ្នា។ អាចប្រៀបធៀបនឹងភោជនីយដ្ឋាន៖

| Layer | ការប្រៀបធៀប | Project | ការទទួលខុសត្រូវ |
|---|---|---|---|
| Presentation | អ្នកបម្រើ | `MiniMart.Presentation` | Windows Forms, buttons, grids, labels; មិនសរសេរ SQL |
| Business Logic | ចុងភៅ និងរូបមន្ត | `MiniMart.BusinessLogic` | Models, services, rules, calculation, validation; មិនមាន SQL ឬ forms |
| Data Access | ឃ្លាំងគ្រឿងផ្សំ | `MiniMart.DataAccess` | Repositories, SQL queries និង database transactions; មិនមាន forms |

អ្នកបម្រើទទួលសំណើ ហើយចុងភៅអនុវត្តតាមរូបមន្ត។ ផ្នែកនីមួយៗមានការងារច្បាស់លាស់។

### តើ layers ទាក់ទងគ្នាយ៉ាងដូចម្ដេច?

```
Presentation  ──────>  BusinessLogic
   (forms)                 ▲  (rules)
      │                    │
      └──>  DataAccess ────┘
               (SQL)
```

Presentation ហៅ Business Logic services។ Services ស្នើទិន្នន័យតាម **interface** ដូចជា contract ថា «អាចរក product តាម SKU បាន» ដោយមិនចាំបាច់ដឹងពី SQL។ DataAccess ជា implementation ដែលអនុវត្ត contract នោះ។ `AppServices` ភ្ជាប់ objects ទាំងនេះជាមួយគ្នា។

ការបែងចែកនេះជួយឱ្យងាយរកបញ្ហា៖ total ខុស → ពិនិត្យ Business Logic; layout ខុស → Presentation; query ត្រឡប់ទិន្នន័យខុស → Data Access។ បើប្ដូរ database provider ការងារសំខាន់ស្ថិតនៅ DataAccess និង configuration/wiring ដោយរក្សា business contracts ដដែល។

### ទំហំ projects តាមការរាប់ក្នុងឯកសារដើម

| Project | Files | Lines ប្រហែល | Framework |
|---|---|---|---|
| `MiniMart.BusinessLogic` | 29 | 1,660 | `net9.0`, plain C# |
| `MiniMart.DataAccess` | 8 | 1,045 | `net9.0`, ADO.NET / `Microsoft.Data.SqlClient` |
| `MiniMart.Presentation` | 23 | 4,285 | `net9.0-windows`, Windows Forms executable |

---

## 3. Database រក្សាអ្វីខ្លះ?

មាន tables ចំនួន 5 ក្នុង [MiniMart_Database_Schema.sql](MiniMart_Database_Schema.sql)៖

```
Categories ──< Products ──< SaleDetails >── Sales >── Users
 (Drinks,      (Milk 1L,     (2 × Milk      (receipt   (admin,
  Snacks…)      $1.20…)       @ $1.20)       #57)       cashier)
```

| Table | ទិន្នន័យដែលរក្សា | ចំណុចសំខាន់ |
|---|---|---|
| `Categories` | ក្រុម products | Name ត្រូវ unique |
| `Products` | Name, SKU, UnitPrice, CostPrice, StockQuantity, ReorderLevel | Database `CHECK` រារាំង stock ក្រោម 0 |
| `Users` | Username, PasswordHash, Role, IsActive | Role ជា `Admin` ឬ `Cashier` |
| `Sales` | Date, total, paid, change និង cashier | មួយ row សម្រាប់ receipt header |
| `SaleDetails` | Product, quantity និង unit price | មួយ row សម្រាប់ line នីមួយៗក្នុង receipt |

**ហេតុអ្វី sale មួយត្រូវការ tables ពីរ?** Receipt មួយអាចមាន products ច្រើន។ `Sales` រក្សាព័ត៌មានរួម ហើយ `SaleDetails` រក្សាទំនិញនីមួយៗ។ `SaleDetails.UnitPrice` រក្សាតម្លៃនៅពេលលក់ ដូច្នេះការប្ដូរតម្លៃ product ថ្ងៃក្រោយមិនកែតម្លៃក្នុង receipt ចាស់ទេ។

---

## 4. ដំណើរការកម្មវិធីមួយជំហានម្ដងៗ

### 4.1 Startup

`Program.cs` ធ្វើការសំខាន់បី៖

1. Initialize Windows Forms។
2. បង្កើត dependencies តាម `AppServices.CreateFromConfiguration()`។
3. បើក login ហើយជ្រើស main screen តាម role។ បើ Sign Out វាត្រឡប់ទៅ login; បើបិទធម្មតា វាចាកចេញពីកម្មវិធី។

**`AppServices` ជាកន្លែងភ្ជាប់ dependencies។** វាបង្កើត concrete repositories រួចប្រគល់ទៅ services។ Forms ទទួល `services` ហើយប្រើ `services.Inventory`, `services.Auth` ជាដើម។

```csharp
// The only place DataAccess is named:
_productRepository = new ProductRepository(connectionFactory);
// Services only ever see the interface:
Inventory = new InventoryService(_productRepository, categoryRepository);
```

`CreateSaleService()` បង្កើត **SaleService ថ្មី** រាល់ពេល ព្រោះ service នេះរក្សា discount ដែលកំពុងប្រើ។ POS នីមួយៗត្រូវមាន discount state ផ្ទាល់ខ្លួន។

### 4.2 Sign in

- បើ `Users` table **ទទេទាំងស្រុង** កម្មវិធីបើក **First-Time Setup** ឱ្យបង្កើត Admin។ មិនមាន default application account ទេ។
- Password រក្សាជា **PBKDF2-HMAC-SHA256 hash**, 100,000 iterations និង random salt ក្នុង format `PBKDF2$100000$<salt>$<hash>`។ ពេល verify ប្រព័ន្ធគណនា hash ឡើងវិញដើម្បីប្រៀបធៀប។
- Username ខុស និង password ខុសបង្ហាញ error message ដូចគ្នា ដើម្បីមិនបង្ហាញថា username ណាមានស្រាប់។
- Sign in ជោគជ័យ៖ `Admin` → dashboard; `Cashier` → POS។ Account ដែល inactive មិនអាចចូលបាន។

### 4.3 POS របស់ Cashier

**បន្ថែម products ទៅ cart** តាមបីវិធី៖

- វាយ name ក្នុង search box ដើម្បី filter grid។
- វាយ ឬ scan SKU ហើយចុច **Enter**។
- Double-click row ឬចុច **Add**។

Cart ជា `Cart` object។ Rules នៅក្នុង model៖ quantity ត្រូវលើស 0, product ត្រូវ active និង quantity សរុបមិនលើស stock។ បើ stock មិនគ្រប់ នឹងមាន `InsufficientStockException`។ `CartItem` រក្សា unit price នៅពេលបន្ថែម។

**Totals៖** ពេល cart ឬ amount paid ផ្លាស់ប្ដូរ form ហៅ `SaleService.QuoteCart(...)`។ វាគណនាដោយមិនទាក់ទង database៖

```
subtotal  = sum of the lines
total     = discount applied to subtotal
change    = paid − total     (if paid is enough)
```

ដោយសារគ្មាន side effects អាចហៅ method នេះញឹកញាប់សម្រាប់ refresh UI បាន។

**Discounts៖** `DiscountStrategy` ជា abstract class មាន `Apply(subtotal)` និង `Description`។ `NoDiscount` មិនបន្ថយតម្លៃ; `PercentageDiscount` បន្ថយតាមភាគរយ; `FlatDiscount` បន្ថយចំនួនថេរ ហើយកំណត់ total ឱ្យមិនក្រោម 0។ នេះជាឧទាហរណ៍ **Polymorphism**។ បើបន្ថែម strategy ថ្មី business calculation អាចប្រើ contract ដដែល ប៉ុន្តែត្រូវបន្ថែមជម្រើស និង wiring នៅ UI ដើម្បីឱ្យអ្នកប្រើអាចជ្រើសវា។

### 4.4 Checkout — ដំណាក់កាលសំខាន់បំផុត

ពេល Cashier ចុច **F9**៖

1. ពិនិត្យថា cart មិនទទេ និង cashier ID ត្រឹមត្រូវ។
2. អាន products ពី database ម្ដងទៀត ដើម្បីពិនិត្យ existence, active status និង stock ថ្មីបំផុត។
3. គណនា total ឡើងវិញ ហើយបដិសេធបើ amount paid មិនគ្រប់។
4. បង្កើត `Sale` និង `SaleDetail` objects។
5. ហៅ repository ដើម្បី save ក្នុង **database transaction** តែមួយ៖

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

**ហេតុអ្វីពិនិត្យ stock ពីរដង?** ការអានមុន save ផ្ដល់ error message ងាយយល់។ ប៉ុន្តែ stock អាចផ្លាស់ប្ដូរនៅចន្លោះពេល read និង write។ `AND StockQuantity >= @Quantity` ក្នុង `UPDATE` ជាការការពារចុងក្រោយ។ បើ Cashiers ពីរលក់ unit ចុងក្រោយដំណាលគ្នា sale ដែល update មិនបាននឹង **ROLLBACK** ទាំងមូល។ Header, lines និង stock updates ក្នុង transaction នោះមិនទុកជាលទ្ធផលពាក់កណ្ដាលទេ។

ក្រោយ save ជោគជ័យ Receipt window បង្ហាញ receipt ជា text ទទឹង 42 columns ដែលអាច print បាន។ Cart និង discount ត្រូវ reset ហើយ product grid reload stock ថ្មី។

### 4.5 Dashboard របស់ Admin

Dashboard បង្ហាញ sales ថ្ងៃនេះ, revenue ថ្ងៃនេះ, low-stock count, active product count និង recent sales។ ទិន្នន័យមកពី Reporting និង Inventory services។ ពេល child window បិទ dashboard refresh ម្ដងទៀត។

| Button | មុខងារ |
|---|---|
| Products | បន្ថែម, កែ, លុប, Adjust Stock និងសម្គាល់ low stock |
| Categories | បន្ថែម, ប្ដូរឈ្មោះ និងលុប |
| Users | បង្កើត, កែ និង deactivate staff accounts |
| Sales History & Reports | Filter តាម date range, transaction details, daily totals និង best sellers |
| Low Stock | Products ដែល stock តិចជាង ឬស្មើ reorder level |
| Point of Sale | ឱ្យ Admin ប្រើ POS បានដែរ |

---

## 5. Rules សំខាន់ប្រាំដែលប្រើជាប្រចាំ

**1. Models ការពារ state របស់ខ្លួន។** `Product.StockQuantity` គ្មាន public setter។ `ReduceStock` និង `AdjustStockTo` ពិនិត្យតម្លៃមុនកែ ហើយមិនឱ្យ stock អវិជ្ជមាន។ នេះគឺ **Encapsulation**។

**2. SQL ប្រើ parameters។** User input មិនត្រូវបានភ្ជាប់ដោយផ្ទាល់ទៅ SQL text ទេ៖

```csharp
parameters.Add("@SKU", SqlDbType.NVarChar, 50).Value = sku;
```

ឧទាហរណ៍ `x'; DROP TABLE Products; --` ត្រូវបានផ្ញើជា data សម្រាប់ search ជំនួស SQL command។ Parameterization ជួយការពារ SQL injection នៅ query paths ទាំងនេះ។

**3. Delete ប្រែជា deactivate បើមាន sales history។** Products ឬ users ដែលមានក្នុង sale ចាស់រក្សា row ដើម្បីឱ្យ receipt អាច reference បាន។ UI ប្រាប់ថាបាន deactivate ឬ delete។ Category ដែលនៅមាន products ត្រូវបានរារាំងមិនឱ្យលុប។

**4. រក្សា active Admin យ៉ាងហោចណាស់ម្នាក់។** មុន demote, deactivate ឬ delete Admin ប្រព័ន្ធពិនិត្យថាមិនទុក active Admin ចំនួន 0។

**5. Errors បង្ហាញតាមប្រភេទ។**

| Error type | អ្វីដែលអ្នកប្រើឃើញ |
|---|---|
| `BusinessRuleException` ដូចជា empty cart ឬ duplicate SKU | Message ដែលពន្យល់បញ្ហាជាក់លាក់ |
| `SqlException` | Generic infrastructure error និងការណែនាំឱ្យ retry |

`AsyncUi.RunAsync` ជួយបង្ហាញ wait cursor, await database operation, ចាប់ exception និងរាយការណ៍តាម `UiFeedback`។ សម្រាប់ generic calls ដែលត្រឡប់ reference type ករណីបរាជ័យត្រឡប់ `null` ហើយ caller ឈប់បន្ត។

---

## 6. Sale មួយពីដើមដល់ចប់

Cashier scan SKU → POS ហៅ Inventory service → service validate input → repository query database → ត្រឡប់ Product → Cart ពិនិត្យ active/stock → UI គណនានិងបង្ហាញ totals។

បន្ទាប់មក Cashier បញ្ចូល amount paid ហើយចុច F9 → `SaleService` validate stock/payment → បង្កើត Sale → repository save header, lines និង stock ក្នុង transaction → បង្ហាញ receipt → reset cart និង reload products។

Diagram ខាងក្រោមរក្សាឈ្មោះ technical flow ដើមសម្រាប់ផ្ទៀងផ្ទាត់ code៖

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

Services ហៅ repositories តាម interfaces។ UI ហៅ service classes ហើយ SQL transaction ស្ថិតក្នុង repository method ដែល save sale។

---

## 7. ចម្លើយសម្រាប់សំណួរពេលបង្ហាញគម្រោង

**ហេតុអ្វីមាន projects បី?** ដើម្បីបំបែក responsibilities និងឱ្យ compiler ជួយគ្រប់គ្រង dependencies។ `MiniMart.BusinessLogic` មិន reference SQL Server provider ឬ Windows Forms ទេ។

**ហេតុអ្វី repository interfaces នៅ BusinessLogic?** `SaleService` ត្រូវការ `ISaleRepository` ខណៈ `SaleRepository` ត្រូវការ `Sale`។ ដាក់ contracts ជាមួយ models ជួយជៀសវាង circular dependency។ នេះគឺ **Dependency Inversion**។

**OOP នៅកន្លែងណា?** Encapsulation នៅ model validation/private setters; Abstraction នៅ `I*Repository`; Polymorphism នៅ `DiscountStrategy` subclasses; Inheritance នៅ `InsufficientStockException : BusinessRuleException`។

**អ្វីរារាំង Cashiers ពីរលក់លើស stock?** Guarded `UPDATE` ដែលមាន `WHERE StockQuantity >= @Quantity` ក្នុង transaction។ បើ 0 rows affected នោះ sale rollback។

**Connection string hard-coded ឬទេ?** វានៅ `App.config` ក្រោមឈ្មោះ `MiniMartDb`។ ក្រោយ deployment អាចកែ configuration ក្បែរ executable ដោយមិន rebuild។ File ដូចគ្នារក្សាព័ត៌មាន store និង currency symbol។

**ហេតុអ្វីប្រើ `async`?** ដើម្បីឱ្យ UI អាចឆ្លើយតបនៅពេលរង់ចាំ database I/O។ UI continuation ប្រើ `ConfigureAwait(true)` ដើម្បីត្រឡប់ទៅ UI thread; Business Logic និង Data Access ប្រើ `ConfigureAwait(false)` ព្រោះមិនប៉ះ controls។

**មុខងារណាមិនទាន់មាន?** គ្មាន Customers table, tax calculation, stored procedures ឬ discount column ក្នុង Sales។ `Sales.TotalAmount` រក្សាតម្លៃក្រោយ discount ហើយ receipt គណនា discount ពី `Subtotal − TotalAmount`។ មូលហេតុនីមួយៗមានក្នុង [README.md](README.md) §8។ កម្មវិធីក៏មិនមាន void/refund UI ដែរ។

---

## 8. តើត្រូវអាន source file ណា?

| អ្វីដែលចង់យល់ | File |
|---|---|
| Entry point និង login/logout loop | `src/MiniMart.Presentation/Program.cs` |
| Dependency wiring | `src/MiniMart.Presentation/AppServices.cs` |
| POS | `src/MiniMart.Presentation/Forms/PosForm.cs` |
| Cart rules | `src/MiniMart.BusinessLogic/Models/Cart.cs` |
| Checkout rules | `src/MiniMart.BusinessLogic/Services/SaleService.cs` |
| Database transaction | `src/MiniMart.DataAccess/Repositories/SaleRepository.cs` |
| SQL helpers | `src/MiniMart.DataAccess/Infrastructure/SqlRepositoryBase.cs` |
| Password hashing | `src/MiniMart.BusinessLogic/Security/Pbkdf2PasswordHasher.cs` |
| Discount strategies | `src/MiniMart.BusinessLogic/Discounts/` |
| Tables និង constraints | `MiniMart_Database_Schema.sql` |

---

## 9. Run ដោយ commands បួន

ត្រូវមាន SQL Server ដំណើរការ និង credentials ត្រឹមត្រូវជាមុន។ Schema command ខាងក្រោមសម្រាប់ database setup ថ្មី; កុំ run លើទិន្នន័យដែលត្រូវរក្សាទុកដោយមិនពិនិត្យ README ជាមុន។

```bash
sqlcmd -S localhost,1434 -U sa -P 'Heak020507#' -b -i MiniMart_Database_Schema.sql
sqlcmd -S localhost,1434 -U sa -P 'Heak020507#' -b -i MiniMart_SeedData.sql
dotnet build MiniMartManagementSystem.sln
dotnet run --project src/MiniMart.Presentation
```

Seed script បន្ថែម 6 categories និង 24 products ក្នុងនោះ 5 products មាន stock ទាបជាង reorder level។ វាមិនបង្កើត users ទេ; ពេល run ដំបូង app ស្នើឱ្យបង្កើត Admin។ មើល [README.md](README.md) §3–§5 សម្រាប់ការដំឡើងពេញលេញ និង non-Docker SQL Server។
