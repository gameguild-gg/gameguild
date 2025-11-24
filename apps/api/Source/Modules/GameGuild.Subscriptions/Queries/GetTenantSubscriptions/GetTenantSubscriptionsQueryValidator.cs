using FluentValidation;

namespace GameGuild.Subscriptions.Queries;

/// <summary>
///     Validator for GetTenantSubscriptionsQuery
/// </summary>
public class GetTenantSubscriptionsQueryValidator : AbstractValidator<GetTenantSubscriptionsQuery>
{
    public GetTenantSubscriptionsQueryValidator() { RuleFor(x => x.TenantId).NotEmpty().WithMessage("TenantId is required"); }
}
