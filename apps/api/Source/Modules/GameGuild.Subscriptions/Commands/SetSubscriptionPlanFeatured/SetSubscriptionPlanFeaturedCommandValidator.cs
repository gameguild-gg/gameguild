using FluentValidation;

namespace GameGuild.Subscriptions.Commands;

/// <summary>
///     Validator for SetSubscriptionPlanFeaturedCommand
/// </summary>
public class SetSubscriptionPlanFeaturedCommandValidator : AbstractValidator<SetSubscriptionPlanFeaturedCommand>
{
    public SetSubscriptionPlanFeaturedCommandValidator() { RuleFor(x => x.Id).NotEmpty().WithMessage("Id is required"); }
}
