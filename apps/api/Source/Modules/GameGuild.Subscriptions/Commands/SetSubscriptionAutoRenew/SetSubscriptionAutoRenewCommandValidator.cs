using FluentValidation;

namespace GameGuild.Subscriptions.Commands;

/// <summary>
///     Validator for SetSubscriptionAutoRenewCommand
/// </summary>
public class SetSubscriptionAutoRenewCommandValidator : AbstractValidator<SetSubscriptionAutoRenewCommand>
{
    public SetSubscriptionAutoRenewCommandValidator() { RuleFor(x => x.SubscriptionId).NotEmpty().WithMessage("SubscriptionId is required"); }
}
