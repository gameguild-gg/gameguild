using FluentValidation;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Validator for UpdateSubscriptionPlanLimitsCommand
/// </summary>
public class UpdateSubscriptionPlanLimitsCommandValidator : AbstractValidator<UpdateSubscriptionPlanLimitsCommand>
{
    public UpdateSubscriptionPlanLimitsCommandValidator() { RuleFor(x => x.Id).NotEmpty().WithMessage("Id is required"); }
}
