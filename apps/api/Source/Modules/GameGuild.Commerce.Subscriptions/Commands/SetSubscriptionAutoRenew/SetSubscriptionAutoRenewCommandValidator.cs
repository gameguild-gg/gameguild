using FluentValidation;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Validator for SetSubscriptionAutoRenewCommand
/// </summary>
public sealed class SetSubscriptionAutoRenewCommandValidator : AbstractValidator<SetSubscriptionAutoRenewCommand>
{
    public SetSubscriptionAutoRenewCommandValidator() { RuleFor(x => x.SubscriptionId).NotEmpty().WithMessage("SubscriptionId is required"); }
}
