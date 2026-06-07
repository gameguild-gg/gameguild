using FluentAssertions;
using FluentValidation.TestHelper;
using GameGuild.Commerce.Payments;
using GameGuild.Commerce.Payments.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GameGuild.Commerce.Payments.UnitTests;

#region ProcessPaymentCommand Validator Tests

public class ProcessPaymentCommandValidatorTests
{
    private readonly ProcessPaymentCommandValidator _validator = new();

    [Fact]
    public void Valid_ShouldPass()
    {
        var cmd = new ProcessPaymentCommand(Guid.NewGuid(), Guid.NewGuid(), 99.99m, "pm_test");
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyTenantId_ShouldFail()
    {
        var cmd = new ProcessPaymentCommand(Guid.Empty, Guid.NewGuid(), 99.99m, "pm_test");
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void EmptySubscriptionId_ShouldFail()
    {
        var cmd = new ProcessPaymentCommand(Guid.NewGuid(), Guid.Empty, 99.99m, "pm_test");
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.SubscriptionId);
    }

    [Fact]
    public void ZeroAmount_ShouldFail()
    {
        var cmd = new ProcessPaymentCommand(Guid.NewGuid(), Guid.NewGuid(), 0m, "pm_test");
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void NegativeAmount_ShouldFail()
    {
        var cmd = new ProcessPaymentCommand(Guid.NewGuid(), Guid.NewGuid(), -5m, "pm_test");
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void AmountExceedsMax_ShouldFail()
    {
        var cmd = new ProcessPaymentCommand(Guid.NewGuid(), Guid.NewGuid(), 1000001m, "pm_test");
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void EmptyPaymentMethodId_ShouldFail()
    {
        var cmd = new ProcessPaymentCommand(Guid.NewGuid(), Guid.NewGuid(), 99.99m, "");
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.PaymentMethodId);
    }

    [Fact]
    public void TooLongPaymentMethodId_ShouldFail()
    {
        var cmd = new ProcessPaymentCommand(Guid.NewGuid(), Guid.NewGuid(), 99.99m, new string('x', 101));
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.PaymentMethodId);
    }

    [Fact]
    public void RawCardNumberPaymentMethodId_ShouldFail()
    {
        var cmd = new ProcessPaymentCommand(Guid.NewGuid(), Guid.NewGuid(), 99.99m, "4242 4242 4242 4242");
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.PaymentMethodId)
            .WithErrorMessage(StripePaymentMethodIdentifier.ValidationMessage);
    }
}

#endregion

#region AddFundsCommand Validator Tests

public class AddFundsCommandValidatorTests
{
    private readonly AddFundsCommandValidator _validator = new();

    [Fact]
    public void Valid_ShouldPass()
    {
        var cmd = new AddFundsCommand(Guid.NewGuid(), 100m, "Test deposit", null);
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyUserId_ShouldFail()
    {
        var cmd = new AddFundsCommand(Guid.Empty, 100m, "Test deposit", null);
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void ZeroAmount_ShouldFail()
    {
        var cmd = new AddFundsCommand(Guid.NewGuid(), 0m, "Test deposit", null);
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void AmountExceedsMax_ShouldFail()
    {
        var cmd = new AddFundsCommand(Guid.NewGuid(), 100001m, "Test deposit", null);
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void EmptyDescription_ShouldFail()
    {
        var cmd = new AddFundsCommand(Guid.NewGuid(), 100m, "", null);
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void TooLongDescription_ShouldFail()
    {
        var cmd = new AddFundsCommand(Guid.NewGuid(), 100m, new string('x', 501), null);
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void TooLongReferenceId_ShouldFail()
    {
        var cmd = new AddFundsCommand(Guid.NewGuid(), 100m, "Test deposit", new string('x', 101));
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.ReferenceId);
    }

    [Fact]
    public void NullReferenceId_ShouldPass()
    {
        var cmd = new AddFundsCommand(Guid.NewGuid(), 100m, "Test deposit", null);
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveValidationErrorFor(x => x.ReferenceId);
    }
}

#endregion

#region DeductFundsCommand Validator Tests

public class DeductFundsCommandValidatorTests
{
    private readonly DeductFundsCommandValidator _validator = new();

    [Fact]
    public void Valid_ShouldPass()
    {
        var cmd = new DeductFundsCommand(Guid.NewGuid(), 50m, "Purchase", null);
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyUserId_ShouldFail()
    {
        var cmd = new DeductFundsCommand(Guid.Empty, 50m, "Purchase", null);
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void ZeroAmount_ShouldFail()
    {
        var cmd = new DeductFundsCommand(Guid.NewGuid(), 0m, "Purchase", null);
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void EmptyDescription_ShouldFail()
    {
        var cmd = new DeductFundsCommand(Guid.NewGuid(), 50m, "", null);
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void TooLongDescription_ShouldFail()
    {
        var cmd = new DeductFundsCommand(Guid.NewGuid(), 50m, new string('x', 501), null);
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void TooLongReferenceId_ShouldFail()
    {
        var cmd = new DeductFundsCommand(Guid.NewGuid(), 50m, "Purchase", new string('x', 101));
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.ReferenceId);
    }
}

#endregion

#region CreateWalletCommand Validator Tests

public class CreateWalletCommandValidatorTests
{
    private readonly CreateWalletCommandValidator _validator = new();

    [Fact]
    public void Valid_ShouldPass()
    {
        var cmd = new CreateWalletCommand(Guid.NewGuid(), "USD");
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyUserId_ShouldFail()
    {
        var cmd = new CreateWalletCommand(Guid.Empty, "USD");
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void EmptyCurrency_ShouldFail()
    {
        var cmd = new CreateWalletCommand(Guid.NewGuid(), "");
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Currency);
    }

    [Fact]
    public void WrongLengthCurrency_ShouldFail()
    {
        var cmd = new CreateWalletCommand(Guid.NewGuid(), "US");
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Currency);
    }

    [Fact]
    public void TooLongCurrency_ShouldFail()
    {
        var cmd = new CreateWalletCommand(Guid.NewGuid(), "USDX");
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Currency);
    }
}

#endregion

#region LockWalletCommand Validator Tests

public class LockWalletCommandValidatorTests
{
    private readonly LockWalletCommandValidator _validator = new();

    [Fact]
    public void Valid_ShouldPass()
    {
        var cmd = new LockWalletCommand(Guid.NewGuid(), "Fraud investigation");
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyUserId_ShouldFail()
    {
        var cmd = new LockWalletCommand(Guid.Empty, "Fraud investigation");
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void EmptyReason_ShouldFail()
    {
        var cmd = new LockWalletCommand(Guid.NewGuid(), "");
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Reason);
    }

    [Fact]
    public void TooLongReason_ShouldFail()
    {
        var cmd = new LockWalletCommand(Guid.NewGuid(), new string('x', 501));
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Reason);
    }
}

#endregion

#region TransferFundsCommand Validator Tests

public class TransferFundsCommandValidatorTests
{
    private readonly TransferFundsCommandValidator _validator = new();

    [Fact]
    public void Valid_ShouldPass()
    {
        var cmd = new TransferFundsCommand(Guid.NewGuid(), Guid.NewGuid(), 100m, "Transfer", null);
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyFromUserId_ShouldFail()
    {
        var cmd = new TransferFundsCommand(Guid.Empty, Guid.NewGuid(), 100m, "Transfer", null);
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.FromUserId);
    }

    [Fact]
    public void EmptyToUserId_ShouldFail()
    {
        var cmd = new TransferFundsCommand(Guid.NewGuid(), Guid.Empty, 100m, "Transfer", null);
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.ToUserId);
    }

    [Fact]
    public void SameFromAndToUser_ShouldFail()
    {
        var userId = Guid.NewGuid();
        var cmd = new TransferFundsCommand(userId, userId, 100m, "Transfer", null);
        var result = _validator.TestValidate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Cannot transfer funds to the same user");
    }

    [Fact]
    public void ZeroAmount_ShouldFail()
    {
        var cmd = new TransferFundsCommand(Guid.NewGuid(), Guid.NewGuid(), 0m, "Transfer", null);
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void AmountExceedsMax_ShouldFail()
    {
        var cmd = new TransferFundsCommand(Guid.NewGuid(), Guid.NewGuid(), 50001m, "Transfer", null);
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void EmptyDescription_ShouldFail()
    {
        var cmd = new TransferFundsCommand(Guid.NewGuid(), Guid.NewGuid(), 100m, "", null);
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void TooLongDescription_ShouldFail()
    {
        var cmd = new TransferFundsCommand(Guid.NewGuid(), Guid.NewGuid(), 100m, new string('x', 501), null);
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void TooLongReferenceId_ShouldFail()
    {
        var cmd = new TransferFundsCommand(Guid.NewGuid(), Guid.NewGuid(), 100m, "Transfer", new string('x', 101));
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.ReferenceId);
    }
}

#endregion

#region SimulatedPaymentResultFactory Tests

public class SimulatedPaymentResultFactoryTests
{
    [Fact]
    public void PaymentSuccess_ShouldReturnSuccessResult()
    {
        var result = SimulatedPaymentResultFactory.PaymentSuccess();

        result.Success.Should().BeTrue();
        result.TransactionId.Should().StartWith("pi_");
        result.ExternalPaymentId.Should().StartWith("ch_");
        result.ErrorCode.Should().BeNull();
        result.ErrorMessage.Should().BeNull();
        result.Status.Should().Be(PaymentStatus.Succeeded);
        result.ProcessedAt.Should().BeAfter(DateTime.MinValue);
    }

    [Fact]
    public void PaymentSuccess_WithLogger_ShouldReturnSuccessResult()
    {
        var result = SimulatedPaymentResultFactory.PaymentSuccess(NullLogger.Instance);

        result.Success.Should().BeTrue();
        result.TransactionId.Should().StartWith("pi_");
    }

    [Fact]
    public void PaymentFailure_ShouldReturnFailureResult()
    {
        var result = SimulatedPaymentResultFactory.PaymentFailure("Card declined", "card_declined");

        result.Success.Should().BeFalse();
        result.TransactionId.Should().BeNull();
        result.ExternalPaymentId.Should().BeNull();
        result.ErrorCode.Should().Be("card_declined");
        result.ErrorMessage.Should().Be("Card declined");
        result.Status.Should().Be(PaymentStatus.Failed);
    }

    [Fact]
    public void PaymentFailure_DefaultErrorCode_ShouldBeStripeError()
    {
        var result = SimulatedPaymentResultFactory.PaymentFailure("error");
        result.ErrorCode.Should().Be("stripe_error");
    }

    [Fact]
    public void RefundSuccess_ShouldReturnSuccessResult()
    {
        var result = SimulatedPaymentResultFactory.RefundSuccess(50m);

        result.Success.Should().BeTrue();
        result.RefundId.Should().StartWith("re_");
        result.AmountRefunded.Should().Be(50m);
        result.ErrorCode.Should().BeNull();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void RefundSuccess_WithLogger_ShouldReturnSuccessResult()
    {
        var result = SimulatedPaymentResultFactory.RefundSuccess(50m, NullLogger.Instance);

        result.Success.Should().BeTrue();
        result.RefundId.Should().StartWith("re_");
    }

    [Fact]
    public void RefundFailure_ShouldReturnFailureResult()
    {
        var result = SimulatedPaymentResultFactory.RefundFailure("Not found");

        result.Success.Should().BeFalse();
        result.RefundId.Should().BeNull();
        result.AmountRefunded.Should().Be(0);
        result.ErrorMessage.Should().Be("Not found");
    }

    [Fact]
    public void CustomerSuccess_ShouldReturnSuccessResult()
    {
        var result = SimulatedPaymentResultFactory.CustomerSuccess();

        result.Success.Should().BeTrue();
        result.ExternalCustomerId.Should().StartWith("cus_");
        result.ErrorCode.Should().BeNull();
    }

    [Fact]
    public void CustomerSuccess_WithLogger_ShouldReturnSuccessResult()
    {
        var result = SimulatedPaymentResultFactory.CustomerSuccess(NullLogger.Instance);

        result.Success.Should().BeTrue();
        result.ExternalCustomerId.Should().StartWith("cus_");
    }

    [Fact]
    public void CustomerFailure_ShouldReturnFailureResult()
    {
        var result = SimulatedPaymentResultFactory.CustomerFailure("Error");

        result.Success.Should().BeFalse();
        result.ExternalCustomerId.Should().BeNull();
        result.ErrorMessage.Should().Be("Error");
    }

    [Fact]
    public void PaymentMethodSuccess_DefaultCard_ShouldReturnVisa4242()
    {
        var result = SimulatedPaymentResultFactory.PaymentMethodSuccess();

        result.Success.Should().BeTrue();
        result.ExternalPaymentMethodId.Should().StartWith("pm_");
        result.CardLast4.Should().Be("4242");
        result.CardBrand.Should().Be("visa");
    }

    [Fact]
    public void PaymentMethodSuccess_CustomCard_ShouldUseProvided()
    {
        var result = SimulatedPaymentResultFactory.PaymentMethodSuccess(SimulatedTestCard.Mastercard5555);

        result.CardLast4.Should().Be("5555");
        result.CardBrand.Should().Be("mastercard");
    }

    [Fact]
    public void PaymentMethodSuccess_WithLogger_ShouldReturnSuccessResult()
    {
        var result = SimulatedPaymentResultFactory.PaymentMethodSuccess(SimulatedTestCard.Visa4242, NullLogger.Instance);

        result.Success.Should().BeTrue();
        result.ExternalPaymentMethodId.Should().StartWith("pm_");
    }

    [Fact]
    public void PaymentMethodSuccess_AmexCard_ShouldReturnCorrectDetails()
    {
        var result = SimulatedPaymentResultFactory.PaymentMethodSuccess(SimulatedTestCard.Amex8431);

        result.CardLast4.Should().Be("8431");
        result.CardBrand.Should().Be("amex");
    }

    [Fact]
    public void PaymentMethodFailure_ShouldReturnFailureResult()
    {
        var result = SimulatedPaymentResultFactory.PaymentMethodFailure("Invalid card");

        result.Success.Should().BeFalse();
        result.ExternalPaymentMethodId.Should().BeNull();
        result.CardLast4.Should().BeNull();
        result.CardBrand.Should().BeNull();
    }

    [Fact]
    public void SetupIntentSuccess_ShouldReturnSuccessResult()
    {
        var result = SimulatedPaymentResultFactory.SetupIntentSuccess("cus_123", NullLogger.Instance);

        result.Success.Should().BeTrue();
        result.ExternalSetupIntentId.Should().StartWith("seti_");
        result.ClientSecret.Should().Contain("_secret_");
        result.CustomerId.Should().Be("cus_123");
    }

    [Fact]
    public void SetupIntentFailure_ShouldReturnFailureResult()
    {
        var result = SimulatedPaymentResultFactory.SetupIntentFailure("setup failed", "setup_error");

        result.Success.Should().BeFalse();
        result.ExternalSetupIntentId.Should().BeNull();
        result.ClientSecret.Should().BeNull();
        result.CustomerId.Should().BeNull();
        result.ErrorCode.Should().Be("setup_error");
        result.ErrorMessage.Should().Be("setup failed");
    }

    [Fact]
    public void DefaultPaymentMethodSuccess_ShouldReturnSuccessResult()
    {
        var result = SimulatedPaymentResultFactory.DefaultPaymentMethodSuccess();

        result.Success.Should().BeTrue();
        result.ErrorCode.Should().BeNull();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void DefaultPaymentMethodFailure_ShouldReturnFailureResult()
    {
        var result = SimulatedPaymentResultFactory.DefaultPaymentMethodFailure("default update failed", "default_error");

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("default_error");
        result.ErrorMessage.Should().Be("default update failed");
    }

    [Fact]
    public void CancellationSuccess_ShouldReturnSuccessResult()
    {
        var result = SimulatedPaymentResultFactory.CancellationSuccess();

        result.Success.Should().BeTrue();
        result.ErrorCode.Should().BeNull();
        result.ErrorMessage.Should().BeNull();
        result.EffectiveDate.Should().NotBeNull();
    }

    [Fact]
    public void CancellationSuccess_WithCustomDate_ShouldUseProvided()
    {
        var date = new DateTime(2025, 6, 15);
        var result = SimulatedPaymentResultFactory.CancellationSuccess(date);

        result.EffectiveDate.Should().Be(date);
    }

    [Fact]
    public void CancellationSuccess_WithLogger_ShouldReturnSuccessResult()
    {
        var result = SimulatedPaymentResultFactory.CancellationSuccess(logger: NullLogger.Instance);

        result.Success.Should().BeTrue();
        result.EffectiveDate.Should().NotBeNull();
    }

    [Fact]
    public void CancellationFailure_ShouldReturnFailureResult()
    {
        var result = SimulatedPaymentResultFactory.CancellationFailure("Subscription not found");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Subscription not found");
        result.EffectiveDate.Should().BeNull();
    }
}

#endregion

#region SimulatedTestCard Tests

public class SimulatedTestCardTests
{
    [Fact]
    public void Visa4242_ShouldHaveCorrectProperties()
    {
        var card = SimulatedTestCard.Visa4242;
        card.Last4.Should().Be("4242");
        card.Brand.Should().Be("visa");
        card.ExpiryMonth.Should().Be(12);
    }

    [Fact]
    public void Mastercard5555_ShouldHaveCorrectProperties()
    {
        var card = SimulatedTestCard.Mastercard5555;
        card.Last4.Should().Be("5555");
        card.Brand.Should().Be("mastercard");
        card.ExpiryMonth.Should().Be(10);
    }

    [Fact]
    public void Amex8431_ShouldHaveCorrectProperties()
    {
        var card = SimulatedTestCard.Amex8431;
        card.Last4.Should().Be("8431");
        card.Brand.Should().Be("amex");
        card.ExpiryMonth.Should().Be(6);
    }

    [Fact]
    public void CustomCard_ShouldSetProperties()
    {
        var card = new SimulatedTestCard("1234", "discover", 3, 2028);
        card.Last4.Should().Be("1234");
        card.Brand.Should().Be("discover");
        card.ExpiryMonth.Should().Be(3);
        card.ExpiryYear.Should().Be(2028);
    }
}

#endregion

#region Request/Response Record Tests

public class AddFundsRequestTests
{
    [Fact]
    public void ShouldBeCreatable()
    {
        var r = new AddFundsRequest { UserId = Guid.NewGuid(), Amount = 100m, Description = "test", ReferenceId = "ref" };
        r.Amount.Should().Be(100m);
        r.Description.Should().Be("test");
        r.ReferenceId.Should().Be("ref");
    }
}

public class DeductFundsRequestTests
{
    [Fact]
    public void ShouldBeCreatable()
    {
        var r = new DeductFundsRequest { UserId = Guid.NewGuid(), Amount = 50m, Description = "purchase", ReferenceId = "ref" };
        r.Amount.Should().Be(50m);
    }
}

public class CreateWalletRequestTests
{
    [Fact]
    public void ShouldBeCreatable()
    {
        var r = new CreateWalletRequest { UserId = Guid.NewGuid(), Currency = "EUR" };
        r.Currency.Should().Be("EUR");
    }

    [Fact]
    public void Currency_ShouldBeOptional()
    {
        var r = new CreateWalletRequest { UserId = Guid.NewGuid() };
        r.Currency.Should().BeNull();
    }
}

public class TransferFundsRequestTests
{
    [Fact]
    public void ShouldBeCreatable()
    {
        var r = new TransferFundsRequest
        {
            FromUserId = Guid.NewGuid(),
            ToUserId = Guid.NewGuid(),
            Amount = 25m,
            Description = "gift",
            ReferenceId = "ref"
        };

        r.Amount.Should().Be(25m);
        r.Description.Should().Be("gift");
    }
}

public class TransferFundsResponseTests
{
    [Fact]
    public void ShouldBeCreatable()
    {
        var debit = new WalletTransaction();
        var credit = new WalletTransaction();
        var r = new TransferFundsResponse { DebitTransaction = debit, CreditTransaction = credit };

        r.DebitTransaction.Should().Be(debit);
        r.CreditTransaction.Should().Be(credit);
    }
}

public class TransferResultTests
{
    [Fact]
    public void ShouldBeCreatable()
    {
        var debit = new WalletTransaction();
        var credit = new WalletTransaction();
        var r = new TransferResult(debit, credit);

        r.DebitTransaction.Should().Be(debit);
        r.CreditTransaction.Should().Be(credit);
    }
}

#endregion
