using FluentValidation;
using GameGuild.Modules.Tenants.Commands;

namespace GameGuild.Modules.Tenants.Validators;

/// <summary>
/// Validator for SearchTenantsCommand
/// </summary>
public class SearchTenantsCommandValidator : AbstractValidator<SearchTenantsCommand>
{
    public SearchTenantsCommandValidator()
    {
        RuleFor(x => x.SearchTerm)
            .MaximumLength(200)
            .WithMessage("Search term cannot exceed 200 characters")
            .When(x => !string.IsNullOrEmpty(x.SearchTerm));

        RuleFor(x => x.Limit)
            .GreaterThan(0)
            .WithMessage("Limit must be greater than 0")
            .LessThanOrEqualTo(1000)
            .WithMessage("Limit cannot exceed 1000")
            .When(x => x.Limit.HasValue);

        RuleFor(x => x.Offset)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Offset must be greater than or equal to 0")
            .When(x => x.Offset.HasValue);
    }
}
