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
/// Comprehensive tests for Payment processing with CQRS architecture
/// </summary>
public class PaymentTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IServiceProvider _serviceProvider;
    private readonly IMediator _mediator;
    private readonly Mock<IUserContext> _mockUserContext;
    private readonly Mock<ITenantContext> _mockTenantContext;

    public PaymentTests()
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
        services.AddMediatR(typeof(CreatePaymentCommandHandler).Assembly);
        
        // Mock contexts
        _mockUserContext = new Mock<IUserContext>();
        _mockTenantContext = new Mock<ITenantContext>();
        
        services.AddSingleton(_mockUserContext.Object);
        services.AddSingleton(_mockTenantContext.Object);
        
        // Add payment handlers
        services.AddScoped<CreatePaymentCommandHandler>();
        services.AddScoped<ProcessPaymentCommandHandler>();
        services.AddScoped<RefundPaymentCommandHandler>();
        services.AddScoped<GetPaymentByIdQueryHandler>();
        services.AddScoped<GetUserPaymentsQueryHandler>();
        services.AddScoped<GetProductPaymentsQueryHandler>();
        services.AddScoped<GetPaymentStatsQueryHandler>();
        services.AddScoped<GetRevenueReportQueryHandler>();
        
        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<ApplicationDbContext>();
        _mediator = _serviceProvider.GetRequiredService<IMediator>();
    }

    [Fact]
    public async Task Payment_Entity_Can_Be_Created_And_Saved()
    {
        // Arrange
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Amount = 99.99m,
            Currency = "USD",
            Status = PaymentStatus.Pending,
            Method = PaymentMethod.CreditCard,
            ProviderTransactionId = "pi_test123",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        // Assert
        var savedPayment = await _context.Payments.FindAsync(payment.Id);
        Assert.NotNull(savedPayment);
        Assert.Equal(99.99m, savedPayment.Amount);
        Assert.Equal(PaymentStatus.Pending, savedPayment.Status);
        Assert.Equal(PaymentMethod.CreditCard, savedPayment.Method);
    }

    [Fact]
    public async Task Payment_Can_Create_Payment_Intent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        
        await SeedTestUser(userId, "Test User");
        await SeedTestProduct(productId, "Test Product", 49.99m);

        _mockUserContext.Setup(x => x.UserId).Returns(userId);
        _mockUserContext.Setup(x => x.IsAuthenticated).Returns(true);

        var command = new CreatePaymentCommand
        {
            UserId = userId,
            ProductId = productId,
            Amount = 49.99m,
            Currency = "USD",
            Method = PaymentMethod.CreditCard
        };

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Payment);
        Assert.Equal(49.99m, result.Payment.Amount);
        Assert.Equal(PaymentStatus.Pending, result.Payment.Status);
        Assert.Equal(PaymentMethod.CreditCard, result.Payment.Method);
    }

    [Fact]
    public async Task Payment_Can_Process_Successful_Payment()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        
        await SeedTestUser(userId, "Test User");
        await SeedTestProduct(productId, "Test Product", 49.99m);

        _mockUserContext.Setup(x => x.UserId).Returns(userId);
        _mockUserContext.Setup(x => x.IsAuthenticated).Returns(true);

        // Create payment first
        var createCommand = new CreatePaymentCommand
        {
            UserId = userId,
            ProductId = productId,
            Amount = 49.99m,
            Currency = "USD",
            Method = PaymentMethod.CreditCard
        };

        var createResult = await _mediator.Send(createCommand);
        Assert.True(createResult.Success);

        // Act - Process payment
        var processCommand = new ProcessPaymentCommand
        {
            PaymentId = createResult.Payment!.Id,
            ProviderTransactionId = "pi_success123"
        };

        var processResult = await _mediator.Send(processCommand);

        // Assert
        Assert.True(processResult.Success);
        Assert.NotNull(processResult.Payment);
        Assert.Equal(PaymentStatus.Completed, processResult.Payment.Status);
        Assert.Equal("pi_success123", processResult.Payment.ProviderTransactionId);
        Assert.NotNull(processResult.Payment.ProcessedAt);
    }

    [Fact]
    public async Task Payment_Can_Handle_Failed_Payment()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        
        await SeedTestUser(userId, "Test User");
        await SeedTestProduct(productId, "Test Product", 49.99m);

        _mockUserContext.Setup(x => x.UserId).Returns(userId);
        _mockUserContext.Setup(x => x.IsAuthenticated).Returns(true);

        // Create payment first
        var createCommand = new CreatePaymentCommand
        {
            UserId = userId,
            ProductId = productId,
            Amount = 49.99m,
            Currency = "USD",
            Method = PaymentMethod.CreditCard
        };

        var createResult = await _mediator.Send(createCommand);
        Assert.True(createResult.Success);

        // Act - Process failed payment
        var processCommand = new ProcessPaymentCommand
        {
            PaymentId = createResult.Payment!.Id,
            ProviderTransactionId = "pi_failed123"
        };

        var processResult = await _mediator.Send(processCommand);

        // Assert
        Assert.True(processResult.Success);
        Assert.NotNull(processResult.Payment);
        Assert.Equal(PaymentStatus.Failed, processResult.Payment.Status);
        Assert.Equal("Insufficient funds", processResult.Payment.FailureReason);
        Assert.NotNull(processResult.Payment.FailedAt);
    }

    [Fact]
    public async Task Payment_Can_Process_Refund()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        
        await SeedTestUser(userId, "Test User");
        await SeedTestProduct(productId, "Test Product", 49.99m);

        _mockUserContext.Setup(x => x.UserId).Returns(userId);
        _mockUserContext.Setup(x => x.IsAuthenticated).Returns(true);

        // Create and process payment first
        var completedPayment = await CreateCompletedPayment(userId, productId, 49.99m);

        // Act - Process refund
        var refundCommand = new RefundPaymentCommand
        {
            PaymentId = completedPayment.Id,
            RefundAmount = 49.99m,
            Reason = "Customer request"
        };

        var refundResult = await _mediator.Send(refundCommand);

        // Assert
        Assert.True(refundResult.Success);
        Assert.NotNull(refundResult.Refund);
        Assert.Equal(PaymentStatus.Refunded, refundResult.Refund.Payment.Status);
        Assert.Equal(49.99m, refundResult.Refund.RefundAmount);
        Assert.Equal("Customer request", refundResult.Refund.Reason);
    }

    [Fact]
    public async Task Payment_Can_Get_User_Payment_History()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        
        await SeedTestUser(userId, "Test User");
        await SeedTestProduct(productId, "Test Product", 49.99m);

        _mockUserContext.Setup(x => x.UserId).Returns(userId);
        _mockUserContext.Setup(x => x.IsAuthenticated).Returns(true);

        // Create multiple payments
        await CreateCompletedPayment(userId, productId, 29.99m);
        await CreateCompletedPayment(userId, productId, 39.99m);
        await CreateCompletedPayment(userId, productId, 49.99m);

        // Act
        var query = new GetUserPaymentsQuery
        {
            UserId = userId,
            Skip = 0,
            Take = 10
        };

        var payments = await _mediator.Send(query);

        // Assert
        Assert.NotNull(payments);
        var paymentList = payments.ToList();
        Assert.True(paymentList.Count >= 3);
        Assert.All(paymentList, p => Assert.Equal(userId, p.UserId));
        Assert.All(paymentList, p => Assert.Equal(PaymentStatus.Completed, p.Status));
    }

    [Fact]
    public async Task Payment_Can_Get_Payment_Stats()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        
        await SeedTestUser(userId, "Test User");
        await SeedTestProduct(productId, "Test Product", 49.99m);

        _mockUserContext.Setup(x => x.UserId).Returns(userId);
        _mockUserContext.Setup(x => x.IsAuthenticated).Returns(true);

        // Create test payments
        await CreateCompletedPayment(userId, productId, 100.00m);
        await CreateCompletedPayment(userId, productId, 200.00m);

        // Act
        var query = new GetPaymentStatsQuery
        {
            FromDate = DateTime.UtcNow.AddDays(-30),
            ToDate = DateTime.UtcNow
        };

        var stats = await _mediator.Send(query);

        // Assert
        Assert.NotNull(stats);
        Assert.True(stats.TotalRevenue >= 300.00m);
        Assert.True(stats.TotalPayments >= 2);
    }

    #region Helper Methods

    private async Task SeedTestData()
    {
        // Add any common test data here
    }

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

    private async Task SeedTestProduct(Guid productId, string name, decimal price)
    {
        var product = new Product
        {
            Id = productId,
            Name = name,
            ShortDescription = $"Description for {name}"
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();
    }

    private async Task<Payment> CreateCompletedPayment(Guid userId, Guid productId, decimal amount)
    {
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ProductId = productId,
            Amount = amount,
            Currency = "USD",
            Status = PaymentStatus.Completed,
            Method = PaymentMethod.CreditCard,
            ProviderTransactionId = $"pi_test_{Guid.NewGuid()}",
            CreatedAt = DateTime.UtcNow,
            ProcessedAt = DateTime.UtcNow
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();
        return payment;
    }

    #endregion

    public void Dispose()
    {
        _context.Dispose();
        if (_serviceProvider is IDisposable disposable)
            disposable.Dispose();
    }
}
