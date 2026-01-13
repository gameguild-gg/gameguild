using FluentValidation;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Validator for RecordSubscriptionPaymentFailureCommand
/// </summary>
public class RecordSubscriptionPaymentFailureCommandValidator : AbstractValidator<RecordSubscriptionPaymentFailureCommand>
{
    public RecordSubscriptionPaymentFailureCommandValidator()
    {
        RuleFor(x => x.SubscriptionId).NotEmpty().WithMessage("SubscriptionId is required");

        RuleFor(x => x.Reason).NotEmpty().WithMessage("Reason is required").MaximumLength(500).WithMessage("Reason cannot exceed 500 characters");

        RuleFor(x => x.FailureDate).LessThanOrEqualTo(DateTime.UtcNow).WithMessage("FailureDate cannot be in the future");
    }
}
