using FluentValidation;

namespace GameGuild.Subscriptions.Commands;

/// <summary>
///     Validator for ReactivateSubscriptionCommand
/// </summary>
public class ReactivateSubscriptionCommandValidator : AbstractValidator<ReactivateSubscriptionCommand>
{
    public ReactivateSubscriptionCommandValidator() { RuleFor(x => x.SubscriptionId).NotEmpty().WithMessage("SubscriptionId is required"); }
}
