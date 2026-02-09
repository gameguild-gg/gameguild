using FluentValidation;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Validator for SearchTenantsQuery
/// </summary>
public sealed class SearchTenantsQueryValidator : AbstractValidator<SearchTenantsQuery>
{
    public SearchTenantsQueryValidator()
    {
        RuleFor(x => x.MaxResponses).GreaterThan(0).LessThanOrEqualTo(1000).When(x => x.MaxResponses.HasValue).WithMessage("Max Responses must be between 1 and 1000");

        RuleFor(x => x.CreatedAfter).LessThan(x => x.CreatedBefore).When(x => x.CreatedAfter.HasValue && x.CreatedBefore.HasValue).WithMessage("CreatedAfter must be before CreatedBefore");

        RuleFor(x => x.AdminEmail).EmailAddress().When(x => !string.IsNullOrEmpty(x.AdminEmail)).WithMessage("Invalid email format");
    }
}
