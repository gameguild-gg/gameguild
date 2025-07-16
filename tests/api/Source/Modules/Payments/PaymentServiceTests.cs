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
/// Comprehensive integration tests for Payment Service operations
/// </summary>
public class PaymentServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IServiceProvider _serviceProvider;
    private readonly IMediator _mediator;
    private readonly Mock<IUserContext> _mockUserContext;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly Mock<IPaymentGatewayService> _mockPaymentGateway;

    public PaymentServiceTests()
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
        services.AddMediatR(typeof(PaymentCommandHandlers).Assembly);
        
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
    }

    [Fact]
    public async Task ProcessPayment_Should_Handle_Successful_Stripe_Payment()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await SeedTestUser(userId, "Test User");
        await SeedTestProduct(productId, "Test Product", userId);

        _mockPaymentGateway
            .Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentRequest>()))
            .ReturnsAsync(new PaymentResponse
            {
                IsSuccess = true,
                TransactionId = "stripe_txn_success_123",
                Status = PaymentStatus.Completed
            });

        var command = new ProcessPaymentCommand
        {
            UserId = userId,
            ProductId = productId,
            Amount = 99.99m,
            Currency = "USD",
            PaymentMethod = PaymentMethod.CreditCard,
            PaymentGateway = PaymentGateway.Stripe
        };

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(PaymentStatus.Completed, result.Status);
        Assert.Equal("stripe_txn_success_123", result.TransactionId);
        
        // Verify gateway was called
        _mockPaymentGateway.Verify(x => x.ProcessPaymentAsync(It.IsAny<PaymentRequest>()), Times.Once);
    }

    [Fact]
    public async Task ProcessPayment_Should_Handle_Failed_PayPal_Payment()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await SeedTestUser(userId, "Test User");
        await SeedTestProduct(productId, "Test Product", userId);

        _mockPaymentGateway
            .Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentRequest>()))
            .ReturnsAsync(new PaymentResponse
            {
                IsSuccess = false,
                TransactionId = "paypal_txn_failed_456",
                Status = PaymentStatus.Failed,
                ErrorMessage = "Insufficient funds"
            });

        var command = new ProcessPaymentCommand
        {
            UserId = userId,
            ProductId = productId,
            Amount = 99.99m,
            Currency = "USD",
            PaymentMethod = PaymentMethod.PayPal,
            PaymentGateway = PaymentGateway.PayPal
        };

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(PaymentStatus.Failed, result.Status);
        Assert.Equal("paypal_txn_failed_456", result.TransactionId);
        
        // Verify gateway was called
        _mockPaymentGateway.Verify(x => x.ProcessPaymentAsync(It.IsAny<PaymentRequest>()), Times.Once);
    }

    [Fact]
    public async Task ProcessRefund_Should_Handle_Successful_Gateway_Refund()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await SeedTestUser(userId, "Test User");
        await SeedTestProduct(productId, "Test Product", userId);
        
        var payment = await CreateTestPayment(userId, productId, PaymentStatus.Completed);

        _mockPaymentGateway
            .Setup(x => x.RefundPaymentAsync(It.IsAny<RefundRequest>()))
            .ReturnsAsync(new RefundResponse
            {
                IsSuccess = true,
                RefundId = "refund_123",
                Status = PaymentStatus.Refunded
            });

        var command = new RefundPaymentCommand
        {
            PaymentId = payment.Id,
            Reason = "Customer requested refund",
            RefundAmount = 99.99m
        };

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(PaymentStatus.Refunded, result.Status);
        Assert.Equal("Customer requested refund", result.RefundReason);
        
        // Verify gateway was called
        _mockPaymentGateway.Verify(x => x.RefundPaymentAsync(It.IsAny<RefundRequest>()), Times.Once);
    }

    [Fact]
    public async Task ProcessPayment_Should_Auto_Enroll_User_On_Program_Purchase()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var programId = Guid.NewGuid();
        
        await SeedTestUser(userId, "Test User");
        await SeedTestUser(creatorId, "Creator");
        await SeedTestProduct(productId, "Test Product", creatorId);
        await SeedTestProgram(programId, "Test Program", creatorId);
        await SeedTestProductProgram(productId, programId);

        _mockPaymentGateway
            .Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentRequest>()))
            .ReturnsAsync(new PaymentResponse
            {
                IsSuccess = true,
                TransactionId = "txn_success_auto_enroll",
                Status = PaymentStatus.Completed
            });

        var command = new ProcessPaymentCommand
        {
            UserId = userId,
            ProductId = productId,
            Amount = 99.99m,
            Currency = "USD",
            PaymentMethod = PaymentMethod.CreditCard,
            PaymentGateway = PaymentGateway.Stripe
        };

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(PaymentStatus.Completed, result.Status);

        // Verify auto-enrollment
        var userProduct = await _context.UserProducts
            .FirstOrDefaultAsync(up => up.UserId == userId && up.ProductId == productId);
        
        Assert.NotNull(userProduct);
        Assert.Equal(ProductAcquisitionType.Purchase, userProduct.AcquisitionType);
        Assert.Equal(ProductAccessStatus.Active, userProduct.Status);
        Assert.True(userProduct.AccessGrantedAt.HasValue);
    }

    [Fact]
    public async Task ProcessPayment_Should_Not_Auto_Enroll_On_Failed_Payment()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var programId = Guid.NewGuid();
        
        await SeedTestUser(userId, "Test User");
        await SeedTestUser(creatorId, "Creator");
        await SeedTestProduct(productId, "Test Product", creatorId);
        await SeedTestProgram(programId, "Test Program", creatorId);
        await SeedTestProductProgram(productId, programId);

        _mockPaymentGateway
            .Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentRequest>()))
            .ReturnsAsync(new PaymentResponse
            {
                IsSuccess = false,
                TransactionId = "txn_failed_no_enroll",
                Status = PaymentStatus.Failed,
                ErrorMessage = "Payment declined"
            });

        var command = new ProcessPaymentCommand
        {
            UserId = userId,
            ProductId = productId,
            Amount = 99.99m,
            Currency = "USD",
            PaymentMethod = PaymentMethod.CreditCard,
            PaymentGateway = PaymentGateway.Stripe
        };

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(PaymentStatus.Failed, result.Status);

        // Verify no enrollment occurred
        var userProduct = await _context.UserProducts
            .FirstOrDefaultAsync(up => up.UserId == userId && up.ProductId == productId);
        
        Assert.Null(userProduct);
    }

    [Fact]
    public async Task ProcessPayment_Should_Handle_Bundle_Product_Purchase()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var bundleProductId = Guid.NewGuid();
        var program1Id = Guid.NewGuid();
        var program2Id = Guid.NewGuid();
        
        await SeedTestUser(userId, "Test User");
        await SeedTestUser(creatorId, "Creator");
        await SeedTestProduct(bundleProductId, "Bundle Product", creatorId, Common.ProductType.Bundle);
        await SeedTestProgram(program1Id, "Program 1", creatorId);
        await SeedTestProgram(program2Id, "Program 2", creatorId);
        await SeedTestProductProgram(bundleProductId, program1Id);
        await SeedTestProductProgram(bundleProductId, program2Id);

        _mockPaymentGateway
            .Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentRequest>()))
            .ReturnsAsync(new PaymentResponse
            {
                IsSuccess = true,
                TransactionId = "txn_bundle_success",
                Status = PaymentStatus.Completed
            });

        var command = new ProcessPaymentCommand
        {
            UserId = userId,
            ProductId = bundleProductId,
            Amount = 199.99m,
            Currency = "USD",
            PaymentMethod = PaymentMethod.CreditCard,
            PaymentGateway = PaymentGateway.Stripe
        };

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(PaymentStatus.Completed, result.Status);

        // Verify bundle enrollment
        var userProduct = await _context.UserProducts
            .FirstOrDefaultAsync(up => up.UserId == userId && up.ProductId == bundleProductId);
        
        Assert.NotNull(userProduct);
        Assert.Equal(ProductAcquisitionType.Purchase, userProduct.AcquisitionType);
        Assert.Equal(ProductAccessStatus.Active, userProduct.Status);
    }

    [Fact]
    public async Task ProcessPayment_Should_Handle_Duplicate_Transaction_Prevention()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await SeedTestUser(userId, "Test User");
        await SeedTestProduct(productId, "Test Product", userId);

        var transactionId = "txn_duplicate_test";
        
        // Create existing payment with same transaction ID
        await CreateTestPayment(userId, productId, PaymentStatus.Completed, transactionId: transactionId);

        var command = new ProcessPaymentCommand
        {
            UserId = userId,
            ProductId = productId,
            Amount = 99.99m,
            Currency = "USD",
            PaymentMethod = PaymentMethod.CreditCard,
            TransactionId = transactionId, // Same transaction ID
            PaymentGateway = PaymentGateway.Stripe
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _mediator.Send(command));
    }

    [Fact]
    public async Task GetPaymentStats_Should_Calculate_Revenue_Correctly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await SeedTestUser(userId, "Test User");
        await SeedTestProduct(productId, "Test Product", userId);
        
        var today = DateTime.UtcNow.Date;
        
        // Create payments for current month
        await CreateTestPayment(userId, productId, PaymentStatus.Completed, today.AddDays(-5), 100m);
        await CreateTestPayment(userId, productId, PaymentStatus.Completed, today.AddDays(-3), 200m);
        await CreateTestPayment(userId, productId, PaymentStatus.Failed, today.AddDays(-2), 150m); // Should not count
        await CreateTestPayment(userId, productId, PaymentStatus.Refunded, today.AddDays(-1), 50m); // Should not count

        var query = new GetPaymentStatsQuery
        {
            StartDate = today.AddDays(-30),
            EndDate = today.AddDays(1)
        };

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result.TotalPayments);
        Assert.Equal(2, result.CompletedPayments);
        Assert.Equal(1, result.FailedPayments);
        Assert.Equal(0, result.PendingPayments);
        Assert.Equal(1, result.RefundedPayments);
        Assert.Equal(300m, result.TotalRevenue); // Only completed payments
        Assert.Equal(150m, result.AveragePaymentAmount); // 300 / 2
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

    private async Task SeedTestProduct(Guid productId, string name, Guid creatorId, GameGuild.Common.ProductType type = GameGuild.Common.ProductType.Program)
    {
        var product = new Product
        {
            Id = productId,
            Name = name,
            Description = $"Description for {name}",
            ShortDescription = $"Short description for {name}",
            Type = type,
            Status = ContentStatus.Published,
            Visibility = AccessLevel.Public,
            CreatorId = creatorId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();
    }

    private async Task SeedTestProgram(Guid programId, string name, Guid creatorId)
    {
        var program = new Program
        {
            Id = programId,
            Name = name,
            Description = $"Description for {name}",
            Status = ContentStatus.Published,
            Visibility = AccessLevel.Public,
            CreatorId = creatorId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Programs.Add(program);
        await _context.SaveChangesAsync();
    }

    private async Task SeedTestProductProgram(Guid productId, Guid programId)
    {
        var productProgram = new ProductProgram
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            ProgramId = programId,
            CreatedAt = DateTime.UtcNow
        };

        _context.ProductPrograms.Add(productProgram);
        await _context.SaveChangesAsync();
    }

    private async Task<Payment> CreateTestPayment(
        Guid userId, 
        Guid productId, 
        PaymentStatus status, 
        DateTime? createdAt = null,
        decimal amount = 99.99m,
        string transactionId = null)
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
            PaymentGateway = PaymentGateway.Stripe,
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
        (_serviceProvider as IDisposable)?.Dispose();
    }
}
