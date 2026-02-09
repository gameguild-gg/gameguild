using FluentValidation;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Validator for GetTenantByIdQuery
/// </summary>
public sealed class GetTenantByIdQueryValidator : AbstractValidator<GetTenantByIdQuery>
{
    public GetTenantByIdQueryValidator() { RuleFor(x => x.TenantId).NotEmpty().WithMessage("Tenant ID is required"); }
}
