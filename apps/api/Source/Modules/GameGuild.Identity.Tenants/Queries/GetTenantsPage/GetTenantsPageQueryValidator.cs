using FluentValidation;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Validator for GetTenantsPageQuery
/// </summary>
public sealed class GetTenantsPageQueryValidator : AbstractValidator<GetTenantsPageQuery>
{
    public GetTenantsPageQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0).WithMessage("Page must be greater than 0");

        RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(500).WithMessage("Page size must be between 1 and 500");

        RuleFor(x => x.SortBy).Must(x => string.IsNullOrEmpty(x) || new[ ] { "Name", "Slug", "AdminEmail", "CreatedAt", "UpdatedAt", "IsActive" }.Contains(x)).WithMessage("Invalid sort field");
    }
}
