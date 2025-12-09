using FluentValidation;
using GameGuild.Modules.Programs.Queries;

namespace GameGuild.Modules.Programs.Validators;

/// <summary> Validator for SearchProgramsQuery </summary>
public class SearchProgramsQueryValidator : AbstractValidator<SearchProgramsQuery> {
    public SearchProgramsQueryValidator() {
        RuleFor(x => x.SearchTerm)
            .NotEmpty().WithMessage("Search term is required")
            .Length(2, 100).WithMessage("Search term must be between 2 and 100 characters");

        RuleFor(x => x.Category)
            .IsInEnum().WithMessage("Invalid program category")
            .When(x => x.Category.HasValue);

        RuleFor(x => x.Difficulty)
            .IsInEnum().WithMessage("Invalid program difficulty")
            .When(x => x.Difficulty.HasValue);

        RuleFor(x => x.MinEstimatedHours)
            .GreaterThan(0).WithMessage("Minimum estimated hours must be greater than 0")
            .When(x => x.MinEstimatedHours.HasValue);

        RuleFor(x => x.MaxEstimatedHours)
            .GreaterThan(0).WithMessage("Maximum estimated hours must be greater than 0")
            .LessThanOrEqualTo(1000).WithMessage("Maximum estimated hours cannot exceed 1000")
            .When(x => x.MaxEstimatedHours.HasValue);

        RuleFor(x => x.MinRating)
            .InclusiveBetween(1, 5).WithMessage("Minimum rating must be between 1 and 5")
            .When(x => x.MinRating.HasValue);

        // Ensure min/max ranges are logical
        RuleFor(x => x)
            .Must(x => !x.MinEstimatedHours.HasValue || !x.MaxEstimatedHours.HasValue ||
                       x.MinEstimatedHours <= x.MaxEstimatedHours)
            .WithMessage("Minimum estimated hours cannot be greater than maximum estimated hours");
    }
}