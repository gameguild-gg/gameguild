using FluentValidation;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Validator for SetSubscriptionPlanFeaturedCommand
/// </summary>
public sealed class SetSubscriptionPlanFeaturedCommandValidator : AbstractValidator<SetSubscriptionPlanFeaturedCommand>
{
    public SetSubscriptionPlanFeaturedCommandValidator() { RuleFor(x => x.Id).NotEmpty().WithMessage("Id is required"); }
}
