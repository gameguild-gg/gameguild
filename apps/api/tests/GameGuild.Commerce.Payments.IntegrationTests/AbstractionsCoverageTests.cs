using FluentAssertions;
using Moq;

namespace GameGuild.Commerce.Payments.IntegrationTests;

public class AbstractionsCoverageTests
{
    [Fact]
    public void GatewayContracts_ShouldExposeAllRecordProperties()
    {
        var processedAt = new DateTime(2026, 5, 25, 10, 30, 0, DateTimeKind.Utc);
        var metadata = new Dictionary<string, string> { ["tenant"] = "tenant-1" };

        var paymentRequest = new GatewayPaymentRequest(
            "idem-pay",
            149.99m,
            "USD",
            "cus_123",
            "pm_123",
            "Subscription payment",
            metadata);

        paymentRequest.IdempotencyKey.Should().Be("idem-pay");
        paymentRequest.Amount.Should().Be(149.99m);
        paymentRequest.Currency.Should().Be("USD");
        paymentRequest.CustomerId.Should().Be("cus_123");
        paymentRequest.PaymentMethodId.Should().Be("pm_123");
        paymentRequest.Description.Should().Be("Subscription payment");
        paymentRequest.Metadata.Should().BeSameAs(metadata);

        var paymentResult = new GatewayPaymentResult(
            true,
            "txn_123",
            "pi_123",
            null,
            null,
            PaymentStatus.Succeeded,
            processedAt);

        paymentResult.Success.Should().BeTrue();
        paymentResult.TransactionId.Should().Be("txn_123");
        paymentResult.ExternalPaymentId.Should().Be("pi_123");
        paymentResult.ErrorCode.Should().BeNull();
        paymentResult.ErrorMessage.Should().BeNull();
        paymentResult.Status.Should().Be(PaymentStatus.Succeeded);
        paymentResult.ProcessedAt.Should().Be(processedAt);

        var refundRequest = new GatewayRefundRequest("idem-refund", "txn_123", 25.50m, "Customer request");

        refundRequest.IdempotencyKey.Should().Be("idem-refund");
        refundRequest.OriginalTransactionId.Should().Be("txn_123");
        refundRequest.Amount.Should().Be(25.50m);
        refundRequest.Reason.Should().Be("Customer request");

        var refundResult = new GatewayRefundResult(true, "re_123", 25.50m, null, null, processedAt);

        refundResult.Success.Should().BeTrue();
        refundResult.RefundId.Should().Be("re_123");
        refundResult.AmountRefunded.Should().Be(25.50m);
        refundResult.ErrorCode.Should().BeNull();
        refundResult.ErrorMessage.Should().BeNull();
        refundResult.ProcessedAt.Should().Be(processedAt);

        var customerRequest = new GatewayCustomerRequest("customer@example.com", "Customer", "+1-555-0100", metadata);

        customerRequest.Email.Should().Be("customer@example.com");
        customerRequest.Name.Should().Be("Customer");
        customerRequest.Phone.Should().Be("+1-555-0100");
        customerRequest.Metadata.Should().BeSameAs(metadata);

        var customerResult = new GatewayCustomerResult(true, "cus_123", null, null);

        customerResult.Success.Should().BeTrue();
        customerResult.ExternalCustomerId.Should().Be("cus_123");
        customerResult.ErrorCode.Should().BeNull();
        customerResult.ErrorMessage.Should().BeNull();

        var paymentMethodRequest = new GatewayPaymentMethodRequest("cus_123", "pm_token_123", true);

        paymentMethodRequest.CustomerId.Should().Be("cus_123");
        paymentMethodRequest.PaymentMethodToken.Should().Be("pm_token_123");
        paymentMethodRequest.SetAsDefault.Should().BeTrue();

        var paymentMethodResult = new GatewayPaymentMethodResult(
            true,
            "pm_123",
            "4242",
            "visa",
            12,
            2030,
            null,
            null);

        paymentMethodResult.Success.Should().BeTrue();
        paymentMethodResult.ExternalPaymentMethodId.Should().Be("pm_123");
        paymentMethodResult.CardLast4.Should().Be("4242");
        paymentMethodResult.CardBrand.Should().Be("visa");
        paymentMethodResult.ExpiryMonth.Should().Be(12);
        paymentMethodResult.ExpiryYear.Should().Be(2030);
        paymentMethodResult.ErrorCode.Should().BeNull();
        paymentMethodResult.ErrorMessage.Should().BeNull();

        var cancellationResult = new GatewayCancellationResult(true, null, null, processedAt);

        cancellationResult.Success.Should().BeTrue();
        cancellationResult.ErrorCode.Should().BeNull();
        cancellationResult.ErrorMessage.Should().BeNull();
        cancellationResult.EffectiveDate.Should().Be(processedAt);
    }

    [Fact]
    public async Task NullPlanPricingResolver_ShouldReturnNullAndFalse()
    {
        var resolver = new NullPlanPricingResolver();
        var planId = Guid.NewGuid();

        var monthlyPrice = await resolver.GetPlanMonthlyPriceAsync(planId);
        var annualPrice = await resolver.GetPlanPriceAsync(planId, BillingCycle.Annually);
        var exists = await resolver.PlanExistsAsync(planId);

        monthlyPrice.Should().BeNull();
        annualPrice.Should().BeNull();
        exists.Should().BeFalse();
    }

    [Fact]
    public void AddDisputeEvidenceCommand_ShouldExposeAllProperties()
    {
        var disputeId = Guid.NewGuid();
        var submittedBy = Guid.NewGuid();

        var command = new AddDisputeEvidenceCommand(
            disputeId,
            EvidenceType.Documentation,
            "Signed contract",
            "Contract proving buyer consent",
            submittedBy,
            true,
            "https://cdn.example.com/evidence/contract.pdf",
            "contract.pdf",
            2048,
            "application/pdf");

        command.DisputeId.Should().Be(disputeId);
        command.EvidenceType.Should().Be(EvidenceType.Documentation);
        command.Title.Should().Be("Signed contract");
        command.Description.Should().Be("Contract proving buyer consent");
        command.SubmittedBy.Should().Be(submittedBy);
        command.IsFromMerchant.Should().BeTrue();
        command.FileUrl.Should().Be("https://cdn.example.com/evidence/contract.pdf");
        command.FileName.Should().Be("contract.pdf");
        command.FileSize.Should().Be(2048);
        command.MimeType.Should().Be("application/pdf");
    }

    [Fact]
    public async Task AddDisputeEvidenceCommandHandler_ShouldDelegateToDisputeService()
    {
        var disputeService = new Mock<IDisputeService>();
        var handler = new AddDisputeEvidenceCommandHandler(disputeService.Object);
        var command = new AddDisputeEvidenceCommand(
            Guid.NewGuid(),
            EvidenceType.Receipt,
            "Receipt",
            "Proof of payment",
            Guid.NewGuid(),
            false,
            "https://cdn.example.com/evidence/receipt.png",
            "receipt.png",
            1024,
            "image/png");

        var evidence = new DisputeEvidence
        {
            DisputeId = command.DisputeId,
            EvidenceType = command.EvidenceType,
            Title = command.Title,
            Description = command.Description,
            SubmittedBy = command.SubmittedBy,
            IsFromMerchant = command.IsFromMerchant,
            FileUrl = command.FileUrl,
            FileName = command.FileName,
            FileSize = command.FileSize,
            MimeType = command.MimeType
        };

        disputeService
            .Setup(service => service.AddEvidenceAsync(
                command.DisputeId,
                command.EvidenceType,
                command.Title,
                command.Description,
                command.SubmittedBy,
                command.IsFromMerchant,
                command.FileUrl,
                command.FileName,
                command.FileSize,
                command.MimeType,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(evidence);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().BeSameAs(evidence);
        disputeService.Verify(service => service.AddEvidenceAsync(
            command.DisputeId,
            command.EvidenceType,
            command.Title,
            command.Description,
            command.SubmittedBy,
            command.IsFromMerchant,
            command.FileUrl,
            command.FileName,
            command.FileSize,
            command.MimeType,
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
