using FluentValidation;

namespace GameGuild.Tenants.Queries;

/// <summary>
///     Validator for GetTenantMembersQuery
/// </summary>
public class GetTenantMembersQueryValidator : AbstractValidator<GetTenantMembersQuery>
{
    public GetTenantMembersQueryValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty().WithMessage("Tenant ID is required");

        RuleFor(x => x.PageNumber).GreaterThan(0).WithMessage("Page number must be greater than 0");

        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100");
    }
}
