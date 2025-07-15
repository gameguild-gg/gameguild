using GameGuild.Common;
using GameGuild.Database;
using GameGuild.Modules.Products;
using GameGuild.Modules.Products.Services;
using GameGuild.Modules.Programs;
using GameGuild.Modules.Programs.Services;
using GameGuild.Modules.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GameGuild.Tests.Modules.Products;

/// <summary>
/// Comprehensive tests for Product management and Product-Program relationships
/// </summary>
public class ProductTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IServiceProvider _serviceProvider;
    private readonly IProductService _productService;

    public ProductTests()
    {
        var services = new ServiceCollection();
        
        // Add database context
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));
        
        // Add logging
        services.AddLogging(builder => builder.AddConsole());
        
        // Add Product service
        services.AddScoped<IProductService, ProductService>();
        
        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<ApplicationDbContext>();
        _productService = _serviceProvider.GetRequiredService<IProductService>();
    }

    [Fact]
    public async Task Product_Entity_Can_Be_Created_And_Saved()
    {
        // Arrange
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Test Course Bundle",
            Description = "A comprehensive course bundle",
            Price = 149.99m,
            Currency = "USD",
            IsActive = true,
            ProductType = ProductType.Course,
            SalePrice = 99.99m,
            SaleStartDate = DateTime.UtcNow,
            SaleEndDate = DateTime.UtcNow.AddDays(30),
            MaxPurchases = 100,
            CurrentPurchases = 0
        };

        // Act
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Assert
        var savedProduct = await _context.Products.FindAsync(product.Id);
        Assert.NotNull(savedProduct);
        Assert.Equal("Test Course Bundle", savedProduct.Name);
        Assert.Equal(149.99m, savedProduct.Price);
        Assert.Equal("USD", savedProduct.Currency);
        Assert.True(savedProduct.IsActive);
        Assert.Equal(ProductType.Course, savedProduct.ProductType);
        Assert.Equal(99.99m, savedProduct.SalePrice);
    }

    [Fact]
    public async Task ProductService_Can_Create_Product()
    {
        // Arrange
        var product = new Product
        {
            Name = "Service Test Product",
            Description = "Created via service layer",
            Price = 79.99m,
            Currency = "USD",
            IsActive = true,
            ProductType = ProductType.Bundle
        };

        // Act
        var result = await _productService.CreateProductAsync(product);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Service Test Product", result.Name);
        Assert.Equal(79.99m, result.Price);
        Assert.Equal(ProductType.Bundle, result.ProductType);
        Assert.True(result.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task ProductService_Can_Get_Active_Products()
    {
        // Arrange
        await SeedTestProducts();

        // Act
        var activeProducts = await _productService.GetActiveProductsAsync();

        // Assert
        Assert.NotNull(activeProducts);
        var productList = activeProducts.ToList();
        Assert.True(productList.Count >= 2); // At least 2 active products from seed
        Assert.All(productList, p => Assert.True(p.IsActive));
    }

    [Fact]
    public async Task ProductService_Can_Get_Products_By_Type()
    {
        // Arrange
        await SeedTestProducts();

        // Act
        var courseProducts = await _productService.GetProductsByTypeAsync(ProductType.Course);

        // Assert
        Assert.NotNull(courseProducts);
        var productList = courseProducts.ToList();
        Assert.True(productList.Count >= 1);
        Assert.All(productList, p => Assert.Equal(ProductType.Course, p.ProductType));
    }

    [Fact]
    public async Task ProductService_Can_Update_Product_Price()
    {
        // Arrange
        var product = await CreateTestProduct("Price Test Product", 100.00m);

        // Act
        await _productService.UpdatePriceAsync(product.Id, 89.99m, 69.99m);

        // Assert
        var updatedProduct = await _productService.GetProductByIdAsync(product.Id);
        Assert.NotNull(updatedProduct);
        Assert.Equal(89.99m, updatedProduct.Price);
        Assert.Equal(69.99m, updatedProduct.SalePrice);
    }

    [Fact]
    public async Task ProductService_Can_Deactivate_Product()
    {
        // Arrange
        var product = await CreateTestProduct("Deactivation Test", 50.00m);

        // Act
        await _productService.DeactivateProductAsync(product.Id);

        // Assert
        var deactivatedProduct = await _productService.GetProductByIdAsync(product.Id);
        Assert.NotNull(deactivatedProduct);
        Assert.False(deactivatedProduct.IsActive);
    }

    [Fact]
    public async Task ProductService_Can_Check_Purchase_Availability()
    {
        // Arrange
        var product = await CreateTestProduct("Limited Product", 25.00m);
        await _productService.UpdateProductLimitsAsync(product.Id, maxPurchases: 2);

        // Act - First check should be available
        var isAvailable1 = await _productService.IsPurchaseAvailableAsync(product.Id);

        // Simulate purchases
        await _productService.RecordPurchaseAsync(product.Id, Guid.NewGuid());
        await _productService.RecordPurchaseAsync(product.Id, Guid.NewGuid());

        // Act - Should now be unavailable
        var isAvailable2 = await _productService.IsPurchaseAvailableAsync(product.Id);

        // Assert
        Assert.True(isAvailable1);
        Assert.False(isAvailable2);
    }

    [Fact]
    public async Task ProductService_Can_Get_Product_Programs()
    {
        // Arrange
        var product = await CreateTestProduct("Program Bundle", 199.99m);
        var programId1 = await CreateTestProgram("Program 1");
        var programId2 = await CreateTestProgram("Program 2");

        await LinkProductToProgram(product.Id, programId1, ProgramAccessLevel.Full);
        await LinkProductToProgram(product.Id, programId2, ProgramAccessLevel.Preview);

        // Act
        var productPrograms = await _productService.GetProductProgramsAsync(product.Id);

        // Assert
        Assert.NotNull(productPrograms);
        var programList = productPrograms.ToList();
        Assert.Equal(2, programList.Count);
        
        var fullAccess = programList.FirstOrDefault(p => p.AccessLevel == ProgramAccessLevel.Full);
        var previewAccess = programList.FirstOrDefault(p => p.AccessLevel == ProgramAccessLevel.Preview);
        
        Assert.NotNull(fullAccess);
        Assert.NotNull(previewAccess);
        Assert.Equal(programId1, fullAccess.ProgramId);
        Assert.Equal(programId2, previewAccess.ProgramId);
    }

    [Fact]
    public async Task ProductService_Can_Calculate_Effective_Price()
    {
        // Arrange
        var product = await CreateTestProduct("Sale Product", 100.00m);
        
        // Set sale price with valid dates
        await _productService.UpdateSaleAsync(
            product.Id, 
            75.00m, 
            DateTime.UtcNow.AddDays(-1), 
            DateTime.UtcNow.AddDays(7)
        );

        // Act
        var effectivePrice = await _productService.GetEffectivePriceAsync(product.Id);

        // Assert
        Assert.Equal(75.00m, effectivePrice); // Should return sale price

        // Test expired sale
        await _productService.UpdateSaleAsync(
            product.Id, 
            60.00m, 
            DateTime.UtcNow.AddDays(-10), 
            DateTime.UtcNow.AddDays(-1)
        );

        var expiredSalePrice = await _productService.GetEffectivePriceAsync(product.Id);
        Assert.Equal(100.00m, expiredSalePrice); // Should return regular price
    }

    [Fact]
    public async Task ProductService_Can_Get_User_Purchased_Products()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SeedTestUser(userId);

        var product1 = await CreateTestProduct("User Product 1", 50.00m);
        var product2 = await CreateTestProduct("User Product 2", 75.00m);

        await _productService.RecordPurchaseAsync(product1.Id, userId);

        // Act
        var userProducts = await _productService.GetUserPurchasedProductsAsync(userId);

        // Assert
        Assert.NotNull(userProducts);
        var productList = userProducts.ToList();
        Assert.Single(productList);
        Assert.Equal(product1.Id, productList[0].Id);
    }

    [Fact]
    public async Task ProductService_Can_Validate_Product_Bundle()
    {
        // Arrange
        var bundleProduct = await CreateTestProduct("Test Bundle", 200.00m);
        bundleProduct.ProductType = ProductType.Bundle;
        await _context.SaveChangesAsync();

        var programId1 = await CreateTestProgram("Bundle Program 1");
        var programId2 = await CreateTestProgram("Bundle Program 2");

        await LinkProductToProgram(bundleProduct.Id, programId1, ProgramAccessLevel.Full);
        await LinkProductToProgram(bundleProduct.Id, programId2, ProgramAccessLevel.Full);

        // Act
        var isValidBundle = await _productService.ValidateBundleAsync(bundleProduct.Id);

        // Assert
        Assert.True(isValidBundle);

        // Test invalid bundle (no programs)
        var emptyBundle = await CreateTestProduct("Empty Bundle", 100.00m);
        emptyBundle.ProductType = ProductType.Bundle;
        await _context.SaveChangesAsync();

        var isInvalidBundle = await _productService.ValidateBundleAsync(emptyBundle.Id);
        Assert.False(isInvalidBundle);
    }

    [Fact]
    public async Task ProductProgram_Relationship_Can_Be_Created()
    {
        // Arrange
        var product = await CreateTestProduct("Relationship Test", 150.00m);
        var programId = await CreateTestProgram("Test Program");

        var productProgram = new ProductProgram
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            ProgramId = programId,
            AccessLevel = ProgramAccessLevel.Full,
            IncludeInBundle = true,
            SortOrder = 1
        };

        // Act
        _context.ProductPrograms.Add(productProgram);
        await _context.SaveChangesAsync();

        // Assert
        var savedRelation = await _context.ProductPrograms.FindAsync(productProgram.Id);
        Assert.NotNull(savedRelation);
        Assert.Equal(product.Id, savedRelation.ProductId);
        Assert.Equal(programId, savedRelation.ProgramId);
        Assert.Equal(ProgramAccessLevel.Full, savedRelation.AccessLevel);
        Assert.True(savedRelation.IncludeInBundle);
        Assert.Equal(1, savedRelation.SortOrder);
    }

    #region Helper Methods

    private async Task<Product> CreateTestProduct(string name, decimal price)
    {
        var product = new Product
        {
            Name = name,
            Description = $"Description for {name}",
            Price = price,
            Currency = "USD",
            IsActive = true,
            ProductType = ProductType.Course
        };

        return await _productService.CreateProductAsync(product);
    }

    private async Task<Guid> CreateTestProgram(string title)
    {
        var programId = Guid.NewGuid();
        var program = new Program
        {
            Id = programId,
            Title = title,
            Description = $"Description for {title}",
            Visibility = AccessLevel.Public,
            AuthorId = Guid.NewGuid()
        };

        _context.Programs.Add(program);
        await _context.SaveChangesAsync();
        
        return programId;
    }

    private async Task LinkProductToProgram(Guid productId, Guid programId, ProgramAccessLevel accessLevel)
    {
        var productProgram = new ProductProgram
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            ProgramId = programId,
            AccessLevel = accessLevel,
            IncludeInBundle = true,
            SortOrder = 1
        };

        _context.ProductPrograms.Add(productProgram);
        await _context.SaveChangesAsync();
    }

    private async Task SeedTestUser(Guid userId)
    {
        var user = new User
        {
            Id = userId,
            Name = $"Test User {userId:N}",
            Email = $"test_{userId:N}@example.com",
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
    }

    private async Task SeedTestProducts()
    {
        var products = new[]
        {
            new Product
            {
                Name = "Active Course 1",
                Description = "First active course",
                Price = 99.99m,
                Currency = "USD",
                IsActive = true,
                ProductType = ProductType.Course
            },
            new Product
            {
                Name = "Active Bundle 1",
                Description = "First active bundle",
                Price = 199.99m,
                Currency = "USD",
                IsActive = true,
                ProductType = ProductType.Bundle
            },
            new Product
            {
                Name = "Inactive Course",
                Description = "Inactive course",
                Price = 79.99m,
                Currency = "USD",
                IsActive = false,
                ProductType = ProductType.Course
            }
        };

        _context.Products.AddRange(products);
        await _context.SaveChangesAsync();
    }

    #endregion

    public void Dispose()
    {
        _context.Dispose();
        if (_serviceProvider is IDisposable disposable)
            disposable.Dispose();
    }
}
