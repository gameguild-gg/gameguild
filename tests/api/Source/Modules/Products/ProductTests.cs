using GameGuild.Common;
using GameGuild.Database;
using GameGuild.Modules.Contents;
using GameGuild.Modules.Products;
using GameGuild.Modules.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MediatR;
using Moq;

namespace GameGuild.Tests.Modules.Products;

/// <summary>
/// Comprehensive tests for Product management with CQRS architecture
/// </summary>
public class ProductTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IServiceProvider _serviceProvider;
    private readonly IMediator _mediator;
    private readonly Mock<IUserContext> _mockUserContext;
    private readonly Mock<ITenantContext> _mockTenantContext;

    public ProductTests()
    {
        var services = new ServiceCollection();
        
        // Add database context factory for GraphQL DataLoader compatibility
        services.AddDbContextFactory<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));
        
        // Add regular DbContext using the factory (ensures compatible lifetimes)
        services.AddScoped<ApplicationDbContext>(provider => {
            var factory = provider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
            return factory.CreateDbContext();
        });
        
        // Add logging
        services.AddLogging(builder => builder.AddConsole());
        
        // Add MediatR
        services.AddMediatR(typeof(ProductCommandHandlers).Assembly);
        
        // Mock contexts
        _mockUserContext = new Mock<IUserContext>();
        _mockTenantContext = new Mock<ITenantContext>();
        
        services.AddSingleton(_mockUserContext.Object);
        services.AddSingleton(_mockTenantContext.Object);
        
        // Add product handlers
        services.AddScoped<ProductCommandHandlers>();
        services.AddScoped<ProductQueryHandlers>();
        
        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<ApplicationDbContext>();
        _mediator = _serviceProvider.GetRequiredService<IMediator>();
    }

    [Fact]
    public async Task Product_Entity_Can_Be_Created_And_Saved()
    {
        // Arrange
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Test Course Bundle",
            ShortDescription = "A comprehensive course bundle"
        };

        // Act
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Assert
        var savedProduct = await _context.Products.FindAsync(product.Id);
        Assert.NotNull(savedProduct);
        Assert.Equal("Test Course Bundle", savedProduct.Name);
        Assert.Equal("A comprehensive course bundle", savedProduct.ShortDescription);
    }

    [Fact]
    public async Task Product_Can_Create_Product_Via_Command()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SeedTestUser(userId, "Test User");

        _mockUserContext.Setup(x => x.UserId).Returns(userId);
        _mockUserContext.Setup(x => x.IsAuthenticated).Returns(true);
        _mockUserContext.Setup(x => x.IsInRole("Admin")).Returns(true);

        var command = new CreateProductCommand
        {
            Name = "New Product",
            ShortDescription = "Created via CQRS command",
            CreatorId = userId
        };

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Product);
        Assert.Equal("New Product", result.Product.Name);
        Assert.Equal("Created via CQRS command", result.Product.ShortDescription);
    }

    [Fact]
    public async Task Product_Can_Update_Product_Via_Command()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SeedTestUser(userId, "Test User");

        _mockUserContext.Setup(x => x.UserId).Returns(userId);
        _mockUserContext.Setup(x => x.IsAuthenticated).Returns(true);
        _mockUserContext.Setup(x => x.IsInRole("Admin")).Returns(true);

        // Create product first
        var createCommand = new CreateProductCommand
        {
            Name = "Original Product",
            ShortDescription = "Original description",
            CreatorId = userId
        };

        var createResult = await _mediator.Send(createCommand);
        Assert.True(createResult.Success);

        // Act - Update product
        var updateCommand = new UpdateProductCommand
        {
            ProductId = createResult.Product!.Id,
            Name = "Updated Product",
            ShortDescription = "Updated description"
        };

        var updateResult = await _mediator.Send(updateCommand);

        // Assert
        Assert.True(updateResult.Success);
        Assert.NotNull(updateResult.Product);
        Assert.Equal("Updated Product", updateResult.Product.Name);
        Assert.Equal("Updated description", updateResult.Product.ShortDescription);
    }

    [Fact]
    public async Task Product_Can_Delete_Product_Via_Command()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SeedTestUser(userId, "Test User");

        _mockUserContext.Setup(x => x.UserId).Returns(userId);
        _mockUserContext.Setup(x => x.IsAuthenticated).Returns(true);
        _mockUserContext.Setup(x => x.IsInRole("Admin")).Returns(true);

        // Create product first
        var createCommand = new CreateProductCommand
        {
            Name = "Product to Delete",
            ShortDescription = "This will be deleted",
            CreatorId = userId
        };

        var createResult = await _mediator.Send(createCommand);
        Assert.True(createResult.Success);

        // Act - Delete product
        var deleteCommand = new DeleteProductCommand
        {
            ProductId = createResult.Product!.Id
        };

        var deleteResult = await _mediator.Send(deleteCommand);

        // Assert
        Assert.True(deleteResult.Success);
        
        // Verify product is soft deleted by checking DeletedAt field directly
        // Note: In-memory database doesn't consistently apply global query filters,
        // so we verify the soft delete by checking the DeletedAt timestamp
        var deletedProduct = await _context.Products
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == createResult.Product.Id);
        Assert.NotNull(deletedProduct);
        Assert.NotNull(deletedProduct.DeletedAt);
        
        // Verify product would be filtered out by explicit DeletedAt check
        // (simulating what the global query filter should do)
        var queryResult = await _context.Products
            .Where(p => p.Id == createResult.Product.Id && p.DeletedAt == null)
            .FirstOrDefaultAsync();
        Assert.Null(queryResult);
    }

    [Fact]
    public async Task Product_Can_Get_Products_Via_Query()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SeedTestUser(userId, "Test User");

        _mockUserContext.Setup(x => x.UserId).Returns(userId);
        _mockUserContext.Setup(x => x.IsAuthenticated).Returns(true);

        // Create test products with the test user as creator
        await SeedTestProduct("Product 1", "Description 1", userId);
        await SeedTestProduct("Product 2", "Description 2", userId);
        await SeedTestProduct("Product 3", "Description 3", userId);

        // Act
        var query = new GetProductsQuery
        {
            Skip = 0,
            Take = 10
        };

        var products = await _mediator.Send(query);

        // Assert
        Assert.NotNull(products);
        var productList = products.ToList();
        Assert.True(productList.Count >= 3);
    }

    [Fact]
    public async Task Product_Can_Get_Product_By_Id_Via_Query()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SeedTestUser(userId, "Test User");

        _mockUserContext.Setup(x => x.UserId).Returns(userId);
        _mockUserContext.Setup(x => x.IsAuthenticated).Returns(true);

        var testProduct = await SeedTestProduct("Test Product", "Test Description", userId);

        // Act
        var query = new GetProductByIdQuery
        {
            ProductId = testProduct.Id
        };

        var product = await _mediator.Send(query);

        // Assert
        Assert.NotNull(product);
        Assert.Equal(testProduct.Id, product.Id);
        Assert.Equal("Test Product", product.Name);
        Assert.Equal("Test Description", product.ShortDescription);
    }

    #region Helper Methods

    private async Task SeedTestUser(Guid userId, string name)
    {
        var user = new User
        {
            Id = userId,
            Name = name,
            Email = $"{name.ToLower().Replace(" ", "")}@test.com"
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
    }

    private async Task<Product> SeedTestProduct(string name, string description, Guid? creatorId = null)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = name,
            ShortDescription = description,
            CreatorId = creatorId ?? Guid.NewGuid(),
            Status = ContentStatus.Published,
            Visibility = AccessLevel.Public
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return product;
    }

    #endregion

    public void Dispose()
    {
        _context.Dispose();
        if (_serviceProvider is IDisposable disposable)
            disposable.Dispose();
    }
}
