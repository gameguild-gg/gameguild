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
using ProductType = GameGuild.Common.ProductType;


namespace GameGuild.Tests.Modules.Products;

/// <summary>
/// Comprehensive tests for Product Command handlers
/// </summary>
public class ProductCommandTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IServiceProvider _serviceProvider;
    private readonly IMediator _mediator;
    private readonly Mock<IUserContext> _mockUserContext;
    private readonly Mock<ITenantContext> _mockTenantContext;

    public ProductCommandTests()
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
        services.AddMediatR(typeof(ProductCommandHandlers).Assembly);
        
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
    public async Task CreateProductCommand_Should_Create_Valid_Product()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SeedTestUser(userId, "Test Creator");
        
        _mockUserContext.Setup(x => x.UserId).Returns(userId);
        _mockUserContext.Setup(x => x.IsInRole("Admin")).Returns(true);

        var command = new CreateProductCommand
        {
            Name = "Test Product",
            Description = "A comprehensive test product",
            ShortDescription = "Test product short description",
            Type = ProductType.Program,
            IsBundle = false,
            CreatorId = userId,
            Visibility = AccessLevel.Public,
            Status = ContentStatus.Published
        };

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Product);
        Assert.Equal("Test Product", result.Product.Name);
        Assert.Equal("A comprehensive test product", result.Product.Description);
        Assert.Equal(ProductType.Program, result.Product.Type);
        Assert.False(result.Product.IsBundle);
        Assert.Equal(AccessLevel.Public, result.Product.Visibility);
        Assert.Equal(ContentStatus.Published, result.Product.Status);
    }

    [Fact]
    public async Task CreateProductCommand_Should_Create_Bundle_Product()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SeedTestUser(userId, "Test Creator");
        
        // Create some products to bundle
        var product1 = await CreateTestProduct("Product 1", userId);
        var product2 = await CreateTestProduct("Product 2", userId);

        _mockUserContext.Setup(x => x.UserId).Returns(userId);
        _mockUserContext.Setup(x => x.IsInRole("Admin")).Returns(true);


        var command = new CreateProductCommand
        {
          Name = "Bundle Product",
          Description = "A bundle of products",
          Type = ProductType.Bundle,
          IsBundle = true,
          CreatorId = userId,
          BundleItems = new List<Guid> { product1.Id, product2.Id }
        };
        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Product);
        Assert.Equal("Bundle Product", result.Product.Name);
        Assert.True(result.Product.IsBundle);
        Assert.Equal(ProductType.Bundle, result.Product.Type);
    }

    [Fact]
    public async Task UpdateProductCommand_Should_Update_Existing_Product()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SeedTestUser(userId, "Test Creator");
        
        var product = await CreateTestProduct("Original Product", userId);

        _mockUserContext.Setup(x => x.UserId).Returns(userId);
        _mockUserContext.Setup(x => x.IsInRole("Admin")).Returns(true);

        var command = new UpdateProductCommand
        {
            ProductId = product.Id,
            Name = "Updated Product",
            Description = "Updated description",
            ShortDescription = "Updated short description",
            Status = ContentStatus.Published
        };

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Product);
        Assert.Equal("Updated Product", result.Product.Name);
        Assert.Equal("Updated description", result.Product.Description);
        Assert.Equal("Updated short description", result.Product.ShortDescription);
        Assert.Equal(ContentStatus.Published, result.Product.Status);
    }

    [Fact]
    public async Task UpdateProductCommand_Should_Fail_For_Nonexistent_Product()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SeedTestUser(userId, "Test Creator");

        _mockUserContext.Setup(x => x.UserId).Returns(userId);
        _mockUserContext.Setup(x => x.IsInRole("Admin")).Returns(true);

        var command = new UpdateProductCommand
        {
            ProductId = Guid.NewGuid(), // Non-existent product
            Name = "Updated Product"
        };

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Product not found", result.ErrorMessage);
    }

    [Fact]
    public async Task DeleteProductCommand_Should_Soft_Delete_Product()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SeedTestUser(userId, "Test Creator");
        
        var product = await CreateTestProduct("Product to Delete", userId);

        _mockUserContext.Setup(x => x.UserId).Returns(userId);
        _mockUserContext.Setup(x => x.IsInRole("Admin")).Returns(true);

        var command = new DeleteProductCommand
        {
            ProductId = product.Id
        };

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.Success);
        
        // Verify soft delete
        var deletedProduct = await _context.Products
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == product.Id);
        
        Assert.NotNull(deletedProduct);
        Assert.NotNull(deletedProduct.DeletedAt);
    }

    [Fact]
    public async Task PublishProductCommand_Should_Change_Status_To_Published()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SeedTestUser(userId, "Test Creator");
        
        var product = await CreateTestProduct("Draft Product", userId, ContentStatus.Draft);

        _mockUserContext.Setup(x => x.UserId).Returns(userId);
        _mockUserContext.Setup(x => x.IsInRole("Admin")).Returns(true);

        var command = new PublishProductCommand
        {
            ProductId = product.Id
        };

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Product);
        Assert.Equal(ContentStatus.Published, result.Product.Status);
    }

    [Fact]
    public async Task UnpublishProductCommand_Should_Change_Status_To_Draft()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SeedTestUser(userId, "Test Creator");
        
        var product = await CreateTestProduct("Published Product", userId, ContentStatus.Published);

        _mockUserContext.Setup(x => x.UserId).Returns(userId);
        _mockUserContext.Setup(x => x.IsInRole("Admin")).Returns(true);

        var command = new UnpublishProductCommand
        {
            ProductId = product.Id
        };

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Product);
        Assert.Equal(ContentStatus.Draft, result.Product.Status);
    }

    [Fact]
    public async Task CreateProductCommand_Should_Fail_With_Invalid_Data()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SeedTestUser(userId, "Test Creator");

        _mockUserContext.Setup(x => x.UserId).Returns(userId);
        _mockUserContext.Setup(x => x.IsInRole("Admin")).Returns(true);

        var command = new CreateProductCommand
        {
            Name = "", // Empty name should fail
            CreatorId = userId
        };

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Name is required", result.ErrorMessage);
    }

    [Fact]
    public async Task CreateProductCommand_Should_Fail_For_Unauthorized_User()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SeedTestUser(userId, "Test User");

        _mockUserContext.Setup(x => x.UserId).Returns(userId);
        _mockUserContext.Setup(x => x.IsInRole("Admin")).Returns(false); // Not admin

        var command = new CreateProductCommand
        {
            Name = "Test Product",
            CreatorId = userId
        };

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Unauthorized", result.ErrorMessage);
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

    private async Task<Product> CreateTestProduct(string name, Guid creatorId, ContentStatus status = ContentStatus.Draft)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = $"Description for {name}",
            ShortDescription = $"Short description for {name}",
            Type = ProductType.Program,
            Status = status,
            Visibility = AccessLevel.Public,
            CreatorId = creatorId
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return product;
    }

    public void Dispose()
    {
        _context?.Dispose();
        (_serviceProvider as IDisposable)?.Dispose();
    }
}
