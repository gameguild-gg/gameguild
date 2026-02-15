using FluentAssertions;
using GameGuild.Commerce.Payments;
using GameGuild.Commerce.Payments.Commands.CloseWallet;
using GameGuild.Commerce.Payments.Commands.FreezeWallet;
using GameGuild.Commerce.Payments.Commands.PatchWallet;
using GameGuild.Commerce.Payments.Commands.UnfreezeWallet;
using GameGuild.Commerce.Payments.Queries.GetWalletAuditLog;
using GameGuild.Commerce.Payments.Queries.GetWalletById;
using GameGuild.Commerce.Payments.Queries.ListWallets;
using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Payments.UnitTests;

#region StripeAmountConverter Tests

public class StripeAmountConverterTests
{
    [Theory]
    [InlineData(10.50, "USD", 1050L)]
    [InlineData(99.99, "EUR", 9999L)]
    [InlineData(0.01, "GBP", 1L)]
    [InlineData(100.00, "USD", 10000L)]
    public void ToStripeAmount_RegularCurrency_MultipliesBy100(decimal amount, string currency, long expected)
    {
        var result = StripeAmountConverter.ToStripeAmount(amount, currency);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(1000, "JPY", 1000L)]
    [InlineData(500, "KRW", 500L)]
    [InlineData(250, "VND", 250L)]
    [InlineData(100, "BIF", 100L)]
    [InlineData(100, "CLP", 100L)]
    [InlineData(100, "DJF", 100L)]
    [InlineData(100, "GNF", 100L)]
    [InlineData(100, "KMF", 100L)]
    [InlineData(100, "MGA", 100L)]
    [InlineData(100, "PYG", 100L)]
    [InlineData(100, "RWF", 100L)]
    [InlineData(100, "UGX", 100L)]
    [InlineData(100, "VUV", 100L)]
    [InlineData(100, "XAF", 100L)]
    [InlineData(100, "XOF", 100L)]
    [InlineData(100, "XPF", 100L)]
    public void ToStripeAmount_ZeroDecimalCurrency_NoMultiplication(decimal amount, string currency, long expected)
    {
        var result = StripeAmountConverter.ToStripeAmount(amount, currency);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(1050L, "USD", 10.50)]
    [InlineData(9999L, "EUR", 99.99)]
    [InlineData(1L, "GBP", 0.01)]
    public void FromStripeAmount_RegularCurrency_DividesBy100(long amount, string currency, decimal expected)
    {
        var result = StripeAmountConverter.FromStripeAmount(amount, currency);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(1000L, "JPY", 1000)]
    [InlineData(500L, "KRW", 500)]
    public void FromStripeAmount_ZeroDecimalCurrency_NoDivision(long amount, string currency, decimal expected)
    {
        var result = StripeAmountConverter.FromStripeAmount(amount, currency);
        result.Should().Be(expected);
    }

    [Fact]
    public void ToStripeAmount_CaseInsensitive()
    {
        var lower = StripeAmountConverter.ToStripeAmount(100, "jpy");
        var upper = StripeAmountConverter.ToStripeAmount(100, "JPY");
        lower.Should().Be(upper);
    }
}

#endregion

#region StripeStatusMapper Tests

public class StripeStatusMapperTests
{
    [Theory]
    [InlineData("succeeded", PaymentStatus.Succeeded)]
    [InlineData("processing", PaymentStatus.Processing)]
    [InlineData("requires_payment_method", PaymentStatus.Failed)]
    [InlineData("requires_confirmation", PaymentStatus.Pending)]
    [InlineData("requires_action", PaymentStatus.RequiresAction)]
    [InlineData("canceled", PaymentStatus.Cancelled)]
    public void MapPaymentStatus_KnownStatus_ReturnsCorrectMapping(string stripeStatus, PaymentStatus expected)
    {
        var result = StripeStatusMapper.MapPaymentStatus(stripeStatus);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("unknown_status")]
    [InlineData("")]
    [InlineData("something_else")]
    public void MapPaymentStatus_UnknownStatus_ReturnsPending(string stripeStatus)
    {
        var result = StripeStatusMapper.MapPaymentStatus(stripeStatus);
        result.Should().Be(PaymentStatus.Pending);
    }

    [Theory]
    [InlineData("duplicate", "duplicate")]
    [InlineData("fraudulent", "fraudulent")]
    [InlineData("requested_by_customer", "requested_by_customer")]
    [InlineData("customer_request", "requested_by_customer")]
    public void MapRefundReason_KnownReason_ReturnsMapping(string reason, string expected)
    {
        var result = StripeStatusMapper.MapRefundReason(reason);
        result.Should().Be(expected);
    }

    [Fact]
    public void MapRefundReason_Null_ReturnsNull()
    {
        var result = StripeStatusMapper.MapRefundReason(null);
        result.Should().BeNull();
    }

    [Fact]
    public void MapRefundReason_Empty_ReturnsNull()
    {
        var result = StripeStatusMapper.MapRefundReason("");
        result.Should().BeNull();
    }

    [Fact]
    public void MapRefundReason_UnknownReason_ReturnsNull()
    {
        var result = StripeStatusMapper.MapRefundReason("other_reason");
        result.Should().BeNull();
    }
}

#endregion

#region LedgerAccountExtensions Tests

public class LedgerAccountExtensionsTests
{
    [Theory]
    [InlineData(LedgerAccount.Cash, "1000")]
    [InlineData(LedgerAccount.AccountsReceivable, "1100")]
    [InlineData(LedgerAccount.AccountsPayable, "2000")]
    [InlineData(LedgerAccount.ProductRevenue, "4000")]
    [InlineData(LedgerAccount.PaymentProcessingFees, "5000")]
    [InlineData(LedgerAccount.SalesDiscounts, "6000")]
    public void ToAccountCode_ReturnsFormattedCode(LedgerAccount account, string expected)
    {
        account.ToAccountCode().Should().Be(expected);
    }

    [Theory]
    [InlineData(LedgerAccount.Cash, "Cash")]
    [InlineData(LedgerAccount.AccountsReceivable, "Accounts Receivable")]
    [InlineData(LedgerAccount.ProductRevenue, "Product Revenue")]
    [InlineData(LedgerAccount.PaymentProcessingFees, "Payment Processing Fees")]
    [InlineData(LedgerAccount.SalesDiscounts, "Sales Discounts")]
    [InlineData(LedgerAccount.ReturnsAndAllowances, "Returns and Allowances")]
    public void GetDescription_ReturnsDescriptionAttribute(LedgerAccount account, string expected)
    {
        account.GetDescription().Should().Be(expected);
    }

    [Theory]
    [InlineData(LedgerAccount.Cash, true)]
    [InlineData(LedgerAccount.AccountsReceivable, true)]
    [InlineData(LedgerAccount.PrepaidExpenses, true)]
    [InlineData(LedgerAccount.UserWalletDeposits, true)]
    [InlineData(LedgerAccount.PaymentGatewayPending, true)]
    [InlineData(LedgerAccount.AccountsPayable, false)]
    [InlineData(LedgerAccount.ProductRevenue, false)]
    public void IsAsset_ReturnsCorrectly(LedgerAccount account, bool expected)
    {
        account.IsAsset().Should().Be(expected);
    }

    [Theory]
    [InlineData(LedgerAccount.AccountsPayable, true)]
    [InlineData(LedgerAccount.DeferredRevenue, true)]
    [InlineData(LedgerAccount.TaxesPayable, true)]
    [InlineData(LedgerAccount.Cash, false)]
    [InlineData(LedgerAccount.ProductRevenue, false)]
    public void IsLiability_ReturnsCorrectly(LedgerAccount account, bool expected)
    {
        account.IsLiability().Should().Be(expected);
    }

    [Theory]
    [InlineData(LedgerAccount.ProductRevenue, true)]
    [InlineData(LedgerAccount.SubscriptionRevenue, true)]
    [InlineData(LedgerAccount.CourseRevenue, true)]
    [InlineData(LedgerAccount.Cash, false)]
    [InlineData(LedgerAccount.PaymentProcessingFees, false)]
    public void IsRevenue_ReturnsCorrectly(LedgerAccount account, bool expected)
    {
        account.IsRevenue().Should().Be(expected);
    }

    [Theory]
    [InlineData(LedgerAccount.PaymentProcessingFees, true)]
    [InlineData(LedgerAccount.CommissionExpense, true)]
    [InlineData(LedgerAccount.RefundsAndChargebacks, true)]
    [InlineData(LedgerAccount.BadDebtExpense, true)]
    [InlineData(LedgerAccount.Cash, false)]
    [InlineData(LedgerAccount.SalesDiscounts, false)]
    public void IsExpense_ReturnsCorrectly(LedgerAccount account, bool expected)
    {
        account.IsExpense().Should().Be(expected);
    }

    [Theory]
    [InlineData(LedgerAccount.SalesDiscounts, true)]
    [InlineData(LedgerAccount.ReturnsAndAllowances, true)]
    [InlineData(LedgerAccount.Cash, false)]
    [InlineData(LedgerAccount.PaymentProcessingFees, false)]
    public void IsContra_ReturnsCorrectly(LedgerAccount account, bool expected)
    {
        account.IsContra().Should().Be(expected);
    }
}

#endregion

#region NullPlanPricingResolver Tests

public class NullPlanPricingResolverTests
{
    private readonly NullPlanPricingResolver _resolver = new();

    [Fact]
    public async Task GetPlanMonthlyPriceAsync_ReturnsNull()
    {
        var result = await _resolver.GetPlanMonthlyPriceAsync(Guid.NewGuid());
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPlanPriceAsync_ReturnsNull()
    {
        var result = await _resolver.GetPlanPriceAsync(Guid.NewGuid(), BillingCycle.Monthly);
        result.Should().BeNull();
    }

    [Fact]
    public async Task PlanExistsAsync_ReturnsFalse()
    {
        var result = await _resolver.PlanExistsAsync(Guid.NewGuid());
        result.Should().BeFalse();
    }
}

#endregion

#region StripePaymentGateway Tests

public class StripePaymentGatewayTests
{
    private readonly Mock<IStripePaymentService> _paymentService = new();
    private readonly Mock<IStripeCustomerService> _customerService = new();
    private readonly StripePaymentGateway _gateway;

    public StripePaymentGatewayTests()
    {
        var options = Options.Create(new StripeGatewayOptions { IsEnabled = true, UseSimulation = true });
        _gateway = new StripePaymentGateway(options, _paymentService.Object, _customerService.Object);
    }

    [Fact]
    public void ProviderId_ReturnsStripe()
    {
        _gateway.ProviderId.Should().Be("stripe");
    }

    [Fact]
    public void DisplayName_ReturnsStripe()
    {
        _gateway.DisplayName.Should().Be("Stripe");
    }

    [Fact]
    public void IsEnabled_ReturnsOptionValue()
    {
        _gateway.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task ProcessPaymentAsync_DelegatesToPaymentService()
    {
        var request = new GatewayPaymentRequest("idem_1", 100m, "USD", "cust_1", "pm_1", "Test payment");
        var expected = new GatewayPaymentResult(true, "tx_1", "ext_1", null, null, PaymentStatus.Succeeded, DateTime.UtcNow);
        _paymentService.Setup(x => x.ProcessPaymentAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _gateway.ProcessPaymentAsync(request);
        result.Should().Be(expected);
        _paymentService.Verify(x => x.ProcessPaymentAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessRefundAsync_DelegatesToPaymentService()
    {
        var request = new GatewayRefundRequest("idem_1", "tx_1", null, "duplicate");
        var expected = new GatewayRefundResult(true, "ref_1", 100m, null, null, DateTime.UtcNow);
        _paymentService.Setup(x => x.ProcessRefundAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _gateway.ProcessRefundAsync(request);
        result.Should().Be(expected);
    }

    [Fact]
    public async Task ValidateWebhookSignatureAsync_DelegatesToPaymentService()
    {
        _paymentService.Setup(x => x.ValidateWebhookSignatureAsync("payload", "sig", "secret"))
            .ReturnsAsync(true);

        var result = await _gateway.ValidateWebhookSignatureAsync("payload", "sig", "secret");
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CreateCustomerAsync_DelegatesToCustomerService()
    {
        var request = new GatewayCustomerRequest("test@test.com", "Test User", null);
        var expected = new GatewayCustomerResult(true, "cus_1", null, null);
        _customerService.Setup(x => x.CreateCustomerAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _gateway.CreateCustomerAsync(request);
        result.Should().Be(expected);
    }

    [Fact]
    public async Task CreatePaymentMethodAsync_DelegatesToCustomerService()
    {
        var request = new GatewayPaymentMethodRequest("cus_1", "tok_1");
        var expected = new GatewayPaymentMethodResult(true, "pm_1", "4242", "visa", 12, 25, null, null);
        _customerService.Setup(x => x.CreatePaymentMethodAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _gateway.CreatePaymentMethodAsync(request);
        result.Should().Be(expected);
    }

    [Fact]
    public async Task CancelSubscriptionAsync_DelegatesToCustomerService()
    {
        var expected = new GatewayCancellationResult(true, null, null, DateTime.UtcNow);
        _customerService.Setup(x => x.CancelSubscriptionAsync("sub_1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _gateway.CancelSubscriptionAsync("sub_1");
        result.Should().Be(expected);
    }

    [Fact]
    public void IsEnabled_WhenDisabled_ReturnsFalse()
    {
        var options = Options.Create(new StripeGatewayOptions { IsEnabled = false });
        var gateway = new StripePaymentGateway(options, _paymentService.Object, _customerService.Object);
        gateway.IsEnabled.Should().BeFalse();
    }
}

#endregion

#region StripePaymentService Tests

public class StripePaymentServiceTests
{
    private readonly StripePaymentService _service;
    private readonly Mock<ILogger<StripePaymentService>> _logger = new();

    public StripePaymentServiceTests()
    {
        var options = Options.Create(new StripeGatewayOptions { UseSimulation = true });
        _service = new StripePaymentService(options, _logger.Object);
    }

    [Fact]
    public async Task ProcessPaymentAsync_Simulated_ReturnsSuccess()
    {
        var request = new GatewayPaymentRequest("idem_1", 50m, "USD", "cust_1", "pm_1", "Test");
        var result = await _service.ProcessPaymentAsync(request);
        result.Success.Should().BeTrue();
        result.TransactionId.Should().NotBeNullOrEmpty();
        result.Status.Should().Be(PaymentStatus.Succeeded);
    }

    [Fact]
    public async Task ProcessRefundAsync_Simulated_ReturnsSuccess()
    {
        var request = new GatewayRefundRequest("idem_2", "tx_1", 25m, "duplicate");
        var result = await _service.ProcessRefundAsync(request);
        result.Success.Should().BeTrue();
        result.RefundId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ValidateWebhookSignatureAsync_Simulated_ValidFormat_ReturnsTrue()
    {
        var result = await _service.ValidateWebhookSignatureAsync("payload", "t=123,v1=abc", "secret");
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateWebhookSignatureAsync_Simulated_EmptySignature_ReturnsFalse()
    {
        var result = await _service.ValidateWebhookSignatureAsync("payload", "", "secret");
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateWebhookSignatureAsync_Simulated_EmptySecret_ReturnsFalse()
    {
        var result = await _service.ValidateWebhookSignatureAsync("payload", "sig", "");
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateWebhookSignatureAsync_Simulated_NonEmptySignature_ReturnsTrue()
    {
        // Even without valid format, non-empty signature with non-empty secret returns true
        var result = await _service.ValidateWebhookSignatureAsync("payload", "any_sig", "secret");
        result.Should().BeTrue();
    }
}

#endregion

#region TaxCalculationService Tests

public class TaxCalculationServiceAdditionalTests
{
    private readonly Mock<IApplicationDbContext> _context = new();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private readonly TaxCalculationService _service;

    public TaxCalculationServiceAdditionalTests()
    {
        _service = new TaxCalculationService(_context.Object, NullLogger<TaxCalculationService>.Instance, _cache);
    }

    [Fact]
    public void Constructor_NullContext_Throws()
    {
        var act = () => new TaxCalculationService(null!, NullLogger<TaxCalculationService>.Instance, _cache);
        act.Should().Throw<ArgumentNullException>().WithParameterName("context");
    }

    [Fact]
    public void Constructor_NullLogger_Throws()
    {
        var act = () => new TaxCalculationService(_context.Object, null!, _cache);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_NullCache_Throws()
    {
        var act = () => new TaxCalculationService(_context.Object, NullLogger<TaxCalculationService>.Instance, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("cache");
    }

    [Theory]
    [InlineData("", "DE", false)]
    [InlineData(" ", "DE", false)]
    [InlineData(null, "DE", false)]
    [InlineData("DE123456789", "DE", true)]  // 11 chars, starts with DE
    [InlineData("DE12345", "DE", false)]     // too short (7 chars)
    [InlineData("DE1234567890123", "DE", false)] // too long (15 chars)
    [InlineData("FR123456789", "FR", true)]
    [InlineData("GB12345678", "GB", true)]   // 10 chars
    [InlineData("DE123456789", "FR", false)] // starts with DE not FR
    public async Task ValidateVatNumberAsync_VariousInputs(string? vatNumber, string countryCode, bool expected)
    {
        var result = await _service.ValidateVatNumberAsync(vatNumber!, countryCode);
        result.Should().Be(expected);
    }

    [Fact]
    public async Task ValidateVatNumberAsync_WithSpaces_StripsAndValidates()
    {
        // "DE 12345 6789" → "DE123456789" (11 chars, starts with DE)
        var result = await _service.ValidateVatNumberAsync("DE 12345 6789", "DE");
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateVatNumberAsync_LowerCase_ConvertsToUpper()
    {
        var result = await _service.ValidateVatNumberAsync("de123456789", "DE");
        result.Should().BeTrue();
    }
}

#endregion

#region PaymentsModule DI Tests

public class PaymentsModuleDiTests
{
    [Fact]
    public void AddPaymentsModule_RegistersExpectedServices()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Stripe:IsEnabled"] = "true",
                ["Stripe:UseSimulation"] = "true"
            })
            .Build();

        services.AddPaymentsModule(config);

        // Verify repositories
        services.Should().Contain(sd => sd.ServiceType == typeof(IAuditTrailRepository));
        services.Should().Contain(sd => sd.ServiceType == typeof(IFinancialLedgerRepository));
        services.Should().Contain(sd => sd.ServiceType == typeof(IPaymentRepository));
        services.Should().Contain(sd => sd.ServiceType == typeof(IRevenueEventRepository));
        services.Should().Contain(sd => sd.ServiceType == typeof(IWalletRepository));

        // Verify services
        services.Should().Contain(sd => sd.ServiceType == typeof(IDisputeService));
        services.Should().Contain(sd => sd.ServiceType == typeof(IRevenueAuditService));
        services.Should().Contain(sd => sd.ServiceType == typeof(ITaxCalculationService));
        services.Should().Contain(sd => sd.ServiceType == typeof(IWalletService));

        // Verify gateway services
        services.Should().Contain(sd => sd.ServiceType == typeof(IStripePaymentService));
        services.Should().Contain(sd => sd.ServiceType == typeof(IStripeCustomerService));
        services.Should().Contain(sd => sd.ServiceType == typeof(IPaymentGateway));
    }

    [Fact]
    public void UsePaymentsModule_ReturnsApp()
    {
        var app = new Mock<IApplicationBuilder>();
        var result = app.Object.UsePaymentsModule();
        result.Should().Be(app.Object);
    }
}

#endregion

#region Stub Handler Tests (Pattern A - No Dependencies)

public class StubHandlerTests
{
    [Fact]
    public async Task PatchTaxRuleHandler_Handle_ReturnsUnit()
    {
        var handler = new PatchTaxRuleHandler();
        var result = await handler.Handle(new PatchTaxRuleCommand(Guid.NewGuid(), 10m, null, null, null, true), CancellationToken.None);
        result.Should().Be(Unit.Value);
    }

    [Fact]
    public async Task PatchTaxJurisdictionHandler_Handle_ReturnsUnit()
    {
        var handler = new PatchTaxJurisdictionHandler();
        var result = await handler.Handle(new PatchTaxJurisdictionCommand(Guid.NewGuid(), null, null, null, null), CancellationToken.None);
        result.Should().Be(Unit.Value);
    }

    [Fact]
    public async Task DeleteTaxRuleHandler_Handle_ReturnsUnit()
    {
        var handler = new DeleteTaxRuleHandler();
        var result = await handler.Handle(new DeleteTaxRuleCommand(Guid.NewGuid()), CancellationToken.None);
        result.Should().Be(Unit.Value);
    }

    [Fact]
    public async Task DeleteTaxJurisdictionHandler_Handle_ReturnsUnit()
    {
        var handler = new DeleteTaxJurisdictionHandler();
        var result = await handler.Handle(new DeleteTaxJurisdictionCommand(Guid.NewGuid()), CancellationToken.None);
        result.Should().Be(Unit.Value);
    }

    [Fact]
    public async Task CreateTaxRuleHandler_Handle_ReturnsGuid()
    {
        var handler = new CreateTaxRuleHandler();
        var result = await handler.Handle(new CreateTaxRuleCommand("US", null, "B2C", 10m, DateTime.UtcNow, null, "Test Rule"), CancellationToken.None);
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateTaxJurisdictionHandler_Handle_ReturnsGuid()
    {
        var handler = new CreateTaxJurisdictionHandler();
        var result = await handler.Handle(new CreateTaxJurisdictionCommand("US", "United States", "US", null, "VAT", 0m), CancellationToken.None);
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetPaymentByIdQueryHandler_Handle_ReturnsNull()
    {
        var handler = new GetPaymentByIdQueryHandler();
        var result = await handler.Handle(new GetPaymentByIdQuery(Guid.NewGuid()), CancellationToken.None);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPaymentHistoryQueryHandler_Handle_ReturnsEmptyList()
    {
        var handler = new GetPaymentHistoryQueryHandler();
        var result = await handler.Handle(new GetPaymentHistoryQuery(), CancellationToken.None);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetScheduledPaymentsQueryHandler_Handle_ReturnsEmpty()
    {
        var handler = new GetScheduledPaymentsQueryHandler();
        var result = await handler.Handle(new GetScheduledPaymentsQuery(), CancellationToken.None);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRefundedPaymentsQueryHandler_Handle_ReturnsEmpty()
    {
        var handler = new GetRefundedPaymentsQueryHandler();
        var result = await handler.Handle(new GetRefundedPaymentsQuery(), CancellationToken.None);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetOverduePaymentsQueryHandler_Handle_ReturnsEmpty()
    {
        var handler = new GetOverduePaymentsQueryHandler();
        var result = await handler.Handle(new GetOverduePaymentsQuery(), CancellationToken.None);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetFailedPaymentsQueryHandler_Handle_ReturnsEmpty()
    {
        var handler = new GetFailedPaymentsQueryHandler();
        var result = await handler.Handle(new GetFailedPaymentsQuery(), CancellationToken.None);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCanceledPaymentsQueryHandler_Handle_ReturnsEmpty()
    {
        var handler = new GetCanceledPaymentsQueryHandler();
        var result = await handler.Handle(new GetCanceledPaymentsQuery(), CancellationToken.None);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllPaymentsQueryHandler_Handle_ReturnsEmpty()
    {
        var handler = new GetAllPaymentsQueryHandler();
        var result = await handler.Handle(new GetAllPaymentsQuery(), CancellationToken.None);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTaxRuleByIdHandler_Handle_ReturnsNull()
    {
        var handler = new GetTaxRuleByIdHandler();
        var result = await handler.Handle(new GetTaxRuleByIdQuery(Guid.NewGuid()), CancellationToken.None);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetTaxJurisdictionByIdHandler_Handle_ReturnsNull()
    {
        var handler = new GetTaxJurisdictionByIdHandler();
        var result = await handler.Handle(new GetTaxJurisdictionByIdQuery(Guid.NewGuid()), CancellationToken.None);
        result.Should().BeNull();
    }
}

#endregion

#region Single-Dependency Handler Constructor Tests

public class WalletServiceHandlerConstructorTests
{
    private readonly Mock<IWalletService> _walletService = new();

    [Fact]
    public void AddFundsCommandHandler_CanBeConstructed()
    {
        var handler = new AddFundsCommandHandler(_walletService.Object);
        handler.Should().NotBeNull();
    }

    [Fact]
    public void DeductFundsCommandHandler_CanBeConstructed()
    {
        var handler = new DeductFundsCommandHandler(_walletService.Object);
        handler.Should().NotBeNull();
    }

    [Fact]
    public void CreateWalletCommandHandler_CanBeConstructed()
    {
        var handler = new CreateWalletCommandHandler(_walletService.Object);
        handler.Should().NotBeNull();
    }

    [Fact]
    public void CloseWalletHandler_CanBeConstructed()
    {
        var handler = new CloseWalletHandler(_walletService.Object);
        handler.Should().NotBeNull();
    }

    [Fact]
    public void FreezeWalletHandler_CanBeConstructed()
    {
        var handler = new FreezeWalletHandler(_walletService.Object);
        handler.Should().NotBeNull();
    }

    [Fact]
    public void UnfreezeWalletHandler_CanBeConstructed()
    {
        var handler = new UnfreezeWalletHandler(_walletService.Object);
        handler.Should().NotBeNull();
    }

    [Fact]
    public void LockWalletCommandHandler_CanBeConstructed()
    {
        var handler = new LockWalletCommandHandler(_walletService.Object);
        handler.Should().NotBeNull();
    }

    [Fact]
    public void UnlockWalletCommandHandler_CanBeConstructed()
    {
        var handler = new UnlockWalletCommandHandler(_walletService.Object);
        handler.Should().NotBeNull();
    }

    [Fact]
    public void PatchWalletHandler_CanBeConstructed()
    {
        var handler = new PatchWalletHandler(_walletService.Object);
        handler.Should().NotBeNull();
    }

    [Fact]
    public void TransferFundsCommandHandler_CanBeConstructed()
    {
        var handler = new TransferFundsCommandHandler(_walletService.Object);
        handler.Should().NotBeNull();
    }

    [Fact]
    public void ListWalletsHandler_CanBeConstructed()
    {
        var handler = new ListWalletsHandler(_walletService.Object);
        handler.Should().NotBeNull();
    }

    [Fact]
    public void GetWalletByIdHandler_CanBeConstructed()
    {
        var handler = new GetWalletByIdHandler(_walletService.Object);
        handler.Should().NotBeNull();
    }

    [Fact]
    public void GetWalletByUserIdQueryHandler_CanBeConstructed()
    {
        var handler = new GetWalletByUserIdQueryHandler(_walletService.Object);
        handler.Should().NotBeNull();
    }

    [Fact]
    public void GetWalletBalanceQueryHandler_CanBeConstructed()
    {
        var handler = new GetWalletBalanceQueryHandler(_walletService.Object);
        handler.Should().NotBeNull();
    }

    [Fact]
    public void GetWalletAuditLogHandler_CanBeConstructed()
    {
        var handler = new GetWalletAuditLogHandler(_walletService.Object);
        handler.Should().NotBeNull();
    }

    [Fact]
    public void GetTransactionHistoryQueryHandler_CanBeConstructed()
    {
        var handler = new GetTransactionHistoryQueryHandler(_walletService.Object);
        handler.Should().NotBeNull();
    }
}

public class RevenueAuditServiceHandlerTests
{
    private readonly Mock<IRevenueAuditService> _revenueAuditService = new();

    [Fact]
    public void ReconcileLedgerCommandHandler_CanBeConstructed()
    {
        var handler = new ReconcileLedgerCommandHandler(_revenueAuditService.Object);
        handler.Should().NotBeNull();
    }

    [Fact]
    public void CreateLedgerEntryCommandHandler_CanBeConstructed()
    {
        var handler = new CreateLedgerEntryCommandHandler(_revenueAuditService.Object);
        handler.Should().NotBeNull();
    }

    [Fact]
    public void RecordRevenueEventCommandHandler_CanBeConstructed()
    {
        var handler = new RecordRevenueEventCommandHandler(_revenueAuditService.Object);
        handler.Should().NotBeNull();
    }

    [Fact]
    public void RecordAuditTrailCommandHandler_CanBeConstructed()
    {
        var handler = new RecordAuditTrailCommandHandler(_revenueAuditService.Object);
        handler.Should().NotBeNull();
    }

    [Fact]
    public void GetUnreconciledLedgerEntriesQueryHandler_CanBeConstructed()
    {
        var handler = new GetUnreconciledLedgerEntriesQueryHandler(_revenueAuditService.Object);
        handler.Should().NotBeNull();
    }

    [Fact]
    public void GetLedgerEntriesByAccountQueryHandler_CanBeConstructed()
    {
        var handler = new GetLedgerEntriesByAccountQueryHandler(_revenueAuditService.Object);
        handler.Should().NotBeNull();
    }

    [Fact]
    public void GetRevenueEventByIdQueryHandler_CanBeConstructed()
    {
        var handler = new GetRevenueEventByIdQueryHandler(_revenueAuditService.Object);
        handler.Should().NotBeNull();
    }

    [Fact]
    public void GetRevenueEventsByDateRangeQueryHandler_CanBeConstructed()
    {
        var handler = new GetRevenueEventsByDateRangeQueryHandler(_revenueAuditService.Object);
        handler.Should().NotBeNull();
    }

    [Fact]
    public void GetRevenueEventsByReferenceIdQueryHandler_CanBeConstructed()
    {
        var handler = new GetRevenueEventsByReferenceIdQueryHandler(_revenueAuditService.Object);
        handler.Should().NotBeNull();
    }

    [Fact]
    public void GetAuditTrailQueryHandler_CanBeConstructed()
    {
        var handler = new GetAuditTrailQueryHandler(_revenueAuditService.Object);
        handler.Should().NotBeNull();
    }
}

public class DisputeServiceHandlerTests
{
    private readonly Mock<IDisputeService> _disputeService = new();

    [Fact]
    public void CreateDisputeCommandHandler_CanBeConstructed()
    {
        var handler = new CreateDisputeCommandHandler(_disputeService.Object);
        handler.Should().NotBeNull();
    }

    [Fact]
    public void UpdateDisputeStatusCommandHandler_CanBeConstructed()
    {
        var handler = new UpdateDisputeStatusCommandHandler(_disputeService.Object);
        handler.Should().NotBeNull();
    }

    [Fact]
    public void ResolveDisputeCommandHandler_CanBeConstructed()
    {
        var handler = new ResolveDisputeCommandHandler(_disputeService.Object);
        handler.Should().NotBeNull();
    }

    [Fact]
    public void CancelDisputeCommandHandler_CanBeConstructed()
    {
        var handler = new CancelDisputeCommandHandler(_disputeService.Object);
        handler.Should().NotBeNull();
    }

    [Fact]
    public void AddDisputeEvidenceCommandHandler_CanBeConstructed()
    {
        var handler = new AddDisputeEvidenceCommandHandler(_disputeService.Object);
        handler.Should().NotBeNull();
    }

    [Fact]
    public void GetDisputeByIdQueryHandler_CanBeConstructed()
    {
        var handler = new GetDisputeByIdQueryHandler(_disputeService.Object);
        handler.Should().NotBeNull();
    }

    [Fact]
    public void GetDisputesByPaymentIdQueryHandler_CanBeConstructed()
    {
        var handler = new GetDisputesByPaymentIdQueryHandler(_disputeService.Object);
        handler.Should().NotBeNull();
    }

    [Fact]
    public void GetDisputesByUserIdQueryHandler_CanBeConstructed()
    {
        var handler = new GetDisputesByUserIdQueryHandler(_disputeService.Object);
        handler.Should().NotBeNull();
    }

    [Fact]
    public void GetDisputesByStatusQueryHandler_CanBeConstructed()
    {
        var handler = new GetDisputesByStatusQueryHandler(_disputeService.Object);
        handler.Should().NotBeNull();
    }

    [Fact]
    public void GetDisputeEvidenceQueryHandler_CanBeConstructed()
    {
        var handler = new GetDisputeEvidenceQueryHandler(_disputeService.Object);
        handler.Should().NotBeNull();
    }
}

public class TaxCalculationHandlerTests
{
    private readonly Mock<ITaxCalculationService> _taxService = new();

    [Fact]
    public void CalculateTaxCommandHandler_CanBeConstructed()
    {
        var handler = new CalculateTaxCommandHandler(_taxService.Object);
        handler.Should().NotBeNull();
    }

    [Fact]
    public void GetTaxJurisdictionsQueryHandler_CanBeConstructed()
    {
        var handler = new GetTaxJurisdictionsQueryHandler(_taxService.Object);
        handler.Should().NotBeNull();
    }

    [Fact]
    public void GetApplicableTaxRulesQueryHandler_CanBeConstructed()
    {
        var handler = new GetApplicableTaxRulesQueryHandler(_taxService.Object);
        handler.Should().NotBeNull();
    }
}

public class PaymentCommandHandlerConstructorTests
{
    [Fact]
    public void ProcessPaymentCommandHandler_CanBeConstructed()
    {
        var handler = new ProcessPaymentCommandHandler(
            new Mock<IPaymentRepository>().Object,
            new Mock<IPaymentGateway>().Object,
            NullLogger<ProcessPaymentCommandHandler>.Instance);
        handler.Should().NotBeNull();
    }

    [Fact]
    public void RetryPaymentCommandHandler_CanBeConstructed()
    {
        var handler = new RetryPaymentCommandHandler(
            new Mock<IPaymentRepository>().Object,
            new Mock<IPaymentGateway>().Object,
            NullLogger<RetryPaymentCommandHandler>.Instance);
        handler.Should().NotBeNull();
    }

    [Fact]
    public void CancelPaymentCommandHandler_CanBeConstructed()
    {
        var handler = new CancelPaymentCommandHandler(
            new Mock<IPaymentRepository>().Object,
            new Mock<IPaymentGateway>().Object,
            NullLogger<CancelPaymentCommandHandler>.Instance);
        handler.Should().NotBeNull();
    }

    [Fact]
    public void ProcessRefundCommandHandler_CanBeConstructed()
    {
        var handler = new ProcessRefundCommandHandler(
            new Mock<IPaymentRepository>().Object,
            new Mock<IPaymentGateway>().Object,
            NullLogger<ProcessRefundCommandHandler>.Instance);
        handler.Should().NotBeNull();
    }

    [Fact]
    public void UpdatePaymentStatusCommandHandler_CanBeConstructed()
    {
        var handler = new UpdatePaymentStatusCommandHandler(
            new Mock<IPaymentRepository>().Object,
            NullLogger<UpdatePaymentStatusCommandHandler>.Instance);
        handler.Should().NotBeNull();
    }
}

public class CalculatePricingQueryHandlerTests
{
    [Fact]
    public void CanBeConstructed()
    {
        var handler = new CalculatePricingQueryHandler(new Mock<IPlanPricingResolver>().Object);
        handler.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_NullResolver_Throws()
    {
        var act = () => new CalculatePricingQueryHandler(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("pricingResolver");
    }

    [Fact]
    public async Task Handle_PlanNotExists_Throws()
    {
        var resolver = new Mock<IPlanPricingResolver>();
        resolver.Setup(x => x.PlanExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = new CalculatePricingQueryHandler(resolver.Object);

        var act = () => handler.Handle(new CalculatePricingQuery(Guid.NewGuid(), null), CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not found*");
    }

    [Fact]
    public async Task Handle_NullPrice_Throws()
    {
        var resolver = new Mock<IPlanPricingResolver>();
        resolver.Setup(x => x.PlanExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        resolver.Setup(x => x.GetPlanMonthlyPriceAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Money?)null);

        var handler = new CalculatePricingQueryHandler(resolver.Object);

        var act = () => handler.Handle(new CalculatePricingQuery(Guid.NewGuid(), null), CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*pricing*");
    }

    [Fact]
    public async Task Handle_ValidPlan_ReturnsPricing()
    {
        var planId = Guid.NewGuid();
        var resolver = new Mock<IPlanPricingResolver>();
        resolver.Setup(x => x.PlanExistsAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        resolver.Setup(x => x.GetPlanMonthlyPriceAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Money(9.99m, "USD"));

        var handler = new CalculatePricingQueryHandler(resolver.Object);
        var result = await handler.Handle(new CalculatePricingQuery(planId, null), CancellationToken.None);

        result.Should().NotBeNull();
        result.BasePrice.Amount.Should().Be(9.99m);
        result.TotalPrice.Amount.Should().Be(9.99m);
    }
}

#endregion

#region Controller Constructor Tests

public class PaymentControllerConstructorTests
{
    [Fact]
    public void PaymentsController_CanBeConstructed()
    {
        var controller = new PaymentsController(
            new Mock<ISender>().Object,
            new Mock<IActorContextAccessor>().Object);
        controller.Should().NotBeNull();
    }

    [Fact]
    public void WalletsController_CanBeConstructed()
    {
        var controller = new WalletsController(new Mock<ISender>().Object);
        controller.Should().NotBeNull();
    }

    [Fact]
    public void TaxesController_CanBeConstructed()
    {
        var controller = new TaxesController(new Mock<ISender>().Object);
        controller.Should().NotBeNull();
    }

    [Fact]
    public void TaxRulesController_CanBeConstructed()
    {
        var controller = new TaxRulesController(new Mock<ISender>().Object);
        controller.Should().NotBeNull();
    }

    [Fact]
    public void TaxJurisdictionsController_CanBeConstructed()
    {
        var controller = new TaxJurisdictionsController(new Mock<ISender>().Object);
        controller.Should().NotBeNull();
    }
}

#endregion

#region Validator Tests

public class AdditionalValidatorTests
{
    [Fact]
    public void GetWalletBalanceQueryValidator_InvalidUserId_HasError()
    {
        var validator = new GetWalletBalanceQueryValidator();
        var result = validator.Validate(new GetWalletBalanceQuery(Guid.Empty));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetWalletBalanceQueryValidator_ValidUserId_NoError()
    {
        var validator = new GetWalletBalanceQueryValidator();
        var result = validator.Validate(new GetWalletBalanceQuery(Guid.NewGuid()));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void GetPaymentByIdQueryValidator_InvalidId_HasError()
    {
        var validator = new GetPaymentByIdQueryValidator();
        var result = validator.Validate(new GetPaymentByIdQuery(Guid.Empty));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetPaymentByIdQueryValidator_ValidId_NoError()
    {
        var validator = new GetPaymentByIdQueryValidator();
        var result = validator.Validate(new GetPaymentByIdQuery(Guid.NewGuid()));
        result.IsValid.Should().BeTrue();
    }
}

#endregion

#region Entity Constructor Tests

public class EntityConstructorTests
{
    [Fact]
    public void AuditTrail_PartialConstructor()
    {
        var entity = new AuditTrail(new { });
        entity.Should().NotBeNull();
    }

    [Fact]
    public void TaxRule_PartialConstructor()
    {
        var entity = new TaxRule(new { });
        entity.Should().NotBeNull();
    }

    [Fact]
    public void TaxRate_PartialConstructor()
    {
        var entity = new TaxRate(new { });
        entity.Should().NotBeNull();
    }

    [Fact]
    public void TaxJurisdiction_PartialConstructor()
    {
        var entity = new TaxJurisdiction(new { });
        entity.Should().NotBeNull();
    }

    [Fact]
    public void RevenueEvent_PartialConstructor()
    {
        var entity = new RevenueEvent(new { });
        entity.Should().NotBeNull();
    }

    [Fact]
    public void PaymentDispute_PartialConstructor()
    {
        var entity = new PaymentDispute(new { });
        entity.Should().NotBeNull();
    }

    [Fact]
    public void FinancialLedgerEntry_PartialConstructor()
    {
        var entity = new FinancialLedgerEntry(new { });
        entity.Should().NotBeNull();
    }

    [Fact]
    public void DisputeEvidence_PartialConstructor()
    {
        var entity = new DisputeEvidence(new { });
        entity.Should().NotBeNull();
    }

    [Fact]
    public void UserWallet_PartialConstructor()
    {
        var entity = new UserWallet(new { });
        entity.Should().NotBeNull();
    }

    [Fact]
    public void WalletTransaction_PartialConstructor()
    {
        var entity = new WalletTransaction(new { });
        entity.Should().NotBeNull();
    }

    [Fact]
    public void PromoStackingRule_PartialConstructor()
    {
        var entity = new PromoStackingRule(new { });
        entity.Should().NotBeNull();
    }
}

#endregion

#region DisputeService Constructor Tests

public class DisputeServiceConstructorTests
{
    [Fact]
    public void DisputeService_CanBeConstructed()
    {
        var service = new DisputeService(
            new Mock<IApplicationDbContext>().Object,
            NullLogger<DisputeService>.Instance);
        service.Should().NotBeNull();
    }
}

#endregion

#region WalletRepository Constructor Tests

public class WalletRepositoryConstructorTests
{
    [Fact]
    public void WalletRepository_CanBeConstructed()
    {
        var repository = new WalletRepository(
            new Mock<IApplicationDbContext>().Object,
            NullLogger<WalletRepository>.Instance);
        repository.Should().NotBeNull();
    }
}

#endregion

#region Record/DTO Tests

public class RecordAndDtoTests
{
    [Fact]
    public void PatchTaxJurisdictionRequest_CanBeCreated()
    {
        var request = new PatchTaxJurisdictionRequest("Updated Name", "Sales", 5.5m, true);
        request.Name.Should().Be("Updated Name");
        request.TaxType.Should().Be("Sales");
        request.DefaultRate.Should().Be(5.5m);
        request.IsActive.Should().BeTrue();
    }

    [Fact]
    public void PatchTaxJurisdictionRequest_DefaultValues()
    {
        var request = new PatchTaxJurisdictionRequest();
        request.Name.Should().BeNull();
        request.TaxType.Should().BeNull();
        request.DefaultRate.Should().BeNull();
        request.IsActive.Should().BeNull();
    }

    [Fact]
    public void TaxJurisdictionDto_CanBeCreated()
    {
        var dto = new TaxJurisdictionDto(Guid.NewGuid(), "US", "United States", "US", null, "VAT", 10m, true);
        dto.Code.Should().Be("US");
        dto.Country.Should().Be("US");
        dto.State.Should().BeNull();
    }

    [Fact]
    public void CreateTaxJurisdictionRequest_CanBeCreated()
    {
        var request = new CreateTaxJurisdictionRequest("US-CA", "California", "US", "CA", "Sales", 7.25m);
        request.Code.Should().Be("US-CA");
        request.State.Should().Be("CA");
    }

    [Fact]
    public void StripeGatewayOptions_DefaultValues()
    {
        var options = new StripeGatewayOptions();
        options.IsEnabled.Should().BeTrue();
        options.UseSimulation.Should().BeTrue();
    }
}

#endregion
