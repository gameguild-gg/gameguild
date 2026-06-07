using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using GameGuild.Commerce.Payments;
using GameGuild.Commerce.Payments.Commands.PatchWallet;
using GameGuild.Commerce.Payments.Queries.ListWallets;
using Xunit;

namespace GameGuild.Commerce.Payments.UnitTests;

#region Validators

public class AddDisputeEvidenceCommandValidatorTests
{
    private readonly AddDisputeEvidenceCommandValidator _validator = new();

    private static AddDisputeEvidenceCommand ValidCommand() => new(
        DisputeId: Guid.NewGuid(),
        EvidenceType: EvidenceType.Documentation,
        Title: "Evidence Title",
        Description: "Detailed description of evidence",
        SubmittedBy: Guid.NewGuid(),
        IsFromMerchant: true,
        FileUrl: "https://example.com/file.pdf",
        FileName: "file.pdf",
        FileSize: 1024,
        MimeType: "application/pdf");

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.Validate(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_DisputeId_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { DisputeId = Guid.Empty });
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Empty_Title_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Title = "" });
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Title_Exceeding_200_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Title = new string('a', 201) });
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Empty_Description_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Description = "" });
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Description_Exceeding_5000_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Description = new string('a', 5001) });
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Empty_SubmittedBy_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { SubmittedBy = Guid.Empty });
        result.IsValid.Should().BeFalse();
    }
}

public class ResolveDisputeCommandValidatorTests
{
    private readonly ResolveDisputeCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var cmd = new ResolveDisputeCommand(Guid.NewGuid(), DisputeResolution.Lost, "Notes", Guid.NewGuid());
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_DisputeId_Fails()
    {
        var cmd = new ResolveDisputeCommand(Guid.Empty, DisputeResolution.Lost);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Notes_Exceeding_2000_Fails()
    {
        var cmd = new ResolveDisputeCommand(Guid.NewGuid(), DisputeResolution.Lost, new string('n', 2001));
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Null_Notes_Passes()
    {
        var cmd = new ResolveDisputeCommand(Guid.NewGuid(), DisputeResolution.Lost, null);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }
}

public class UpdateDisputeStatusCommandValidatorTests
{
    private readonly UpdateDisputeStatusCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var cmd = new UpdateDisputeStatusCommand(Guid.NewGuid(), DisputeStatus.UnderReview);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_DisputeId_Fails()
    {
        var cmd = new UpdateDisputeStatusCommand(Guid.Empty, DisputeStatus.UnderReview);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void FutureDueDate_Passes()
    {
        var cmd = new UpdateDisputeStatusCommand(Guid.NewGuid(), DisputeStatus.UnderReview, DateTime.UtcNow.AddDays(7));
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void PastDueDate_Fails()
    {
        var cmd = new UpdateDisputeStatusCommand(Guid.NewGuid(), DisputeStatus.UnderReview, DateTime.UtcNow.AddDays(-1));
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }
}

public class GetPaymentHistoryQueryValidatorTests
{
    private readonly GetPaymentHistoryQueryValidator _validator = new();

    [Fact]
    public void Valid_AdminRequest_Passes()
    {
        var query = new GetPaymentHistoryQuery(IsAdminRequest: true, PageNumber: 1, PageSize: 20);
        var result = _validator.Validate(query);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void NonAdmin_WithoutUserId_Fails()
    {
        var query = new GetPaymentHistoryQuery(UserId: null, IsAdminRequest: false, PageNumber: 1, PageSize: 20);
        var result = _validator.Validate(query);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void NonAdmin_WithUserId_Passes()
    {
        var query = new GetPaymentHistoryQuery(UserId: Guid.NewGuid(), IsAdminRequest: false, PageNumber: 1, PageSize: 20);
        var result = _validator.Validate(query);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void PageSize_Zero_Fails()
    {
        var query = new GetPaymentHistoryQuery(IsAdminRequest: true, PageSize: 0);
        var result = _validator.Validate(query);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void PageSize_Over100_Fails()
    {
        var query = new GetPaymentHistoryQuery(IsAdminRequest: true, PageSize: 101);
        var result = _validator.Validate(query);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void PageNumber_Zero_Fails()
    {
        var query = new GetPaymentHistoryQuery(IsAdminRequest: true, PageNumber: 0);
        var result = _validator.Validate(query);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void StartDate_After_EndDate_Fails()
    {
        var query = new GetPaymentHistoryQuery(
            IsAdminRequest: true,
            StartDate: DateTime.UtcNow.AddDays(1),
            EndDate: DateTime.UtcNow);
        var result = _validator.Validate(query);
        result.IsValid.Should().BeFalse();
    }
}

public class GetTransactionHistoryQueryValidatorTests
{
    private readonly GetTransactionHistoryQueryValidator _validator = new();

    [Fact]
    public void Valid_Query_Passes()
    {
        var query = new GetTransactionHistoryQuery(Guid.NewGuid(), 0, 50);
        var result = _validator.Validate(query);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_UserId_Fails()
    {
        var query = new GetTransactionHistoryQuery(Guid.Empty);
        var result = _validator.Validate(query);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Negative_Skip_Fails()
    {
        var query = new GetTransactionHistoryQuery(Guid.NewGuid(), Skip: -1);
        var result = _validator.Validate(query);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Zero_Take_Fails()
    {
        var query = new GetTransactionHistoryQuery(Guid.NewGuid(), Take: 0);
        var result = _validator.Validate(query);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Take_Over1000_Fails()
    {
        var query = new GetTransactionHistoryQuery(Guid.NewGuid(), Take: 1001);
        var result = _validator.Validate(query);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ValidTypeFilter_Passes()
    {
        var query = new GetTransactionHistoryQuery(Guid.NewGuid(), TypeFilter: WalletTransactionType.Credit);
        var result = _validator.Validate(query);
        result.IsValid.Should().BeTrue();
    }
}

#endregion

#region Handler

public class ValidateTaxExemptionHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsValidResult()
    {
        var tenantId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var exemption = CustomerTaxExemption.Create(
            tenantId,
            customerId,
            "US-CA",
            TaxExemptionType.NonProfit,
            "CERT-123",
            DateTime.UtcNow.AddYears(-1),
            DateTime.UtcNow.AddYears(1));
        exemption.MarkVerified("test");

        await using var context = CreateContext(seed => seed.Set<CustomerTaxExemption>().Add(exemption));
        var handler = new ValidateTaxExemptionHandler(context);
        var command = new ValidateTaxExemptionCommand(
            "US-CA", "nonprofit", "CERT-123", null, customerId, DateTime.UtcNow);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsValid.Should().BeTrue();
        result.ExemptionType.Should().Be(TaxExemptionType.NonProfit.ToString());
        result.ExemptionRate.Should().Be(1.0m);
        result.ValidationMessage.Should().Be("Exemption certificate is active and verified");
        result.Warnings.Should().BeNull();
        result.ValidFrom.Should().NotBeNull();
        result.ValidTo.Should().NotBeNull();
    }

    private static PaymentsPersistenceTestDbContext CreateContext(Action<PaymentsPersistenceTestDbContext>? seed = null)
    {
        var options = new DbContextOptionsBuilder<PaymentsPersistenceTestDbContext>()
            .UseInMemoryDatabase($"payments-exemption-{Guid.NewGuid()}")
            .Options;

        var context = new PaymentsPersistenceTestDbContext(options);
        seed?.Invoke(context);
        context.SaveChanges();
        return context;
    }
}

#endregion

#region Records and DTOs

public class PaymentRecordConstructionTests
{
    [Fact]
    public void AddDisputeEvidenceCommand_CanBeCreated()
    {
        var cmd = new AddDisputeEvidenceCommand(
            Guid.NewGuid(), EvidenceType.Documentation, "Title", "Desc",
            Guid.NewGuid(), true, "https://url", "file.pdf", 1024, "application/pdf");

        cmd.Title.Should().Be("Title");
        cmd.IsFromMerchant.Should().BeTrue();
        cmd.FileUrl.Should().Be("https://url");
    }

    [Fact]
    public void ValidateTaxExemptionCommand_CanBeCreated()
    {
        var cmd = new ValidateTaxExemptionCommand(
            "US-CA", "nonprofit", "CERT-123", "VAT-456", Guid.NewGuid(), DateTime.UtcNow);

        cmd.JurisdictionCode.Should().Be("US-CA");
        cmd.ExemptionType.Should().Be("nonprofit");
    }

    [Fact]
    public void TaxExemptionValidationResult_CanBeCreated()
    {
        var result = new TaxExemptionValidationResult(
            true, "nonprofit", 1.0m, DateTime.UtcNow, DateTime.UtcNow.AddYears(1),
            "Valid", new List<string> { "Warning1" });

        result.IsValid.Should().BeTrue();
        result.Warnings.Should().HaveCount(1);
    }

    [Fact]
    public void GetPaymentHistoryQuery_CanBeCreated()
    {
        var query = new GetPaymentHistoryQuery(
            Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow, 2, 50, true);

        query.PageNumber.Should().Be(2);
        query.PageSize.Should().Be(50);
        query.IsAdminRequest.Should().BeTrue();
    }

    [Fact]
    public void CreateTaxRuleCommand_CanBeCreated()
    {
        var cmd = new CreateTaxRuleCommand(
            "US-CA", "digital", "individual", 0.0725m,
            DateTime.UtcNow, DateTime.UtcNow.AddYears(1), "CA sales tax");

        cmd.JurisdictionCode.Should().Be("US-CA");
        cmd.Rate.Should().Be(0.0725m);
    }

    [Fact]
    public void CreateTaxJurisdictionCommand_CanBeCreated()
    {
        var cmd = new CreateTaxJurisdictionCommand(
            "US-CA", "California", "US", "CA", "SalesTax", 0.0725m);

        cmd.Code.Should().Be("US-CA");
        cmd.DefaultRate.Should().Be(0.0725m);
    }

    [Fact]
    public void PatchTaxRuleCommand_CanBeCreated()
    {
        var cmd = new PatchTaxRuleCommand(
            Guid.NewGuid(), 0.08m, DateTime.UtcNow, null, "Updated", true);

        cmd.Rate.Should().Be(0.08m);
        cmd.IsActive.Should().BeTrue();
    }

    [Fact]
    public void PatchTaxJurisdictionCommand_CanBeCreated()
    {
        var cmd = new PatchTaxJurisdictionCommand(
            Guid.NewGuid(), "Updated Name", "VAT", 0.20m, true);

        cmd.Name.Should().Be("Updated Name");
        cmd.DefaultRate.Should().Be(0.20m);
    }

    [Fact]
    public void GatewayPaymentRequest_CanBeCreated()
    {
        var req = new GatewayPaymentRequest(
            "idem-key", 99.99m, "USD", "cus_123", "pm_456",
            "Test payment", new Dictionary<string, string> { { "order", "123" } });

        req.Amount.Should().Be(99.99m);
        req.Currency.Should().Be("USD");
        req.Metadata.Should().ContainKey("order");
    }

    [Fact]
    public void GatewayRefundRequest_CanBeCreated()
    {
        var req = new GatewayRefundRequest("idem-key", "txn_123", 50.00m, "customer_request");

        req.OriginalTransactionId.Should().Be("txn_123");
        req.Amount.Should().Be(50.00m);
    }

    [Fact]
    public void GatewayCustomerRequest_CanBeCreated()
    {
        var req = new GatewayCustomerRequest(
            "user@example.com", "John Doe", "+1234567890",
            new Dictionary<string, string> { { "source", "web" } });

        req.Email.Should().Be("user@example.com");
        req.Name.Should().Be("John Doe");
    }

    [Fact]
    public void GatewayPaymentMethodRequest_CanBeCreated()
    {
        var req = new GatewayPaymentMethodRequest("cus_123", "tok_456", true);

        req.CustomerId.Should().Be("cus_123");
        req.SetAsDefault.Should().BeTrue();
    }

    [Fact]
    public void PatchWalletCommand_CanBeCreated()
    {
        var cmd = new PatchWalletCommand(Guid.NewGuid(), "EUR", 1000m, 5000m);

        cmd.Currency.Should().Be("EUR");
        cmd.DailyLimit.Should().Be(1000m);
        cmd.MonthlyLimit.Should().Be(5000m);
    }

    [Fact]
    public void ListWalletsQuery_CanBeCreated()
    {
        var query = new ListWalletsQuery(1, 20, "USD", false);

        query.Page.Should().Be(1);
        query.PageSize.Should().Be(20);
        query.Currency.Should().Be("USD");
        query.IsFrozen.Should().BeFalse();
    }

    [Fact]
    public void ValidateTaxExemptionRequest_CanBeCreated()
    {
        var req = new ValidateTaxExemptionRequest(
            "US-NY", "diplomatic", "CERT-789", null, Guid.NewGuid(), DateTime.UtcNow);

        req.JurisdictionCode.Should().Be("US-NY");
        req.ExemptionType.Should().Be("diplomatic");
    }

    [Fact]
    public void CreateTaxRuleRequest_CanBeCreated()
    {
        var req = new CreateTaxRuleRequest(
            "GB", "software", "business", 0.20m,
            DateTime.UtcNow, null, "UK VAT");

        req.JurisdictionCode.Should().Be("GB");
        req.Rate.Should().Be(0.20m);
    }

    [Fact]
    public void PatchTaxRuleRequest_CanBeCreated()
    {
        var req = new PatchTaxRuleRequest(0.15m, null, null, "Reduced rate", true);

        req.Rate.Should().Be(0.15m);
        req.IsActive.Should().BeTrue();
    }

    [Fact]
    public void TaxRuleDto_CanBeCreated()
    {
        var dto = new TaxRuleDto(
            Guid.NewGuid(), "US-CA", "digital", "individual",
            0.0725m, DateTime.UtcNow, null, "Sales tax", true);

        dto.JurisdictionCode.Should().Be("US-CA");
        dto.IsActive.Should().BeTrue();
    }

    [Fact]
    public void CreateTaxJurisdictionRequest_CanBeCreated()
    {
        var req = new CreateTaxJurisdictionRequest(
            "US-NY", "New York", "US", "NY", "SalesTax", 0.08m);

        req.Code.Should().Be("US-NY");
        req.DefaultRate.Should().Be(0.08m);
    }

    [Fact]
    public void TaxJurisdictionDto_CanBeCreated()
    {
        var dto = new TaxJurisdictionDto(
            Guid.NewGuid(), "DE", "Germany", "DE", null,
            "VAT", 0.19m, true);

        dto.Code.Should().Be("DE");
        dto.TaxType.Should().Be("VAT");
    }

    [Fact]
    public void GatewayPaymentResult_CanBeCreated()
    {
        var res = new GatewayPaymentResult(
            true, "txn_abc", "pi_xyz", null, null,
            PaymentStatus.Succeeded, DateTime.UtcNow);

        res.Success.Should().BeTrue();
        res.Status.Should().Be(PaymentStatus.Succeeded);
    }

    [Fact]
    public void GatewayRefundResult_CanBeCreated()
    {
        var res = new GatewayRefundResult(true, "re_123", 50.00m, null, null, DateTime.UtcNow);

        res.Success.Should().BeTrue();
        res.AmountRefunded.Should().Be(50.00m);
    }

    [Fact]
    public void GatewayCustomerResult_CanBeCreated()
    {
        var res = new GatewayCustomerResult(true, "cus_ext_123", null, null);

        res.Success.Should().BeTrue();
        res.ExternalCustomerId.Should().Be("cus_ext_123");
    }

    [Fact]
    public void GatewayPaymentMethodResult_CanBeCreated()
    {
        var res = new GatewayPaymentMethodResult(
            true, "pm_ext_123", "4242", "visa", 12, 2026, null, null);

        res.Success.Should().BeTrue();
        res.CardLast4.Should().Be("4242");
        res.CardBrand.Should().Be("visa");
    }
}

#endregion

#region LedgerAccount Extensions

public class LedgerAccountAdditionalExtensionTests
{
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
    [InlineData(LedgerAccount.TransactionFeeRevenue, true)]
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
    [InlineData(LedgerAccount.Cash, false)]
    [InlineData(LedgerAccount.ProductRevenue, false)]
    public void IsExpense_ReturnsCorrectly(LedgerAccount account, bool expected)
    {
        account.IsExpense().Should().Be(expected);
    }

    [Theory]
    [InlineData(LedgerAccount.SalesDiscounts, true)]
    [InlineData(LedgerAccount.ReturnsAndAllowances, true)]
    [InlineData(LedgerAccount.Cash, false)]
    [InlineData(LedgerAccount.ProductRevenue, false)]
    public void IsContra_ReturnsCorrectly(LedgerAccount account, bool expected)
    {
        account.IsContra().Should().Be(expected);
    }
}

#endregion
