using FluentValidation;
using GameGuild.Modules.Programs.Queries;


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

// ===== SEARCH AND FILTER QUERY VALIDATORS =====

// Note: Additional query validators will be added when the corresponding query types are defined
// The following query types are referenced in handlers but not yet defined:
// - GetProgramsByCreatorQuery, GetUserEnrolledProgramsQuery, GetProgramEnrollmentsQuery
// - CheckUserEnrollmentQuery, GetProgramContentQuery, GetUserProgramProgressQuery
// - GetProgramStatisticsQuery, GetCreatorProgramStatisticsQuery, GetPopularProgramsQuery
// - GetRecentProgramsQuery, GetFeaturedProgramsQuery, GetRecommendedProgramsQuery
// - GetProgramRatingsQuery, GetUserProgramRatingQuery, GetUserWishlistQuery
