using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace GameGuild.Commerce.Payments.UnitTests.Validators;

public class ProcessPaymentCommandValidatorTests
{
    private readonly ProcessPaymentCommandValidator _validator = new();

    [Fact]
    public void ShouldFail_WhenTenantIdEmpty()
    {
        var cmd = new ProcessPaymentCommand(Guid.Empty, Guid.NewGuid(), 100m, "pm_123");
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void ShouldFail_WhenSubscriptionIdEmpty()
    {
        var cmd = new ProcessPaymentCommand(Guid.NewGuid(), Guid.Empty, 100m, "pm_123");
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.SubscriptionId);
    }

    [Fact]
    public void ShouldFail_WhenAmountZero()
    {
        var cmd = new ProcessPaymentCommand(Guid.NewGuid(), Guid.NewGuid(), 0m, "pm_123");
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void ShouldFail_WhenAmountExceedsMax()
    {
        var cmd = new ProcessPaymentCommand(Guid.NewGuid(), Guid.NewGuid(), 1000001m, "pm_123");
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void ShouldFail_WhenPaymentMethodIdEmpty()
    {
        var cmd = new ProcessPaymentCommand(Guid.NewGuid(), Guid.NewGuid(), 100m, "");
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.PaymentMethodId);
    }

    [Fact]
    public void ShouldFail_WhenPaymentMethodIdLooksLikeRawCardNumber()
    {
        var cmd = new ProcessPaymentCommand(Guid.NewGuid(), Guid.NewGuid(), 100m, "4242424242424242");
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.PaymentMethodId);
    }

    [Fact]
    public void ShouldPass_WhenValid()
    {
        var cmd = new ProcessPaymentCommand(Guid.NewGuid(), Guid.NewGuid(), 100m, "pm_123");
        _validator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }
}

public class ProcessRefundCommandValidatorTests
{
    private readonly ProcessRefundCommandValidator _validator = new();

    [Fact]
    public void ShouldFail_WhenPaymentIdEmpty()
    {
        var cmd = new ProcessRefundCommand(Guid.Empty, 50m, "reason");
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.PaymentId);
    }

    [Fact]
    public void ShouldFail_WhenAmountZero()
    {
        var cmd = new ProcessRefundCommand(Guid.NewGuid(), 0m, "reason");
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void ShouldFail_WhenAmountExceedsMax()
    {
        var cmd = new ProcessRefundCommand(Guid.NewGuid(), 10001m, "reason");
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void ShouldFail_WhenReasonEmpty()
    {
        var cmd = new ProcessRefundCommand(Guid.NewGuid(), 50m, "");
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Reason);
    }

    [Fact]
    public void ShouldFail_WhenReasonTooLong()
    {
        var cmd = new ProcessRefundCommand(Guid.NewGuid(), 50m, new string('R', 501));
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Reason);
    }

    [Fact]
    public void ShouldPass_WhenValid()
    {
        var cmd = new ProcessRefundCommand(Guid.NewGuid(), 50m, "Customer request");
        _validator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }
}

public class CreateWalletCommandValidatorTests
{
    private readonly CreateWalletCommandValidator _validator = new();

    [Fact]
    public void ShouldFail_WhenUserIdEmpty()
    {
        var cmd = new CreateWalletCommand(Guid.Empty);
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void ShouldFail_WhenCurrencyWrongLength()
    {
        var cmd = new CreateWalletCommand(Guid.NewGuid(), Currency: "US");
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Currency);
    }

    [Fact]
    public void ShouldPass_WhenValid()
    {
        var cmd = new CreateWalletCommand(Guid.NewGuid());
        _validator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }
}

public class AddFundsCommandValidatorTests
{
    private readonly AddFundsCommandValidator _validator = new();

    [Fact]
    public void ShouldFail_WhenUserIdEmpty()
    {
        var cmd = new AddFundsCommand(Guid.Empty, 100m, "deposit");
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void ShouldFail_WhenAmountZero()
    {
        var cmd = new AddFundsCommand(Guid.NewGuid(), 0m, "deposit");
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void ShouldFail_WhenAmountExceedsMax()
    {
        var cmd = new AddFundsCommand(Guid.NewGuid(), 100001m, "deposit");
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void ShouldFail_WhenDescriptionEmpty()
    {
        var cmd = new AddFundsCommand(Guid.NewGuid(), 100m, "");
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void ShouldPass_WhenValid()
    {
        var cmd = new AddFundsCommand(Guid.NewGuid(), 100m, "Top up");
        _validator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }
}

public class DeductFundsCommandValidatorTests
{
    private readonly DeductFundsCommandValidator _validator = new();

    [Fact]
    public void ShouldFail_WhenUserIdEmpty()
    {
        var cmd = new DeductFundsCommand(Guid.Empty, 50m, "payment");
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void ShouldFail_WhenAmountZero()
    {
        var cmd = new DeductFundsCommand(Guid.NewGuid(), 0m, "payment");
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void ShouldPass_WhenValid()
    {
        var cmd = new DeductFundsCommand(Guid.NewGuid(), 50m, "Purchase");
        _validator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }
}

public class TransferFundsCommandValidatorTests
{
    private readonly TransferFundsCommandValidator _validator = new();

    [Fact]
    public void ShouldFail_WhenFromUserIdEmpty()
    {
        var cmd = new TransferFundsCommand(Guid.Empty, Guid.NewGuid(), 50m, "transfer");
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.FromUserId);
    }

    [Fact]
    public void ShouldFail_WhenToUserIdEmpty()
    {
        var cmd = new TransferFundsCommand(Guid.NewGuid(), Guid.Empty, 50m, "transfer");
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.ToUserId);
    }

    [Fact]
    public void ShouldFail_WhenSelfTransfer()
    {
        var userId = Guid.NewGuid();
        var cmd = new TransferFundsCommand(userId, userId, 50m, "transfer");
        var result = _validator.TestValidate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ShouldFail_WhenAmountExceedsMax()
    {
        var cmd = new TransferFundsCommand(Guid.NewGuid(), Guid.NewGuid(), 50001m, "transfer");
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void ShouldPass_WhenValid()
    {
        var cmd = new TransferFundsCommand(Guid.NewGuid(), Guid.NewGuid(), 50m, "Transfer");
        _validator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }
}

public class LockWalletCommandValidatorTests
{
    private readonly LockWalletCommandValidator _validator = new();

    [Fact]
    public void ShouldFail_WhenUserIdEmpty()
    {
        var cmd = new LockWalletCommand(Guid.Empty, "suspicious");
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void ShouldFail_WhenReasonEmpty()
    {
        var cmd = new LockWalletCommand(Guid.NewGuid(), "");
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Reason);
    }

    [Fact]
    public void ShouldPass_WhenValid()
    {
        var cmd = new LockWalletCommand(Guid.NewGuid(), "Suspicious activity");
        _validator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }
}

public class UnlockWalletCommandValidatorTests
{
    private readonly UnlockWalletCommandValidator _validator = new();

    [Fact]
    public void ShouldFail_WhenUserIdEmpty()
    {
        var cmd = new UnlockWalletCommand(Guid.Empty);
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void ShouldPass_WhenValid()
    {
        var cmd = new UnlockWalletCommand(Guid.NewGuid());
        _validator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }
}

public class CreateDisputeCommandValidatorTests
{
    private readonly CreateDisputeCommandValidator _validator = new();

    [Fact]
    public void ShouldFail_WhenPaymentIdEmpty()
    {
        var cmd = new CreateDisputeCommand(Guid.Empty, Guid.NewGuid(), DisputeType.Fraudulent, 100m, "reason", "desc");
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.PaymentId);
    }

    [Fact]
    public void ShouldFail_WhenAmountZero()
    {
        var cmd = new CreateDisputeCommand(Guid.NewGuid(), Guid.NewGuid(), DisputeType.Fraudulent, 0m, "reason", "desc");
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void ShouldFail_WhenReasonEmpty()
    {
        var cmd = new CreateDisputeCommand(Guid.NewGuid(), Guid.NewGuid(), DisputeType.Fraudulent, 100m, "", "desc");
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Reason);
    }

    [Fact]
    public void ShouldPass_WhenValid()
    {
        var cmd = new CreateDisputeCommand(Guid.NewGuid(), Guid.NewGuid(), DisputeType.ProductNotReceived, 100m, "Unauthorized", "Full description");
        _validator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }
}

public class CancelDisputeCommandValidatorTests
{
    private readonly CancelDisputeCommandValidator _validator = new();

    [Fact]
    public void ShouldFail_WhenDisputeIdEmpty()
    {
        var cmd = new CancelDisputeCommand(Guid.Empty, "no longer needed");
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.DisputeId);
    }

    [Fact]
    public void ShouldFail_WhenReasonEmpty()
    {
        var cmd = new CancelDisputeCommand(Guid.NewGuid(), "");
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Reason);
    }

    [Fact]
    public void ShouldPass_WhenValid()
    {
        var cmd = new CancelDisputeCommand(Guid.NewGuid(), "Resolved directly");
        _validator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }
}

public class RetryPaymentCommandValidatorTests
{
    private readonly RetryPaymentCommandValidator _validator = new();

    [Fact]
    public void ShouldFail_WhenPaymentIdEmpty()
    {
        var cmd = new RetryPaymentCommand(Guid.Empty);
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.PaymentId);
    }

    [Fact]
    public void ShouldPass_WhenValid()
    {
        var cmd = new RetryPaymentCommand(Guid.NewGuid());
        _validator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }
}

public class CreateLedgerEntryCommandValidatorTests
{
    private readonly CreateLedgerEntryCommandValidator _validator = new();

    [Fact]
    public void ShouldFail_WhenDebitAccountEmpty()
    {
        var cmd = new CreateLedgerEntryCommand(LedgerEntryType.Revenue, "", "4000", 100m, "USD", "Test");
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.DebitAccount);
    }

    [Fact]
    public void ShouldFail_WhenAmountZero()
    {
        var cmd = new CreateLedgerEntryCommand(LedgerEntryType.Revenue, "1000", "4000", 0m, "USD", "Test");
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void ShouldFail_WhenCurrencyWrongLength()
    {
        var cmd = new CreateLedgerEntryCommand(LedgerEntryType.Revenue, "1000", "4000", 100m, "US", "Test");
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Currency);
    }

    [Fact]
    public void ShouldPass_WhenValid()
    {
        var cmd = new CreateLedgerEntryCommand(LedgerEntryType.Expense, "1000", "4000", 100m, "USD", "Sale revenue");
        _validator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }
}

public class RecordRevenueEventCommandValidatorTests
{
    private readonly RecordRevenueEventCommandValidator _validator = new();

    [Fact]
    public void ShouldFail_WhenAmountZero()
    {
        var cmd = new RecordRevenueEventCommand(RevenueEventType.PaymentReceived, 0m, "USD", RevenueSource.OneTimePayment, "ref-1");
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void ShouldFail_WhenReferenceIdEmpty()
    {
        var cmd = new RecordRevenueEventCommand(RevenueEventType.PaymentReceived, 100m, "USD", RevenueSource.OneTimePayment, "");
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.ReferenceId);
    }

    [Fact]
    public void ShouldPass_WhenValid()
    {
        var cmd = new RecordRevenueEventCommand(RevenueEventType.SubscriptionStarted, 100m, "USD", RevenueSource.OneTimePayment, "ref-1");
        _validator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }
}

public class ReconcileLedgerCommandValidatorTests
{
    private readonly ReconcileLedgerCommandValidator _validator = new();

    [Fact]
    public void ShouldFail_WhenEntryIdEmpty()
    {
        var cmd = new ReconcileLedgerCommand(Guid.Empty, Guid.NewGuid());
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.EntryId);
    }

    [Fact]
    public void ShouldFail_WhenReconciledByEmpty()
    {
        var cmd = new ReconcileLedgerCommand(Guid.NewGuid(), Guid.Empty);
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.ReconciledBy);
    }

    [Fact]
    public void ShouldPass_WhenValid()
    {
        var cmd = new ReconcileLedgerCommand(Guid.NewGuid(), Guid.NewGuid());
        _validator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }
}

public class RecordAuditTrailCommandValidatorTests
{
    private readonly RecordAuditTrailCommandValidator _validator = new();

    [Fact]
    public void ShouldFail_WhenEntityTypeEmpty()
    {
        var cmd = new RecordAuditTrailCommand("", Guid.NewGuid(), "Create", Guid.NewGuid(), null, null, null, null, null);
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.EntityType);
    }

    [Fact]
    public void ShouldFail_WhenEntityIdEmpty()
    {
        var cmd = new RecordAuditTrailCommand("Payment", Guid.Empty, "Create", Guid.NewGuid(), null, null, null, null, null);
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.EntityId);
    }

    [Fact]
    public void ShouldPass_WhenValid()
    {
        var cmd = new RecordAuditTrailCommand("Payment", Guid.NewGuid(), "Create", Guid.NewGuid(), null, null, null, null, null);
        _validator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }
}

public class UpdatePaymentStatusCommandValidatorTests
{
    private readonly UpdatePaymentStatusCommandValidator _validator = new();

    [Fact]
    public void ShouldFail_WhenPaymentIdEmpty()
    {
        var cmd = new UpdatePaymentStatusCommand(Guid.Empty, PaymentStatus.Succeeded);
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.PaymentId);
    }

    [Fact]
    public void ShouldPass_WhenValid()
    {
        var cmd = new UpdatePaymentStatusCommand(Guid.NewGuid(), PaymentStatus.Succeeded);
        _validator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }
}

public class CalculateTaxCommandValidatorTests
{
    private readonly CalculateTaxCommandValidator _validator = new();

    [Fact]
    public void ShouldFail_WhenAmountZero()
    {
        var cmd = new CalculateTaxCommand("US", 0m, "USD", "Individual");
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void ShouldFail_WhenJurisdictionCodeEmpty()
    {
        var cmd = new CalculateTaxCommand("", 100m, "USD", "Individual");
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.JurisdictionCode);
    }

    [Fact]
    public void ShouldPass_WhenValid()
    {
        var cmd = new CalculateTaxCommand("US", 100m, "USD", "Individual");
        _validator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }
}
