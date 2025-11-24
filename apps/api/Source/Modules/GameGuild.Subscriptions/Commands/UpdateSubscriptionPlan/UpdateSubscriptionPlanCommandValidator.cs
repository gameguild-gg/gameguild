using FluentValidation;

namespace GameGuild.Subscriptions.Commands;

/// <summary>
///     Validator for UpdateSubscriptionPlanCommand
/// </summary>
public class UpdateSubscriptionPlanCommandValidator : AbstractValidator<UpdateSubscriptionPlanCommand>
{
    public UpdateSubscriptionPlanCommandValidator() { RuleFor(x => x.Id).NotEmpty().WithMessage("Plan ID is required."); }
}
