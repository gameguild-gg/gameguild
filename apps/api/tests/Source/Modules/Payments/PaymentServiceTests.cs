using GameGuild.Common;
using GameGuild.Database;
using GameGuild.Modules.Payments;
using GameGuild.Modules.Products;
using GameGuild.Modules.Users;
using GameGuild.Modules.Contents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MediatR;
using Moq;

namespace GameGuild.Tests.Modules.Payments;

/// <summary>
/// Comprehensive integration tests for Payment Service operations
/// </summary>
public class PaymentServiceTests : IDisposable {
  private readonly ApplicationDbContext _context;
  private readonly IServiceProvider _serviceProvider;
  private readonly IMediator _mediator;
  private readonly Mock<IUserContext> _mockUserContext;
  private readonly Mock<ITenantContext> _mockTenantContext;
  private readonly Mock<IPaymentGatewayService> _mockPaymentGateway;

  public PaymentServiceTests() {
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

    // Add MediatR with correct handler types
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
  public async Task Payment_End_To_End_Flow_Should_Work_Successfully() {
    // Arrange
    var userId = Guid.NewGuid();
    var productId = Guid.NewGuid();
    var creatorId = Guid.NewGuid();
    var programId = Guid.NewGuid();

    await SeedTestUser(userId, "Test User");
    await SeedTestUser(creatorId, "Test Creator");
    await SeedTestProduct(productId, "Test Product", creatorId);
    await SeedTestProgram(programId, "Test Program", creatorId);
    await SeedTestProductProgram(productId, programId);

    // Setup user context for this specific user
    _mockUserContext.Setup(x => x.UserId).Returns(userId);

    // Setup successful payment gateway
    _mockPaymentGateway.Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentRequest>()))
        .ReturnsAsync(new PaymentResult {
          Success = true,
          TransactionId = "txn_success_123",
          PaymentIntentId = "pi_success_123",
          ProcessedAt = DateTime.UtcNow,
        });

    // Step 1: Create Payment Intent
    var createCommand = new CreatePaymentCommand {
      UserId = userId,
      ProductId = productId,
      Amount = 99.99m,
      Currency = "USD",
      Method = PaymentMethod.CreditCard,
      Description = "Test Product Purchase",
    };

    var createResult = await _mediator.Send(createCommand);

    // Assert create result
    Assert.True(createResult.Success);
    Assert.NotNull(createResult.Payment);
    Assert.Equal(PaymentStatus.Pending, createResult.Payment.Status);

    // Step 2: Process Payment
    var processCommand = new ProcessPaymentCommand {
      PaymentId = createResult.Payment.Id,
      ProviderTransactionId = "txn_success_123",
    };

    var processResult = await _mediator.Send(processCommand);

    // Assert process result
    Assert.True(processResult.Success);
    Assert.NotNull(processResult.Payment);
    Assert.Equal(PaymentStatus.Completed, processResult.Payment.Status);
    Assert.True(processResult.AutoEnrollTriggered);

    // Verify user was enrolled in the program (since payment handler auto-enrolls users in programs, not direct product access)
    var programEnrollment = await _context.ProgramUsers
        .FirstOrDefaultAsync(pu => pu.UserId == userId && pu.ProgramId == programId && pu.IsActive);
    Assert.NotNull(programEnrollment);
    Assert.True(programEnrollment.IsActive);
  }

  [Fact]
  public async Task Payment_Failure_Should_Not_Grant_Access() {
    // Arrange
    var userId = Guid.NewGuid();
    var productId = Guid.NewGuid();
    var creatorId = Guid.NewGuid();

    await SeedTestUser(userId, "Test User");
    await SeedTestUser(creatorId, "Test Creator");
    await SeedTestProduct(productId, "Test Product", creatorId);

    // Setup user context for this specific user
    _mockUserContext.Setup(x => x.UserId).Returns(userId);

    // Setup failed payment gateway
    _mockPaymentGateway.Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentRequest>()))
        .ReturnsAsync(new PaymentResult {
          Success = false,
          Error = "Insufficient funds",
          ProcessedAt = DateTime.UtcNow,
        });

    // Create and process payment
    var createCommand = new CreatePaymentCommand {
      UserId = userId,
      ProductId = productId,
      Amount = 99.99m,
      Currency = "USD",
      Method = PaymentMethod.CreditCard,
    };

    var createResult = await _mediator.Send(createCommand);

    var processCommand = new ProcessPaymentCommand {
      PaymentId = createResult.Payment!.Id,
      ProviderTransactionId = "pi_failed_123", // Must start with "pi_failed" to be marked as failed
    };

    var processResult = await _mediator.Send(processCommand);

    // Assert - processing was successful but payment failed
    Assert.True(processResult.Success); // Processing operation succeeded
    Assert.NotNull(processResult.Payment);
    Assert.Equal(PaymentStatus.Failed, processResult.Payment.Status); // But payment itself failed
    Assert.False(processResult.AutoEnrollTriggered);

    // Verify user does NOT have access to the product
    var userProduct = await _context.UserProducts
        .FirstOrDefaultAsync(up => up.UserId == userId && up.ProductId == productId);
    Assert.Null(userProduct);
  }

  [Fact]
  public async Task Refund_Should_Process_Successfully() {
    // Arrange
    var userId = Guid.NewGuid();
    var productId = Guid.NewGuid();
    var creatorId = Guid.NewGuid();

    await SeedTestUser(userId, "Test User");
    await SeedTestUser(creatorId, "Test Creator");
    await SeedTestProduct(productId, "Test Product", creatorId);

    // Setup user context for this specific user
    _mockUserContext.Setup(x => x.UserId).Returns(userId);

    // Create a completed payment
    var payment = await CreateTestPayment(userId, productId, PaymentStatus.Completed);

    // Setup refund gateway mock
    _mockPaymentGateway.Setup(x => x.RefundPaymentAsync(It.IsAny<RefundRequest>()))
        .ReturnsAsync(new RefundResult {
          Success = true,
          RefundId = "refund_123",
          ProcessedAt = DateTime.UtcNow,
        });

    var refundCommand = new RefundPaymentCommand {
      PaymentId = payment.Id,
      RefundAmount = 50.00m,
      Reason = "Customer request",
      RefundedBy = userId,
    };

    // Act
    var result = await _mediator.Send(refundCommand);

    // Assert
    Assert.True(result.Success);
    Assert.NotNull(result.Refund);
    Assert.Equal(50.00m, result.Refund.RefundAmount);
    Assert.Equal("Customer request", result.Refund.Reason);

    // Verify refund is saved
    var savedRefund = await _context.PaymentRefunds.FindAsync(result.Refund.Id);
    Assert.NotNull(savedRefund);
  }

  [Fact]
  public async Task GetPaymentStats_Should_Return_Correct_Statistics() {
    // Arrange
    var userId = Guid.NewGuid();
    var productId = Guid.NewGuid();
    var creatorId = Guid.NewGuid();

    await SeedTestUser(userId, "Test User");
    await SeedTestUser(creatorId, "Test Creator");
    await SeedTestProduct(productId, "Test Product", creatorId);

    // Setup user context for this specific user
    _mockUserContext.Setup(x => x.UserId).Returns(userId);

    // Create test payments
    await CreateTestPayment(userId, productId, PaymentStatus.Completed);
    await CreateTestPayment(userId, productId, PaymentStatus.Failed);
    await CreateTestPayment(userId, productId, PaymentStatus.Pending);

    var query = new GetPaymentStatsQuery {
      UserId = userId,
      FromDate = DateTime.UtcNow.AddDays(-30),
      ToDate = DateTime.UtcNow,
    };

    // Act
    var stats = await _mediator.Send(query);

    // Assert
    Assert.NotNull(stats);
    Assert.Equal(3, stats.TotalPayments);
    Assert.Equal(1, stats.SuccessfulPayments);
    Assert.Equal(1, stats.FailedPayments);
    Assert.Equal(99.99m, stats.TotalRevenue);
  }

  private async Task SeedTestUser(Guid userId, string userName) {
    var user = new User {
      Id = userId,
      Name = userName,
      Email = $"{userName.ToLowerInvariant().Replace(" ", "")}@test.com",
      CreatedAt = DateTime.UtcNow,
    };

    _context.Users.Add(user);
    await _context.SaveChangesAsync();
  }

  private async Task SeedTestProduct(Guid productId, string productName, Guid creatorId) {
    var product = new Product {
      Id = productId,
      Name = productName,
      Description = $"{productName} description",
      Title = productName,
      CreatorId = creatorId,
      Status = ContentStatus.Published,
      Visibility = AccessLevel.Public,
      CreatedAt = DateTime.UtcNow,
    };

    _context.Products.Add(product);
    await _context.SaveChangesAsync();
  }

  private async Task SeedTestProgram(Guid programId, string title, Guid creatorId) {
    var program = new GameGuild.Modules.Programs.Program {
      Id = programId,
      Title = title,
      Description = $"{title} description",
      Status = ContentStatus.Published,
      Visibility = AccessLevel.Public,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
    };

    _context.Programs.Add(program);
    await _context.SaveChangesAsync();
  }

  private async Task SeedTestProductProgram(Guid productId, Guid programId) {
    var productProgram = new ProductProgram {
      ProductId = productId,
      ProgramId = programId,
    };

    _context.ProductPrograms.Add(productProgram);
    await _context.SaveChangesAsync();
  }

  private async Task<Payment> CreateTestPayment(Guid userId, Guid productId, PaymentStatus status) {
    var payment = new Payment {
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
      NetAmount = 95.99m,
    };

    if (status == PaymentStatus.Completed) {
      payment.ProcessedAt = DateTime.UtcNow;
      payment.ProviderTransactionId = "test_txn_123";
    }

    _context.Payments.Add(payment);
    await _context.SaveChangesAsync();
    return payment;
  }

  public void Dispose() {
    _context?.Dispose();
    _serviceProvider?.GetService<IServiceScope>()?.Dispose();
  }
}
