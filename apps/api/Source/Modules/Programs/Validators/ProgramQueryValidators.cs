using FluentValidation;
using GameGuild.Modules.Contents;
using GameGuild.Modules.Programs;
using GameGuild.Source.Modules.Programs.Queries;
using ProgramAvailabilityStatus = GameGuild.Source.Modules.Programs.Models.EnrollmentStatus;

namespace GameGuild.Modules.Programs.Validators;

/// <summary>
/// FluentValidation validators for Program CQRS queries
/// </summary>

// ===== BASIC QUERY VALIDATORS =====

/// <summary> Validator for GetAllProgramsQuery </summary>
public class GetAllProgramsQueryValidator : AbstractValidator<GetAllProgramsQuery> {
    public GetAllProgramsQueryValidator() {
        RuleFor(x => x.Skip)
          .GreaterThanOrEqualTo(0).WithMessage("Skip must be greater than or equal to 0");

        RuleFor(x => x.Take)
          .InclusiveBetween(1, 100).WithMessage("Take must be between 1 and 100");

        RuleFor(x => x.Search)
          .Length(2, 100).WithMessage("Search term must be between 2 and 100 characters")
          .When(x => !string.IsNullOrEmpty(x.Search));

        RuleFor(x => x.Category)
          .IsInEnum().WithMessage("Invalid program category")
          .When(x => x.Category.HasValue);

        RuleFor(x => x.Difficulty)
          .IsInEnum().WithMessage("Invalid program difficulty")
          .When(x => x.Difficulty.HasValue);

        RuleFor(x => x.Status)
          .IsInEnum().WithMessage("Invalid content status")
          .When(x => x.Status.HasValue);

        RuleFor(x => x.Visibility)
          .IsInEnum().WithMessage("Invalid access level")
          .When(x => x.Visibility.HasValue);

        RuleFor(x => x.EnrollmentStatus)
          .IsInEnum().WithMessage("Invalid enrollment status")
          .When(x => x.EnrollmentStatus.HasValue);

        RuleFor(x => x.CreatorId)
          .NotEmpty().WithMessage("Creator ID cannot be empty")
          .When(x => !string.IsNullOrEmpty(x.CreatorId));

        RuleFor(x => x.SortBy)
          .Must(BeValidSortField).WithMessage("Invalid sort field")
          .When(x => !string.IsNullOrEmpty(x.SortBy));
    }

    private static bool BeValidSortField(string? sortField) {
        var validFields = new[] { "CreatedAt", "UpdatedAt", "Title", "EstimatedHours", "Rating", "EnrollmentCount" };
        return string.IsNullOrEmpty(sortField) || validFields.Contains(sortField);
    }
}

/// <summary> Validator for GetProgramByIdQuery </summary>
public class GetProgramByIdQueryValidator : AbstractValidator<GetProgramByIdQuery> {
    public GetProgramByIdQueryValidator() {
        RuleFor(x => x.Id)
          .NotEmpty().WithMessage("Program ID is required");
    }
}

/// <summary> Validator for GetProgramBySlugQuery </summary>
public class GetProgramBySlugQueryValidator : AbstractValidator<GetProgramBySlugQuery> {
    public GetProgramBySlugQueryValidator() {
        RuleFor(x => x.Slug)
          .NotEmpty().WithMessage("Program slug is required")
          .Length(3, 100).WithMessage("Program slug must be between 3 and 100 characters")
          .Matches(@"^[a-z0-9-]+$").WithMessage("Program slug must contain only lowercase letters, numbers, and hyphens");
    }
}

/// <summary> Validator for GetPublishedProgramBySlugQuery </summary>
public class GetPublishedProgramBySlugQueryValidator : AbstractValidator<GetPublishedProgramBySlugQuery> {
    public GetPublishedProgramBySlugQueryValidator() {
        RuleFor(x => x.Slug)
          .NotEmpty().WithMessage("Program slug is required")
          .Length(3, 100).WithMessage("Program slug must be between 3 and 100 characters")
          .Matches(@"^[a-z0-9-]+$").WithMessage("Program slug must contain only lowercase letters, numbers, and hyphens");
    }
}

// ===== SEARCH AND FILTER QUERY VALIDATORS =====

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

// Note: Additional query validators will be added when the corresponding query types are defined
// The following query types are referenced in handlers but not yet defined:
// - GetProgramsByCreatorQuery, GetUserEnrolledProgramsQuery, GetProgramEnrollmentsQuery
// - CheckUserEnrollmentQuery, GetProgramContentQuery, GetUserProgramProgressQuery
// - GetProgramStatisticsQuery, GetCreatorProgramStatisticsQuery, GetPopularProgramsQuery
// - GetRecentProgramsQuery, GetFeaturedProgramsQuery, GetRecommendedProgramsQuery
// - GetProgramRatingsQuery, GetUserProgramRatingQuery, GetUserWishlistQuery
