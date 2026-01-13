using FluentValidation;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Validator for GetTenantSubscriptionsQuery
/// </summary>
public class GetTenantSubscriptionsQueryValidator : AbstractValidator<GetTenantSubscriptionsQuery>
{
    public GetTenantSubscriptionsQueryValidator() { RuleFor(x => x.TenantId).NotEmpty().WithMessage("TenantId is required"); }
}
