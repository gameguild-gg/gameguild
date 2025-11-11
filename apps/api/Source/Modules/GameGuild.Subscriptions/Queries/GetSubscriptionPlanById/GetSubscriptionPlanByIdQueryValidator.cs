using FluentValidation;

namespace GameGuild.Subscriptions.Queries;

/// <summary>
///     Validator for GetSubscriptionPlanByIdQuery
/// </summary>
public class GetSubscriptionPlanByIdQueryValidator : AbstractValidator<GetSubscriptionPlanByIdQuery>
{
    public GetSubscriptionPlanByIdQueryValidator() { RuleFor(x => x.Id).NotEmpty().WithMessage("Id is required"); }
}
