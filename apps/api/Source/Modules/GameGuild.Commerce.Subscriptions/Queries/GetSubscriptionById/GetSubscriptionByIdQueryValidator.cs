using FluentValidation;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Validator for GetSubscriptionByIdQuery
/// </summary>
public class GetSubscriptionByIdQueryValidator : AbstractValidator<GetSubscriptionByIdQuery>
{
    public GetSubscriptionByIdQueryValidator() { RuleFor(x => x.SubscriptionId).NotEmpty().WithMessage("SubscriptionId is required"); }
}
