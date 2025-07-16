using GameGuild.Common;
using GameGuild.Database;
using GameGuild.Modules.Payments;
using GameGuild.Modules.Products;
using GameGuild.Modules.Users;
using GameGuild.Modules.Programs;
using GameGuild.Modules.Contents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MediatR;
using Moq;

namespace GameGuild.Tests.Modules.Payments;

/// <summary>
/// Comprehensive tests for Payment Command handlers
/// </summary>
public class PaymentCommandTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IServiceProvider _serviceProvider;
    private readonly IMediator _mediator;
    private readonly Mock<IUserContext> _mockUserContext;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly Mock<IPaymentGatewayService> _mockPaymentGateway;

    public PaymentCommandTests()
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
        services.AddMediatR(typeof(CreatePaymentCommandHandler).Assembly);
        
        // Mock contexts and services
        _mockUserContext = new Mock<IUserContext>();
        _mockTenantContext = new Mock<ITenantContext>();
        _mockPaymentGateway = new Mock<IPaymentGatewayService>();
        
        services.AddSingleton(_mockUserContext.Object);
        services.AddSingleton(_mockTenantContext.Object);
        services.AddSingleton(_mockPaymentGateway.Object);
        
        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<ApplicationDbContext>();
        _mediator = _serviceProvider.GetRequiredService<IMediator>();
        
        // Setup default mock behavior
        _mockUserContext.Setup(x => x.IsAuthenticated).Returns(true);
        _mockTenantContext.Setup(x => x.TenantId).Returns(Guid.NewGuid());
        _mockPaymentGateway.Setup(x => x.Gateway).Returns(PaymentGateway.Stripe);
    }

    [Fact]
    public async Task CreatePaymentCommand_Should_Create_Payment_Intent_Successfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await SeedTestUser(userId, "Test User");
        await SeedTestProduct(productId, "Test Product", userId);

        // Setup user context for this specific user
        _mockUserContext.Setup(x => x.UserId).Returns(userId);

        var command = new CreatePaymentCommand
        {
            UserId = userId,
            ProductId = productId,
            Amount = 99.99m,
            Currency = "USD",
            Method = PaymentMethod.CreditCard,
            Description = "Test Product Purchase"
        };

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success, $"Expected success but got error: {result.ErrorMessage}");
        Assert.NotNull(result.Payment);
        Assert.Equal(PaymentStatus.Pending, result.Payment.Status);
        Assert.Equal(99.99m, result.Payment.Amount);
        Assert.Equal("USD", result.Payment.Currency);
        Assert.Equal(userId, result.Payment.UserId);
        Assert.Equal(productId, result.Payment.ProductId);

        // Verify payment is saved in database
        var payment = await _context.Payments.FindAsync(result.Payment.Id);
        Assert.NotNull(payment);
        Assert.Equal(PaymentStatus.Pending, payment.Status);
    }

    [Fact]
    public async Task ProcessPaymentCommand_Should_Complete_Payment_Successfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await SeedTestUser(userId, "Test User");
        await SeedTestProduct(productId, "Test Product", userId);

        // Setup user context for this specific user
        _mockUserContext.Setup(x => x.UserId).Returns(userId);

        // Setup payment gateway mock for success
        _mockPaymentGateway.Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentRequest>()))
            .ReturnsAsync(new PaymentResult 
            { 
                Success = true, 
                TransactionId = "txn_success_123",
                ProcessedAt = DateTime.UtcNow 
            });

        // Step 1: Create Payment Intent
        var createCommand = new CreatePaymentCommand
        {
            UserId = userId,
            ProductId = productId,
            Amount = 99.99m,
            Currency = "USD",
            Method = PaymentMethod.CreditCard
        };

        var createResult = await _mediator.Send(createCommand);
        Assert.True(createResult.Success);
        Assert.NotNull(createResult.Payment);

        // Step 2: Process Payment
        var processCommand = new ProcessPaymentCommand
        {
            PaymentId = createResult.Payment.Id,
            ProviderTransactionId = "txn_success_123"
        };

        // Act
        var processResult = await _mediator.Send(processCommand);

        // Assert
        Assert.NotNull(processResult);
        Assert.True(processResult.Success, $"Expected success but got error: {processResult.ErrorMessage}");
        Assert.NotNull(processResult.Payment);
        Assert.Equal(PaymentStatus.Completed, processResult.Payment.Status);
        Assert.Equal("txn_success_123", processResult.Payment.ProviderTransactionId);

        // Verify payment is updated in database
        var payment = await _context.Payments.FindAsync(processResult.Payment.Id);
        Assert.NotNull(payment);
        Assert.Equal(PaymentStatus.Completed, payment.Status);
    }

    [Fact]
    public async Task ProcessPaymentCommand_Should_Handle_Failed_Payment()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await SeedTestUser(userId, "Test User");
        await SeedTestProduct(productId, "Test Product", userId);

        // Setup user context for this specific user
        _mockUserContext.Setup(x => x.UserId).Returns(userId);

        // Setup payment gateway mock to fail
        _mockPaymentGateway.Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentRequest>()))
            .ReturnsAsync(new PaymentResult 
            { 
                Success = false, 
                ErrorMessage = "Payment failed",
                ProcessedAt = DateTime.UtcNow 
            });

        // Step 1: Create Payment Intent
        var createCommand = new CreatePaymentCommand
        {
            UserId = userId,
            ProductId = productId,
            Amount = 99.99m,
            Currency = "USD",
            Method = PaymentMethod.CreditCard
        };

        var createResult = await _mediator.Send(createCommand);
        Assert.True(createResult.Success, $"Create payment failed: {createResult.ErrorMessage}");
        Assert.NotNull(createResult.Payment);

        // Step 2: Process Payment (should fail)
        var processCommand = new ProcessPaymentCommand
        {
            PaymentId = createResult.Payment.Id,
            ProviderTransactionId = "pi_failed_456"
        };

        // Act
        var result = await _mediator.Send(processCommand);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success, $"Expected success but got error: {result.ErrorMessage}"); // Process command should succeed even if payment fails
        Assert.NotNull(result.Payment);
        Assert.Equal(PaymentStatus.Failed, result.Payment.Status);
        
        // Verify payment is updated in database
        var payment = await _context.Payments.FindAsync(result.Payment.Id);
        Assert.NotNull(payment);
        Assert.Equal(PaymentStatus.Failed, payment.Status);
    }

    [Fact]
    public async Task RefundPaymentCommand_Should_Refund_Payment_Successfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await SeedTestUser(userId, "Test User");
        await SeedTestProduct(productId, "Test Product", userId);

        // Setup user context for this specific user
        _mockUserContext.Setup(x => x.UserId).Returns(userId);

        // Create and process a payment first
        var payment = await CreateTestPayment(userId, productId, PaymentStatus.Completed);

        // Setup refund gateway mock
        _mockPaymentGateway.Setup(x => x.RefundPaymentAsync(It.IsAny<RefundRequest>()))
            .ReturnsAsync(new RefundResult 
            { 
                Success = true, 
                RefundId = "refund_123",
                ProcessedAt = DateTime.UtcNow 
            });

        var command = new RefundPaymentCommand
        {
            PaymentId = payment.Id,
            RefundAmount = 50.00m,
            Reason = "Customer request",
            RefundedBy = userId
        };

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success, $"Expected success but got error: {result.ErrorMessage}");
        Assert.NotNull(result.Refund);
        Assert.Equal(50.00m, result.Refund.RefundAmount);
    }

    [Fact]
    public async Task CancelPaymentCommand_Should_Cancel_Pending_Payment()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await SeedTestUser(userId, "Test User");
        await SeedTestProduct(productId, "Test Product", userId);

        // Setup user context for this specific user
        _mockUserContext.Setup(x => x.UserId).Returns(userId);

        // Create a pending payment
        var payment = await CreateTestPayment(userId, productId, PaymentStatus.Pending);

        var command = new CancelPaymentCommand
        {
            PaymentId = payment.Id,
            Reason = "User cancellation",
            CancelledBy = userId
        };

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success, $"Expected success but got error: {result.ErrorMessage}");
        Assert.NotNull(result.Payment);
        Assert.Equal(PaymentStatus.Cancelled, result.Payment.Status);
    }

    private async Task SeedTestUser(Guid userId, string userName)
    {
        var user = new User 
        { 
            Id = userId, 
            Name = userName,
            Email = $"{userName.ToLowerInvariant()}@test.com",
            CreatedAt = DateTime.UtcNow
        };
        
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
    }

    private async Task SeedTestProduct(Guid productId, string productName, Guid creatorId)
    {
        var product = new Product 
        { 
            Id = productId, 
            Name = productName,
            Description = $"{productName} description",
            Title = productName,
            CreatorId = creatorId,
            Status = ContentStatus.Published,
            Visibility = AccessLevel.Public,
            CreatedAt = DateTime.UtcNow
        };
        
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
    }

    private async Task<Payment> CreateTestPayment(Guid userId, Guid productId, PaymentStatus status)
    {
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ProductId = productId,
            Amount = 99.99m,
            Currency = "USD",
            Status = status,
            Method = PaymentMethod.CreditCard,
            Gateway = PaymentGateway.Stripe,
            CreatedAt = DateTime.UtcNow,
            FinalAmount = 99.99m,
            NetAmount = 95.99m
        };

        if (status == PaymentStatus.Completed)
        {
            payment.ProcessedAt = DateTime.UtcNow;
            payment.ProviderTransactionId = "test_txn_123";
        }

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();
        return payment;
    }

    public void Dispose()
    {
        _context?.Dispose();
        _serviceProvider?.GetService<IServiceScope>()?.Dispose();
    }
}
