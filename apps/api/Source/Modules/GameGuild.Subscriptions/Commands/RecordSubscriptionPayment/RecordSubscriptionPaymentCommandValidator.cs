using FluentValidation;

namespace GameGuild.Subscriptions.Commands;

/// <summary>
///     Validator for RecordSubscriptionPaymentCommand
/// </summary>
public class RecordSubscriptionPaymentCommandValidator : AbstractValidator<RecordSubscriptionPaymentCommand>
{
    public RecordSubscriptionPaymentCommandValidator()
    {
        RuleFor(x => x.SubscriptionId).NotEmpty().WithMessage("SubscriptionId is required");

        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Amount must be greater than 0");

        RuleFor(x => x.Currency).NotEmpty().WithMessage("Currency is required").Length(3).WithMessage("Currency must be exactly 3 characters (ISO 4217)");

        RuleFor(x => x.PaymentDate).LessThanOrEqualTo(DateTime.UtcNow).WithMessage("PaymentDate cannot be in the future");
    }
}
