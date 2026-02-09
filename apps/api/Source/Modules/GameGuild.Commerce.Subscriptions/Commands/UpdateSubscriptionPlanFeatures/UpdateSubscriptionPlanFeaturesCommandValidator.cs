using FluentValidation;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Validator for UpdateSubscriptionPlanFeaturesCommand
/// </summary>
public sealed class UpdateSubscriptionPlanFeaturesCommandValidator : AbstractValidator<UpdateSubscriptionPlanFeaturesCommand>
{
    public UpdateSubscriptionPlanFeaturesCommandValidator() { RuleFor(x => x.Id).NotEmpty().WithMessage("Id is required"); }
}
