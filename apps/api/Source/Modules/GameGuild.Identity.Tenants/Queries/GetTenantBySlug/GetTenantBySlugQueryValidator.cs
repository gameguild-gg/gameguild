using FluentValidation;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Validator for GetTenantBySlugQuery
/// </summary>
public class GetTenantBySlugQueryValidator : AbstractValidator<GetTenantBySlugQuery>
{
    public GetTenantBySlugQueryValidator() { RuleFor(x => x.Slug).NotEmpty().WithMessage("Tenant slug is required").MaximumLength(100).WithMessage("Tenant slug cannot exceed 100 characters"); }
}
