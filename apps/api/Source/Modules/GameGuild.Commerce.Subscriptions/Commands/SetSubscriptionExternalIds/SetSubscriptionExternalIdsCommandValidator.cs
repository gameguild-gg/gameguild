using FluentValidation;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Validator for SetSubscriptionExternalIdsCommand
/// </summary>
public class SetSubscriptionExternalIdsCommandValidator : AbstractValidator<SetSubscriptionExternalIdsCommand>
{
    public SetSubscriptionExternalIdsCommandValidator()
    {
        RuleFor(x => x.SubscriptionId).NotEmpty().WithMessage("SubscriptionId is required");

        RuleFor(x => x.StripeSubscriptionId).MaximumLength(255).When(x => !string.IsNullOrEmpty(x.StripeSubscriptionId)).WithMessage("StripeSubscriptionId cannot exceed 255 characters");

        RuleFor(x => x.PayPalSubscriptionId).MaximumLength(255).When(x => !string.IsNullOrEmpty(x.PayPalSubscriptionId)).WithMessage("PayPalSubscriptionId cannot exceed 255 characters");

        RuleFor(x => x).Must(x => !string.IsNullOrEmpty(x.StripeSubscriptionId) || !string.IsNullOrEmpty(x.PayPalSubscriptionId)).WithMessage("At least one external subscription ID must be provided");
    }
}
