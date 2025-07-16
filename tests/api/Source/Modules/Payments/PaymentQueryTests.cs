using GameGuild.Common;
using GameGuild.Database;
using GameGuild.Modules.Payments;
using GameGuild.Modules.Products;
using GameGuild.Modules.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MediatR;
using Moq;

namespace GameGuild.Tests.Modules.Payments;

/// <summary>
/// Comprehensive tests for Payment Query handlers
/// </summary>
public class PaymentQueryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IServiceProvider _serviceProvider;
    private readonly IMediator _mediator;
    private readonly Mock<IUserContext> _mockUserContext;
    private readonly Mock<ITenantContext> _mockTenantContext;

    public PaymentQueryTests()
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
        services.AddMediatR(typeof(PaymentQueryHandlers).Assembly);
        
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
    public async Task GetPaymentByIdQuery_Should_Return_Payment_When_Exists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await SeedTestUser(userId, "Test User");
        await SeedTestProduct(productId, "Test Product", userId);
        
        var payment = await CreateTestPayment(userId, productId, PaymentStatus.Completed);

        var query = new GetPaymentByIdQuery
        {
            PaymentId = payment.Id
        };

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(payment.Id, result.Id);
        Assert.Equal(PaymentStatus.Completed, result.Status);
        Assert.Equal(99.99m, result.Amount);
        Assert.Equal("USD", result.Currency);
    }

    [Fact]
    public async Task GetPaymentByIdQuery_Should_Return_Null_When_Not_Exists()
    {
        // Arrange
        var query = new GetPaymentByIdQuery
        {
            PaymentId = Guid.NewGuid() // Non-existent payment
        };

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPaymentsQuery_Should_Return_All_Payments()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await SeedTestUser(userId, "Test User");
        await SeedTestProduct(productId, "Test Product", userId);
        
        await CreateTestPayment(userId, productId, PaymentStatus.Completed);
        await CreateTestPayment(userId, productId, PaymentStatus.Failed);
        await CreateTestPayment(userId, productId, PaymentStatus.Pending);

        var query = new GetPaymentsQuery
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
    public async Task GetPaymentsQuery_Should_Filter_By_Status()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await SeedTestUser(userId, "Test User");
        await SeedTestProduct(productId, "Test Product", userId);
        
        await CreateTestPayment(userId, productId, PaymentStatus.Completed);
        await CreateTestPayment(userId, productId, PaymentStatus.Completed);
        await CreateTestPayment(userId, productId, PaymentStatus.Failed);

        var query = new GetPaymentsQuery
        {
            Status = PaymentStatus.Completed,
            Take = 10
        };

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, p => Assert.Equal(PaymentStatus.Completed, p.Status));
    }

    [Fact]
    public async Task GetPaymentsQuery_Should_Filter_By_User()
    {
        // Arrange
        var user1Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await SeedTestUser(user1Id, "Test User 1");
        await SeedTestUser(user2Id, "Test User 2");
        await SeedTestProduct(productId, "Test Product", user1Id);
        
        await CreateTestPayment(user1Id, productId, PaymentStatus.Completed);
        await CreateTestPayment(user1Id, productId, PaymentStatus.Completed);
        await CreateTestPayment(user2Id, productId, PaymentStatus.Completed);

        var query = new GetPaymentsQuery
        {
            UserId = user1Id,
            Take = 10
        };

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, p => Assert.Equal(user1Id, p.UserId));
    }

    [Fact]
    public async Task GetPaymentsQuery_Should_Filter_By_Product()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product1Id = Guid.NewGuid();
        var product2Id = Guid.NewGuid();
        await SeedTestUser(userId, "Test User");
        await SeedTestProduct(product1Id, "Test Product 1", userId);
        await SeedTestProduct(product2Id, "Test Product 2", userId);
        
        await CreateTestPayment(userId, product1Id, PaymentStatus.Completed);
        await CreateTestPayment(userId, product1Id, PaymentStatus.Completed);
        await CreateTestPayment(userId, product2Id, PaymentStatus.Completed);

        var query = new GetPaymentsQuery
        {
            ProductId = product1Id,
            Take = 10
        };

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, p => Assert.Equal(product1Id, p.ProductId));
    }

    [Fact]
    public async Task GetPaymentsQuery_Should_Filter_By_Date_Range()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await SeedTestUser(userId, "Test User");
        await SeedTestProduct(productId, "Test Product", userId);
        
        var startDate = DateTime.UtcNow.AddDays(-10);
        var endDate = DateTime.UtcNow.AddDays(-5);
        
        await CreateTestPayment(userId, productId, PaymentStatus.Completed, startDate.AddDays(1));
        await CreateTestPayment(userId, productId, PaymentStatus.Completed, startDate.AddDays(2));
        await CreateTestPayment(userId, productId, PaymentStatus.Completed, DateTime.UtcNow); // Outside range

        var query = new GetPaymentsQuery
        {
            StartDate = startDate,
            EndDate = endDate,
            Take = 10
        };

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, p => Assert.True(p.CreatedAt >= startDate && p.CreatedAt <= endDate));
    }

    [Fact]
    public async Task GetPaymentsQuery_Should_Handle_Pagination()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await SeedTestUser(userId, "Test User");
        await SeedTestProduct(productId, "Test Product", userId);
        
        // Create 10 payments
        for (int i = 0; i < 10; i++)
        {
            await CreateTestPayment(userId, productId, PaymentStatus.Completed);
        }

        var query = new GetPaymentsQuery
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
    public async Task GetPaymentsQuery_Should_Sort_By_CreatedAt_Descending()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await SeedTestUser(userId, "Test User");
        await SeedTestProduct(productId, "Test Product", userId);
        
        var payment1 = await CreateTestPayment(userId, productId, PaymentStatus.Completed, DateTime.UtcNow.AddDays(-3));
        var payment2 = await CreateTestPayment(userId, productId, PaymentStatus.Completed, DateTime.UtcNow.AddDays(-2));
        var payment3 = await CreateTestPayment(userId, productId, PaymentStatus.Completed, DateTime.UtcNow.AddDays(-1));

        var query = new GetPaymentsQuery
        {
            SortBy = "CreatedAt",
            SortDirection = "DESC",
            Take = 10
        };

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.NotNull(result);
        var paymentList = result.ToList();
        Assert.Equal(3, paymentList.Count);
        
        // Should be ordered by CreatedAt descending (newest first)
        Assert.True(paymentList[0].CreatedAt >= paymentList[1].CreatedAt);
        Assert.True(paymentList[1].CreatedAt >= paymentList[2].CreatedAt);
    }

    [Fact]
    public async Task GetUserPaymentsQuery_Should_Return_User_Payments()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await SeedTestUser(userId, "Test User");
        await SeedTestUser(otherUserId, "Other User");
        await SeedTestProduct(productId, "Test Product", userId);
        
        await CreateTestPayment(userId, productId, PaymentStatus.Completed);
        await CreateTestPayment(userId, productId, PaymentStatus.Failed);
        await CreateTestPayment(otherUserId, productId, PaymentStatus.Completed); // Different user

        var query = new GetUserPaymentsQuery
        {
            UserId = userId
        };

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, p => Assert.Equal(userId, p.UserId));
    }

    [Fact]
    public async Task GetPaymentStatsQuery_Should_Return_Statistics()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await SeedTestUser(userId, "Test User");
        await SeedTestProduct(productId, "Test Product", userId);
        
        await CreateTestPayment(userId, productId, PaymentStatus.Completed, amount: 100m);
        await CreateTestPayment(userId, productId, PaymentStatus.Completed, amount: 200m);
        await CreateTestPayment(userId, productId, PaymentStatus.Failed, amount: 150m);
        await CreateTestPayment(userId, productId, PaymentStatus.Pending, amount: 75m);

        var query = new GetPaymentStatsQuery
        {
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(1)
        };

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result.TotalPayments);
        Assert.Equal(2, result.CompletedPayments);
        Assert.Equal(1, result.FailedPayments);
        Assert.Equal(1, result.PendingPayments);
        Assert.Equal(300m, result.TotalRevenue); // Only completed payments
        Assert.Equal(150m, result.AveragePaymentAmount); // 300 / 2 completed payments
    }

    [Fact]
    public async Task GetPaymentsByTransactionIdQuery_Should_Return_Payment()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await SeedTestUser(userId, "Test User");
        await SeedTestProduct(productId, "Test Product", userId);
        
        var transactionId = "txn_unique_123";
        var payment = await CreateTestPayment(userId, productId, PaymentStatus.Completed, transactionId: transactionId);

        var query = new GetPaymentsByTransactionIdQuery
        {
            TransactionId = transactionId
        };

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(payment.Id, result.Id);
        Assert.Equal(transactionId, result.TransactionId);
    }

    [Fact]
    public async Task GetPaymentsByGatewayQuery_Should_Filter_By_Gateway()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await SeedTestUser(userId, "Test User");
        await SeedTestProduct(productId, "Test Product", userId);
        
        await CreateTestPayment(userId, productId, PaymentStatus.Completed, gateway: PaymentGateway.Stripe);
        await CreateTestPayment(userId, productId, PaymentStatus.Completed, gateway: PaymentGateway.Stripe);
        await CreateTestPayment(userId, productId, PaymentStatus.Completed, gateway: PaymentGateway.PayPal);

        var query = new GetPaymentsByGatewayQuery
        {
            Gateway = PaymentGateway.Stripe,
            Take = 10
        };

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, p => Assert.Equal(PaymentGateway.Stripe, p.PaymentGateway));
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

    private async Task SeedTestProduct(Guid productId, string name, Guid creatorId)
    {
        var product = new Product
        {
            Id = productId,
            Name = name,
            Description = $"Description for {name}",
            ShortDescription = $"Short description for {name}",
            Type = Common.ProductType.Program,
            Status = ContentStatus.Published,
            Visibility = AccessLevel.Public,
            CreatorId = creatorId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();
    }

    private async Task<Payment> CreateTestPayment(
        Guid userId, 
        Guid productId, 
        PaymentStatus status, 
        DateTime? createdAt = null,
        decimal amount = 99.99m,
        string transactionId = null,
        PaymentGateway gateway = PaymentGateway.Stripe)
    {
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ProductId = productId,
            Amount = amount,
            Currency = "USD",
            Status = status,
            PaymentMethod = PaymentMethod.CreditCard,
            PaymentGateway = gateway,
            TransactionId = transactionId ?? $"txn_test_{Guid.NewGuid().ToString("N")[..8]}",
            CreatedAt = createdAt ?? DateTime.UtcNow,
            UpdatedAt = createdAt ?? DateTime.UtcNow
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();
        return payment;
    }

    public void Dispose()
    {
        _context?.Dispose();
        _serviceProvider?.Dispose();
    }
}
