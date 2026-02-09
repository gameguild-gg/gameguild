using FluentValidation;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Validator for GetProductsPagedQuery
/// </summary>
public sealed class GetProductsPagedQueryValidator : AbstractValidator<GetProductsPagedQuery>
{
    private static readonly string[] AllowedSortFields = { "name", "createdat", "updatedat" };
    private static readonly string[] AllowedSortDirections = { "asc", "desc" };

    public GetProductsPagedQueryValidator()
    {
        RuleFor(x => x.Skip)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Skip must be non-negative.");

        RuleFor(x => x.Take)
            .InclusiveBetween(1, 100)
            .WithMessage("Take must be between 1 and 100.");

        RuleFor(x => x.SortBy)
            .Must(x => AllowedSortFields.Contains(x.ToLowerInvariant()))
            .WithMessage($"SortBy must be one of: {string.Join(", ", AllowedSortFields)}")
            .When(x => !string.IsNullOrWhiteSpace(x.SortBy));

        RuleFor(x => x.SortDirection)
            .Must(x => AllowedSortDirections.Contains(x.ToLowerInvariant()))
            .WithMessage($"SortDirection must be one of: {string.Join(", ", AllowedSortDirections)}")
            .When(x => !string.IsNullOrWhiteSpace(x.SortDirection));

        RuleFor(x => x.SearchTerm)
            .MaximumLength(200)
            .WithMessage("Search term cannot exceed 200 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.SearchTerm));
    }
}
