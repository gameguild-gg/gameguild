using FluentValidation;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Validator for GetActiveTenantSubscriptionQuery
/// </summary>
public class GetActiveTenantSubscriptionQueryValidator : AbstractValidator<GetActiveTenantSubscriptionQuery>
{
    public GetActiveTenantSubscriptionQueryValidator() { RuleFor(x => x.TenantId).NotEmpty().WithMessage("TenantId is required"); }
}
