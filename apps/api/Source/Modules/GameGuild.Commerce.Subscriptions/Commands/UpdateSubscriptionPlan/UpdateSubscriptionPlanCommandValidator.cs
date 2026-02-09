using FluentValidation;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Validator for UpdateSubscriptionPlanCommand
/// </summary>
public sealed class UpdateSubscriptionPlanCommandValidator : AbstractValidator<UpdateSubscriptionPlanCommand>
{
    public UpdateSubscriptionPlanCommandValidator() { RuleFor(x => x.Id).NotEmpty().WithMessage("Plan ID is required."); }
}
