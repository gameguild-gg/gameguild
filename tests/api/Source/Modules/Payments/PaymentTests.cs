using GameGuild.Common;
using GameGuild.Database;
using GameGuild.Modules.Payments;
using GameGuild.Modules.Payments.Services;
using GameGuild.Modules.Payments.Models;
using GameGuild.Modules.Products;
using GameGuild.Modules.Products.Services;
using GameGuild.Modules.Programs;
using GameGuild.Modules.Programs.Services;
using GameGuild.Modules.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GameGuild.Tests.Modules.Payments;

/// <summary>
/// Comprehensive tests for Payment processing and integration with Products and Program enrollment
/// </summary>
public class PaymentTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IServiceProvider _serviceProvider;
    private readonly IPaymentService _paymentService;
    private readonly IProductService _productService;
    private readonly IProgramEnrollmentService _enrollmentService;

    public PaymentTests()
    {
        var services = new ServiceCollection();
        
        // Add database context
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));
        
        // Add logging
        services.AddLogging(builder => builder.AddConsole());
        
        // Add services
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IProgramEnrollmentService, ProgramEnrollmentService>();
        
        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<ApplicationDbContext>();
        _paymentService = _serviceProvider.GetRequiredService<IPaymentService>();
        _productService = _serviceProvider.GetRequiredService<IProductService>();
        _enrollmentService = _serviceProvider.GetRequiredService<IProgramEnrollmentService>();
    }

    [Fact]
    public async Task Payment_Entity_Can_Be_Created_And_Saved()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await SeedTestData(userId, productId);

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ProductId = productId,
            Amount = 99.99m,
            Currency = "USD",
            Status = PaymentStatus.Pending,
            PaymentMethod = PaymentMethod.CreditCard,
            TransactionId = "test_txn_123",
            PaymentDate = DateTime.UtcNow,
            ProcessorReference = "stripe_ref_456"
        };

        // Act
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        // Assert
        var savedPayment = await _context.Payments.FindAsync(payment.Id);
        Assert.NotNull(savedPayment);
        Assert.Equal(userId, savedPayment.UserId);
        Assert.Equal(productId, savedPayment.ProductId);
        Assert.Equal(99.99m, savedPayment.Amount);
        Assert.Equal("USD", savedPayment.Currency);
        Assert.Equal(PaymentStatus.Pending, savedPayment.Status);
        Assert.Equal(PaymentMethod.CreditCard, savedPayment.PaymentMethod);
    }

    [Fact]
    public async Task PaymentService_Can_Create_Payment_Intent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await SeedTestData(userId, productId);

        var paymentRequest = new PaymentRequest
        {
            UserId = userId,
            ProductId = productId,
            Amount = 149.99m,
            Currency = "USD",
            PaymentMethod = PaymentMethod.CreditCard
        };

        // Act
        var payment = await _paymentService.CreatePaymentIntentAsync(paymentRequest);

        // Assert
        Assert.NotNull(payment);
        Assert.Equal(userId, payment.UserId);
        Assert.Equal(productId, payment.ProductId);
        Assert.Equal(149.99m, payment.Amount);
        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.NotNull(payment.TransactionId);
        Assert.True(payment.PaymentDate <= DateTime.UtcNow);
    }

    [Fact]
    public async Task PaymentService_Can_Process_Successful_Payment()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var programId = Guid.NewGuid();
        
        await SeedTestData(userId, productId, programId);
        await LinkProductToProgram(productId, programId);

        var paymentRequest = new PaymentRequest
        {
            UserId = userId,
            ProductId = productId,
            Amount = 99.99m,
            Currency = "USD",
            PaymentMethod = PaymentMethod.CreditCard
        };

        var payment = await _paymentService.CreatePaymentIntentAsync(paymentRequest);

        // Act
        var processedPayment = await _paymentService.ProcessPaymentAsync(
            payment.Id, 
            "successful_charge_123", 
            PaymentStatus.Completed
        );

        // Assert
        Assert.NotNull(processedPayment);
        Assert.Equal(PaymentStatus.Completed, processedPayment.Status);
        Assert.Equal("successful_charge_123", processedPayment.ProcessorReference);
        Assert.True(processedPayment.CompletedDate.HasValue);

        // Verify auto-enrollment occurred
        var enrollments = await _enrollmentService.GetUserEnrollmentsAsync(userId);
        var enrollmentList = enrollments.ToList();
        Assert.Single(enrollmentList);
        Assert.Equal(programId, enrollmentList[0].ProgramId);
        Assert.Equal(EnrollmentSource.ProductPurchase, enrollmentList[0].Source);
    }

    [Fact]
    public async Task PaymentService_Can_Handle_Failed_Payment()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await SeedTestData(userId, productId);

        var paymentRequest = new PaymentRequest
        {
            UserId = userId,
            ProductId = productId,
            Amount = 199.99m,
            Currency = "USD",
            PaymentMethod = PaymentMethod.CreditCard
        };

        var payment = await _paymentService.CreatePaymentIntentAsync(paymentRequest);

        // Act
        var failedPayment = await _paymentService.ProcessPaymentAsync(
            payment.Id, 
            "failed_charge_456", 
            PaymentStatus.Failed,
            "Insufficient funds"
        );

        // Assert
        Assert.NotNull(failedPayment);
        Assert.Equal(PaymentStatus.Failed, failedPayment.Status);
        Assert.Equal("Insufficient funds", failedPayment.FailureReason);
        Assert.False(failedPayment.CompletedDate.HasValue);

        // Verify no enrollment occurred
        var enrollments = await _enrollmentService.GetUserEnrollmentsAsync(userId);
        Assert.Empty(enrollments);
    }

    [Fact]
    public async Task PaymentService_Can_Process_Refund()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await SeedTestData(userId, productId);

        // Create and complete a payment first
        var payment = await CreateCompletedPayment(userId, productId, 79.99m);

        // Act
        var refundedPayment = await _paymentService.ProcessRefundAsync(
            payment.Id, 
            79.99m, 
            "Customer requested refund"
        );

        // Assert
        Assert.NotNull(refundedPayment);
        Assert.Equal(PaymentStatus.Refunded, refundedPayment.Status);
        Assert.Equal(79.99m, refundedPayment.RefundAmount);
        Assert.Equal("Customer requested refund", refundedPayment.RefundReason);
        Assert.True(refundedPayment.RefundDate.HasValue);
    }

    [Fact]
    public async Task PaymentService_Can_Get_User_Payment_History()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId1 = Guid.NewGuid();
        var productId2 = Guid.NewGuid();
        
        await SeedTestData(userId, productId1);
        await SeedTestProduct(productId2, "Product 2", 129.99m);

        // Create multiple payments
        await CreateCompletedPayment(userId, productId1, 99.99m);
        await CreateCompletedPayment(userId, productId2, 129.99m);

        // Act
        var paymentHistory = await _paymentService.GetUserPaymentHistoryAsync(userId);

        // Assert
        Assert.NotNull(paymentHistory);
        var payments = paymentHistory.ToList();
        Assert.Equal(2, payments.Count);
        Assert.All(payments, p => Assert.Equal(userId, p.UserId));
        Assert.All(payments, p => Assert.Equal(PaymentStatus.Completed, p.Status));
    }

    [Fact]
    public async Task PaymentService_Can_Calculate_Total_Revenue()
    {
        // Arrange
        var productId = Guid.NewGuid();
        await SeedTestProduct(productId, "Revenue Test Product", 50.00m);

        // Create multiple successful payments
        for (int i = 0; i < 5; i++)
        {
            var userId = Guid.NewGuid();
            await SeedTestUser(userId, $"User {i}");
            await CreateCompletedPayment(userId, productId, 50.00m);
        }

        // Act
        var totalRevenue = await _paymentService.CalculateTotalRevenueAsync(
            DateTime.UtcNow.AddDays(-1), 
            DateTime.UtcNow.AddDays(1)
        );

        // Assert
        Assert.Equal(250.00m, totalRevenue); // 5 payments × $50.00
    }

    [Fact]
    public async Task PaymentService_Can_Get_Product_Revenue()
    {
        // Arrange
        var productId = Guid.NewGuid();
        await SeedTestProduct(productId, "Product Revenue Test", 75.00m);

        // Create payments with different amounts (discounts, sales, etc.)
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var userId3 = Guid.NewGuid();
        
        await SeedTestUser(userId1, "User 1");
        await SeedTestUser(userId2, "User 2");
        await SeedTestUser(userId3, "User 3");

        await CreateCompletedPayment(userId1, productId, 75.00m); // Full price
        await CreateCompletedPayment(userId2, productId, 60.00m); // Discounted
        await CreateCompletedPayment(userId3, productId, 75.00m); // Full price

        // Act
        var productRevenue = await _paymentService.GetProductRevenueAsync(productId);

        // Assert
        Assert.Equal(210.00m, productRevenue); // $75 + $60 + $75
    }

    [Fact]
    public async Task PaymentService_Can_Handle_Subscription_Payment()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await SeedTestData(userId, productId);

        var subscriptionRequest = new SubscriptionPaymentRequest
        {
            UserId = userId,
            ProductId = productId,
            Amount = 29.99m,
            Currency = "USD",
            PaymentMethod = PaymentMethod.CreditCard,
            BillingCycle = BillingCycle.Monthly,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(1)
        };

        // Act
        var subscription = await _paymentService.CreateSubscriptionAsync(subscriptionRequest);

        // Assert
        Assert.NotNull(subscription);
        Assert.Equal(userId, subscription.UserId);
        Assert.Equal(productId, subscription.ProductId);
        Assert.Equal(29.99m, subscription.Amount);
        Assert.Equal(BillingCycle.Monthly, subscription.BillingCycle);
        Assert.Equal(SubscriptionStatus.Active, subscription.Status);
    }

    [Fact]
    public async Task PaymentService_Can_Validate_Payment_Amount()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await SeedTestData(userId, productId, price: 99.99m);

        // Act & Assert - Valid amount
        var isValid = await _paymentService.ValidatePaymentAmountAsync(productId, 99.99m);
        Assert.True(isValid);

        // Act & Assert - Invalid amount
        var isInvalid = await _paymentService.ValidatePaymentAmountAsync(productId, 79.99m);
        Assert.False(isInvalid);
    }

    [Fact]
    public async Task PaymentService_Can_Apply_Discount_Code()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await SeedTestData(userId, productId, price: 100.00m);

        var discountCode = await CreateTestDiscountCode("SAVE20", 20.00m, DiscountType.FixedAmount);

        var paymentRequest = new PaymentRequest
        {
            UserId = userId,
            ProductId = productId,
            Amount = 100.00m,
            Currency = "USD",
            PaymentMethod = PaymentMethod.CreditCard,
            DiscountCode = "SAVE20"
        };

        // Act
        var payment = await _paymentService.CreatePaymentIntentAsync(paymentRequest);

        // Assert
        Assert.NotNull(payment);
        Assert.Equal(80.00m, payment.Amount); // $100 - $20 discount
        Assert.Equal(20.00m, payment.DiscountAmount);
        Assert.Equal("SAVE20", payment.DiscountCode);
    }

    #region Helper Methods

    private async Task SeedTestData(Guid userId, Guid productId, Guid? programId = null, decimal price = 99.99m)
    {
        await SeedTestUser(userId, "Test User");
        await SeedTestProduct(productId, "Test Product", price);
        
        if (programId.HasValue)
        {
            await SeedTestProgram(programId.Value, "Test Program");
        }
    }

    private async Task SeedTestUser(Guid userId, string name)
    {
        var user = new User
        {
            Id = userId,
            Name = name,
            Email = $"test_{userId:N}@example.com",
            IsActive = true
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
            Description = $"Description for {name}",
            Price = price,
            Currency = "USD",
            IsActive = true,
            ProductType = ProductType.Course
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();
    }

    private async Task SeedTestProgram(Guid programId, string title)
    {
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
    }

    private async Task LinkProductToProgram(Guid productId, Guid programId)
    {
        var productProgram = new ProductProgram
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            ProgramId = programId,
            AccessLevel = ProgramAccessLevel.Full,
            IncludeInBundle = true,
            SortOrder = 1
        };

        _context.ProductPrograms.Add(productProgram);
        await _context.SaveChangesAsync();
    }

    private async Task<Payment> CreateCompletedPayment(Guid userId, Guid productId, decimal amount)
    {
        var paymentRequest = new PaymentRequest
        {
            UserId = userId,
            ProductId = productId,
            Amount = amount,
            Currency = "USD",
            PaymentMethod = PaymentMethod.CreditCard
        };

        var payment = await _paymentService.CreatePaymentIntentAsync(paymentRequest);
        return await _paymentService.ProcessPaymentAsync(
            payment.Id, 
            $"completed_charge_{Guid.NewGuid():N}", 
            PaymentStatus.Completed
        );
    }

    private async Task<DiscountCode> CreateTestDiscountCode(string code, decimal value, DiscountType type)
    {
        var discountCode = new DiscountCode
        {
            Id = Guid.NewGuid(),
            Code = code,
            DiscountType = type,
            Value = value,
            IsActive = true,
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            ValidUntil = DateTime.UtcNow.AddDays(30),
            UsageLimit = 100,
            CurrentUsage = 0
        };

        _context.DiscountCodes.Add(discountCode);
        await _context.SaveChangesAsync();
        return discountCode;
    }

    #endregion

    public void Dispose()
    {
        _context.Dispose();
        if (_serviceProvider is IDisposable disposable)
            disposable.Dispose();
    }
}
