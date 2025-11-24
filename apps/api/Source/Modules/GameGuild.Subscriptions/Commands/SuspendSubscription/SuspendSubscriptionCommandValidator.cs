using FluentValidation;

namespace GameGuild.Subscriptions.Commands;

/// <summary>
///     Validator for SuspendSubscriptionCommand
/// </summary>
public class SuspendSubscriptionCommandValidator : AbstractValidator<SuspendSubscriptionCommand>
{
    public SuspendSubscriptionCommandValidator() { RuleFor(x => x.SubscriptionId).NotEmpty().WithMessage("SubscriptionId is required"); }
}
