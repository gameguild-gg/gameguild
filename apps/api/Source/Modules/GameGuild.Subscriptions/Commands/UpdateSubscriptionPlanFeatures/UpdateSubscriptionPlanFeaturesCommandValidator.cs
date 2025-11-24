using FluentValidation;

namespace GameGuild.Subscriptions.Commands;

/// <summary>
///     Validator for UpdateSubscriptionPlanFeaturesCommand
/// </summary>
public class UpdateSubscriptionPlanFeaturesCommandValidator : AbstractValidator<UpdateSubscriptionPlanFeaturesCommand>
{
    public UpdateSubscriptionPlanFeaturesCommandValidator() { RuleFor(x => x.Id).NotEmpty().WithMessage("Id is required"); }
}
