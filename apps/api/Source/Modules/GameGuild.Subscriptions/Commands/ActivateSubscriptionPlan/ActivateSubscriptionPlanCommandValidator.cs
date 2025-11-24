using FluentValidation;

namespace GameGuild.Subscriptions.Commands;

/// <summary>
///     Validator for ActivateSubscriptionPlanCommand
/// </summary>
public class ActivateSubscriptionPlanCommandValidator : AbstractValidator<ActivateSubscriptionPlanCommand>
{
    public ActivateSubscriptionPlanCommandValidator() { RuleFor(x => x.Id).NotEmpty().WithMessage("Id is required"); }
}
