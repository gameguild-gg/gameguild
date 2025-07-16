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
/// Comprehensive tests for Product Query handlers
/// </summary>
public class ProductQueryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IServiceProvider _serviceProvider;
    private readonly IMediator _mediator;
    private readonly Mock<IUserContext> _mockUserContext;
    private readonly Mock<ITenantContext> _mockTenantContext;

    public ProductQueryTests()
    {
        var services = new ServiceCollection();
        
        // Add database context
        services.AddDbContextFactory<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));
        
        services.AddScoped<ApplicationDbContext>(provider => {
            var factory = provider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
            return factory.CreateDbContext();
        });
        
        // Add logging
        services.AddLogging(builder => builder.AddConsole());
        
        // Add MediatR
        services.AddMediatR(typeof(ProductQueryHandlers).Assembly);
        
        // Mock contexts
        _mockUserContext = new Mock<IUserContext>();
        _mockTenantContext = new Mock<ITenantContext>();
        
        services.AddSingleton(_mockUserContext.Object);
        services.AddSingleton(_mockTenantContext.Object);
        
        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<ApplicationDbContext>();
        _mediator = _serviceProvider.GetRequiredService<IMediator>();
        
        // Setup default mock behavior
        _mockUserContext.Setup(x => x.IsAuthenticated).Returns(true);
        _mockTenantContext.Setup(x => x.TenantId).Returns(Guid.NewGuid());
    }

    [Fact]
    public async Task GetProductByIdQuery_Should_Return_Product_When_Exists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SeedTestUser(userId, "Test Creator");
        
        var product = await CreateTestProduct("Test Product", userId);

        var query = new GetProductByIdQuery
        {
            ProductId = product.Id,
            IncludePricing = true,
            IncludePrograms = true
        };

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(product.Id, result.Id);
        Assert.Equal("Test Product", result.Name);
    }

    [Fact]
    public async Task GetProductByIdQuery_Should_Return_Null_When_Not_Exists()
    {
        // Arrange
        var query = new GetProductByIdQuery
        {
            ProductId = Guid.NewGuid() // Non-existent product
        };

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetProductsQuery_Should_Return_All_Products()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SeedTestUser(userId, "Test Creator");
        
        await CreateTestProduct("Product 1", userId, ContentStatus.Published);
        await CreateTestProduct("Product 2", userId, ContentStatus.Published);
        await CreateTestProduct("Product 3", userId, ContentStatus.Draft);

        var query = new GetProductsQuery
        {
            Take = 10
        };

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count());
    }

    [Fact]
    public async Task GetProductsQuery_Should_Filter_By_Status()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SeedTestUser(userId, "Test Creator");
        
        await CreateTestProduct("Published Product 1", userId, ContentStatus.Published);
        await CreateTestProduct("Published Product 2", userId, ContentStatus.Published);
        await CreateTestProduct("Draft Product", userId, ContentStatus.Draft);

        var query = new GetProductsQuery
        {
            Status = ContentStatus.Published,
            Take = 10
        };

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, p => Assert.Equal(ContentStatus.Published, p.Status));
    }

    [Fact]
    public async Task GetProductsQuery_Should_Filter_By_Type()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SeedTestUser(userId, "Test Creator");
        
        await CreateTestProduct("Program Product", userId, type: GameGuild.Common.ProductType.Program);
        await CreateTestProduct("Bundle Product", userId, type: GameGuild.Common.ProductType.Bundle);
        await CreateTestProduct("Course Product", userId, type: GameGuild.Common.ProductType.Program);

        var query = new GetProductsQuery
        {
            Type = GameGuild.Common.ProductType.Program,
            Take = 10
        };

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(GameGuild.Common.ProductType.Program, result.First().Type);
    }

    [Fact]
    public async Task GetProductsQuery_Should_Filter_By_Creator()
    {
        // Arrange
        var user1Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();
        await SeedTestUser(user1Id, "Creator 1");
        await SeedTestUser(user2Id, "Creator 2");
        
        await CreateTestProduct("Product by Creator 1", user1Id);
        await CreateTestProduct("Another Product by Creator 1", user1Id);
        await CreateTestProduct("Product by Creator 2", user2Id);

        var query = new GetProductsQuery
        {
            CreatorId = user1Id,
            Take = 10
        };

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, p => Assert.Equal(user1Id, p.CreatorId));
    }

    [Fact]
    public async Task GetProductsQuery_Should_Search_By_Name()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SeedTestUser(userId, "Test Creator");
        
        await CreateTestProduct("JavaScript Course", userId);
        await CreateTestProduct("Python Programming", userId);
        await CreateTestProduct("Java Fundamentals", userId);

        var query = new GetProductsQuery
        {
            SearchTerm = "Java",
            Take = 10
        };

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count()); // Should match "JavaScript" and "Java"
        Assert.All(result, p => Assert.Contains("Java", p.Name, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetProductsQuery_Should_Handle_Pagination()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SeedTestUser(userId, "Test Creator");
        
        // Create 10 products
        for (int i = 1; i <= 10; i++)
        {
            await CreateTestProduct($"Product {i}", userId);
        }

        var query = new GetProductsQuery
        {
            Skip = 3,
            Take = 4
        };

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result.Count());
    }

    [Fact]
    public async Task GetProductsQuery_Should_Sort_By_CreatedAt_Descending()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SeedTestUser(userId, "Test Creator");
        
        var product1 = await CreateTestProduct("Product 1", userId);
        await Task.Delay(10); // Ensure different timestamps
        var product2 = await CreateTestProduct("Product 2", userId);
        await Task.Delay(10);
        var product3 = await CreateTestProduct("Product 3", userId);

        var query = new GetProductsQuery
        {
            SortBy = "CreatedAt",
            SortDirection = "DESC",
            Take = 10
        };

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.NotNull(result);
        var productList = result.ToList();
        Assert.Equal(3, productList.Count);
        
        // Should be ordered by CreatedAt descending (newest first)
        Assert.True(productList[0].CreatedAt >= productList[1].CreatedAt);
        Assert.True(productList[1].CreatedAt >= productList[2].CreatedAt);
    }

    [Fact]
    public async Task GetUserProductsQuery_Should_Return_User_Products()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        await SeedTestUser(userId, "Test User");
        await SeedTestUser(creatorId, "Test Creator");
        
        var product1 = await CreateTestProduct("Product 1", creatorId);
        var product2 = await CreateTestProduct("Product 2", creatorId);
        
        // Create user product relationships
        await CreateUserProduct(userId, product1.Id, ProductAcquisitionType.Purchase);
        await CreateUserProduct(userId, product2.Id, ProductAcquisitionType.Gift);

        var query = new GetUserProductsQuery
        {
            UserId = userId
        };

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, up => Assert.Equal(userId, up.UserId));
    }

    [Fact]
    public async Task GetUserProductsQuery_Should_Filter_By_Acquisition_Type()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        await SeedTestUser(userId, "Test User");
        await SeedTestUser(creatorId, "Test Creator");
        
        var product1 = await CreateTestProduct("Product 1", creatorId);
        var product2 = await CreateTestProduct("Product 2", creatorId);
        
        await CreateUserProduct(userId, product1.Id, ProductAcquisitionType.Purchase);
        await CreateUserProduct(userId, product2.Id, ProductAcquisitionType.Gift);

        var query = new GetUserProductsQuery
        {
            UserId = userId,
            AcquisitionType = ProductAcquisitionType.Purchase
        };

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(ProductAcquisitionType.Purchase, result.First().AcquisitionType);
    }

    [Fact]
    public async Task GetProductStatsQuery_Should_Return_Statistics()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SeedTestUser(userId, "Test Creator");
        
        // Create products with different statuses
        await CreateTestProduct("Published Product 1", userId, ContentStatus.Published);
        await CreateTestProduct("Published Product 2", userId, ContentStatus.Published);
        await CreateTestProduct("Draft Product", userId, ContentStatus.Draft);

        var query = new GetProductStatsQuery();

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalProducts);
        Assert.Equal(2, result.PublishedProducts);
        Assert.Equal(1, result.DraftProducts);
    }

    // Helper methods
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

    private async Task<Product> CreateTestProduct(
        string name, 
        Guid creatorId, 
        ContentStatus status = ContentStatus.Published,
        GameGuild.Common.ProductType type = GameGuild.Common.ProductType.Program)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = $"Description for {name}",
            ShortDescription = $"Short description for {name}",
            Type = type,
            Status = status,
            Visibility = AccessLevel.Public,
            CreatorId = creatorId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return product;
    }

    private async Task<UserProduct> CreateUserProduct(
        Guid userId, 
        Guid productId, 
        ProductAcquisitionType acquisitionType)
    {
        var userProduct = new UserProduct
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ProductId = productId,
            AcquisitionType = acquisitionType,
            AccessStatus = ProductAccessStatus.Active,
            PricePaid = 0,
            Currency = "USD"
        };

        _context.UserProducts.Add(userProduct);
        await _context.SaveChangesAsync();
        return userProduct;
    }

    public void Dispose()
    {
        _context?.Dispose();
        (_serviceProvider as IDisposable)?.Dispose();
    }
}
