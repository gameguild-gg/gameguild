using FluentValidation;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Validator for ReactivateSubscriptionCommand
/// </summary>
public sealed class ReactivateSubscriptionCommandValidator : AbstractValidator<ReactivateSubscriptionCommand>
{
    public ReactivateSubscriptionCommandValidator() { RuleFor(x => x.SubscriptionId).NotEmpty().WithMessage("SubscriptionId is required"); }
}
