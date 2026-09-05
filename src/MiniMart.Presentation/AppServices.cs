using MiniMart.BusinessLogic.Repositories;
using MiniMart.BusinessLogic.Security;
using MiniMart.BusinessLogic.Services;
using MiniMart.DataAccess.Infrastructure;
using MiniMart.DataAccess.Repositories;

namespace MiniMart.Presentation;

public sealed class AppServices
{
    private readonly ISaleRepository _saleRepository;
    private readonly IProductRepository _productRepository;

    public AuthService Auth { get; }

    public InventoryService Inventory { get; }

    public CategoryService Categories { get; }

    public UserService Users { get; }

    public ReportingService Reporting { get; }

    public string DatabaseDescription { get; }

    public AppServices(ISqlConnectionFactory connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);

        // Data Access Layer — concrete ADO.NET implementations, named only here.
        ICategoryRepository categoryRepository = new CategoryRepository(connectionFactory);
        IUserRepository userRepository = new UserRepository(connectionFactory);
        _productRepository = new ProductRepository(connectionFactory);
        _saleRepository = new SaleRepository(connectionFactory);

        // Business Logic Layer — each service receives only the contracts it needs.
        IPasswordHasher passwordHasher = new Pbkdf2PasswordHasher();

        Auth = new AuthService(userRepository, passwordHasher);
        Users = new UserService(userRepository, passwordHasher);
        Categories = new CategoryService(categoryRepository);
        Inventory = new InventoryService(_productRepository, categoryRepository);
        Reporting = new ReportingService(_saleRepository);

        DatabaseDescription = connectionFactory.SafeDescription;
    }

    public static AppServices CreateFromConfiguration() =>
        new(DatabaseSettings.CreateConnectionFactory());

    public SaleService CreateSaleService() => new(_saleRepository, _productRepository);
}
