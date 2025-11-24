using FluentValidation;

namespace GameGuild.Subscriptions.Commands;

/// <summary>
///     Validator for CancelSubscriptionCommand
/// </summary>
public class CancelSubscriptionCommandValidator : AbstractValidator<CancelSubscriptionCommand>
{
    public CancelSubscriptionCommandValidator() { RuleFor(x => x.SubscriptionId).NotEmpty().WithMessage("SubscriptionId is required"); }
}
