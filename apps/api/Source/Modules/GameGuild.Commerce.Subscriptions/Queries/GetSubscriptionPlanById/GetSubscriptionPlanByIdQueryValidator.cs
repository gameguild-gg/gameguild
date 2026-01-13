using FluentValidation;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Validator for GetSubscriptionPlanByIdQuery
/// </summary>
public class GetSubscriptionPlanByIdQueryValidator : AbstractValidator<GetSubscriptionPlanByIdQuery>
{
    public GetSubscriptionPlanByIdQueryValidator() { RuleFor(x => x.Id).NotEmpty().WithMessage("Id is required"); }
}
