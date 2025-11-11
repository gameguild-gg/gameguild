using FluentValidation;

namespace GameGuild.Tenants.Queries;

/// <summary>
///     Validator for GetTenantByIdQuery
/// </summary>
public class GetTenantByIdQueryValidator : AbstractValidator<GetTenantByIdQuery>
{
    public GetTenantByIdQueryValidator() { RuleFor(x => x.TenantId).NotEmpty().WithMessage("Tenant ID is required"); }
}
