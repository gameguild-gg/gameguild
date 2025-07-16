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
/// Comprehensive tests for Payment Command handlers
/// </summary>
public class PaymentCommandTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IServiceProvider _serviceProvider;
    private readonly IMediator _mediator;
    private readonly Mock<IUserContext> _mockUserContext;
    private readonly Mock<ITenantContext> _mockTenantContext;

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
        services.AddMediatR(typeof(PaymentCommandHandlers).Assembly);
        
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
    public async Task ProcessPaymentCommand_Should_Create_Payment_Successfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await SeedTestUser(userId, "Test User");
        await SeedTestProduct(productId, "Test Product", userId);

        var command = new ProcessPaymentCommand
        {
            UserId = userId,
            ProductId = productId,
            Amount = 99.99m,
            Currency = "USD",
            PaymentMethod = PaymentMethod.CreditCard,
            TransactionId = "txn_success_123",
            PaymentGateway = PaymentGateway.Stripe
        };

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(PaymentStatus.Completed, result.Status);
        Assert.Equal(99.99m, result.Amount);
        Assert.Equal("USD", result.Currency);
        Assert.Equal(userId, result.UserId);
        Assert.Equal(productId, result.ProductId);

        // Verify payment is saved in database
        var payment = await _context.Payments.FindAsync(result.Id);
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

        var command = new ProcessPaymentCommand
        {
            UserId = userId,
            ProductId = productId,
            Amount = 99.99m,
            Currency = "USD",
            PaymentMethod = PaymentMethod.CreditCard,
            TransactionId = "txn_failed_456", // Transaction ID indicates failure
            PaymentGateway = PaymentGateway.Stripe
        };

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(PaymentStatus.Failed, result.Status);
        
        // Verify payment is saved in database
        var payment = await _context.Payments.FindAsync(result.Id);
        Assert.NotNull(payment);
        Assert.Equal(PaymentStatus.Failed, payment.Status);
    }

    [Fact]
    public async Task ProcessPaymentCommand_Should_Auto_Enroll_User_On_Successful_Payment()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var programId = Guid.NewGuid();
        
        await SeedTestUser(userId, "Test User");
        await SeedTestUser(creatorId, "Test Creator");
        await SeedTestProduct(productId, "Test Product", creatorId);
        await SeedTestProgram(programId, "Test Program", creatorId);
        await SeedTestProductProgram(productId, programId);

        var command = new ProcessPaymentCommand
        {
            UserId = userId,
            ProductId = productId,
            Amount = 99.99m,
            Currency = "USD",
            PaymentMethod = PaymentMethod.CreditCard,
            TransactionId = "txn_success_123",
            PaymentGateway = PaymentGateway.Stripe
        };

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(PaymentStatus.Completed, result.Status);

        // Verify auto-enrollment occurred
        var userProduct = await _context.UserProducts
            .FirstOrDefaultAsync(up => up.UserId == userId && up.ProductId == productId);
        
        Assert.NotNull(userProduct);
        Assert.Equal(ProductAcquisitionType.Purchase, userProduct.AcquisitionType);
        Assert.Equal(ProductAccessStatus.Active, userProduct.Status);
    }

    [Fact]
    public async Task ProcessPaymentCommand_Should_Fail_For_Nonexistent_User()
    {
        // Arrange
        var productId = Guid.NewGuid();
        await SeedTestProduct(productId, "Test Product", Guid.NewGuid());

        var command = new ProcessPaymentCommand
        {
            UserId = Guid.NewGuid(), // Non-existent user
            ProductId = productId,
            Amount = 99.99m,
            Currency = "USD",
            PaymentMethod = PaymentMethod.CreditCard,
            TransactionId = "txn_success_123",
            PaymentGateway = PaymentGateway.Stripe
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _mediator.Send(command));
    }

    [Fact]
    public async Task ProcessPaymentCommand_Should_Fail_For_Nonexistent_Product()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SeedTestUser(userId, "Test User");

        var command = new ProcessPaymentCommand
        {
            UserId = userId,
            ProductId = Guid.NewGuid(), // Non-existent product
            Amount = 99.99m,
            Currency = "USD",
            PaymentMethod = PaymentMethod.CreditCard,
            TransactionId = "txn_success_123",
            PaymentGateway = PaymentGateway.Stripe
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _mediator.Send(command));
    }

    [Fact]
    public async Task RefundPaymentCommand_Should_Process_Refund_Successfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await SeedTestUser(userId, "Test User");
        await SeedTestProduct(productId, "Test Product", userId);
        
        var payment = await CreateTestPayment(userId, productId, PaymentStatus.Completed);

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
        Assert.Equal(99.99m, result.RefundAmount);
        Assert.Equal("Customer requested refund", result.RefundReason);

        // Verify payment status is updated in database
        var updatedPayment = await _context.Payments.FindAsync(payment.Id);
        Assert.NotNull(updatedPayment);
        Assert.Equal(PaymentStatus.Refunded, updatedPayment.Status);
    }

    [Fact]
    public async Task RefundPaymentCommand_Should_Fail_For_Already_Refunded_Payment()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await SeedTestUser(userId, "Test User");
        await SeedTestProduct(productId, "Test Product", userId);
        
        var payment = await CreateTestPayment(userId, productId, PaymentStatus.Refunded);

        var command = new RefundPaymentCommand
        {
            PaymentId = payment.Id,
            Reason = "Customer requested refund",
            RefundAmount = 99.99m
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _mediator.Send(command));
    }

    [Fact]
    public async Task CancelPaymentCommand_Should_Cancel_Pending_Payment()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await SeedTestUser(userId, "Test User");
        await SeedTestProduct(productId, "Test Product", userId);
        
        var payment = await CreateTestPayment(userId, productId, PaymentStatus.Pending);

        var command = new CancelPaymentCommand
        {
            PaymentId = payment.Id,
            Reason = "Payment timeout"
        };

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(PaymentStatus.Cancelled, result.Status);

        // Verify payment status is updated in database
        var updatedPayment = await _context.Payments.FindAsync(payment.Id);
        Assert.NotNull(updatedPayment);
        Assert.Equal(PaymentStatus.Cancelled, updatedPayment.Status);
    }

    [Fact]
    public async Task CancelPaymentCommand_Should_Fail_For_Completed_Payment()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await SeedTestUser(userId, "Test User");
        await SeedTestProduct(productId, "Test Product", userId);
        
        var payment = await CreateTestPayment(userId, productId, PaymentStatus.Completed);

        var command = new CancelPaymentCommand
        {
            PaymentId = payment.Id,
            Reason = "Customer changed mind"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _mediator.Send(command));
    }

    [Fact]
    public async Task CreatePaymentIntentCommand_Should_Create_Payment_Intent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await SeedTestUser(userId, "Test User");
        await SeedTestProduct(productId, "Test Product", userId);

        var command = new CreatePaymentIntentCommand
        {
            UserId = userId,
            ProductId = productId,
            Amount = 99.99m,
            Currency = "USD",
            PaymentMethod = PaymentMethod.CreditCard
        };

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(PaymentStatus.Pending, result.Status);
        Assert.Equal(99.99m, result.Amount);
        Assert.Equal("USD", result.Currency);
        Assert.Equal(userId, result.UserId);
        Assert.Equal(productId, result.ProductId);

        // Verify payment intent is saved in database
        var payment = await _context.Payments.FindAsync(result.Id);
        Assert.NotNull(payment);
        Assert.Equal(PaymentStatus.Pending, payment.Status);
    }

    [Fact]
    public async Task UpdatePaymentStatusCommand_Should_Update_Payment_Status()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await SeedTestUser(userId, "Test User");
        await SeedTestProduct(productId, "Test Product", userId);
        
        var payment = await CreateTestPayment(userId, productId, PaymentStatus.Pending);

        var command = new UpdatePaymentStatusCommand
        {
            PaymentId = payment.Id,
            Status = PaymentStatus.Completed,
            TransactionId = "txn_success_789"
        };

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(PaymentStatus.Completed, result.Status);
        Assert.Equal("txn_success_789", result.TransactionId);

        // Verify payment status is updated in database
        var updatedPayment = await _context.Payments.FindAsync(payment.Id);
        Assert.NotNull(updatedPayment);
        Assert.Equal(PaymentStatus.Completed, updatedPayment.Status);
        Assert.Equal("txn_success_789", updatedPayment.TransactionId);
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
            PaymentMethod = PaymentMethod.CreditCard,
            PaymentGateway = PaymentGateway.Stripe,
            TransactionId = "txn_test_" + Guid.NewGuid().ToString("N")[..8],
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
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
