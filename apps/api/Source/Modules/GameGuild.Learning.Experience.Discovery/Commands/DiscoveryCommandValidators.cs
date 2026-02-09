using FluentValidation;

namespace GameGuild.Learning.Experience.Discovery;

/// <summary>
/// Validator for CreateFeaturedContentCommand
/// </summary>
public sealed class CreateFeaturedContentCommandValidator : AbstractValidator<CreateFeaturedContentCommand>
{
    public CreateFeaturedContentCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters");

        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Display order must be non-negative");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid featured content type");

        RuleFor(x => x.Subtitle)
            .MaximumLength(500).WithMessage("Subtitle must not exceed 500 characters")
            .When(x => x.Subtitle != null);

        RuleFor(x => x.ImageUrl)
            .MaximumLength(2000).WithMessage("Image URL must not exceed 2000 characters")
            .When(x => x.ImageUrl != null);

        RuleFor(x => x.LinkUrl)
            .MaximumLength(2000).WithMessage("Link URL must not exceed 2000 characters")
            .When(x => x.LinkUrl != null);

        RuleFor(x => x.EndsAt)
            .GreaterThan(x => x.StartsAt).WithMessage("End date must be after start date")
            .When(x => x.StartsAt.HasValue && x.EndsAt.HasValue);

        // Either CourseId or LearningPathId should be provided (not both)
        RuleFor(x => x)
            .Must(x => !(x.CourseId.HasValue && x.LearningPathId.HasValue))
            .WithMessage("Cannot specify both CourseId and LearningPathId");
    }
}

/// <summary>
/// Validator for UpdateFeaturedContentCommand
/// </summary>
public sealed class UpdateFeaturedContentCommandValidator : AbstractValidator<UpdateFeaturedContentCommand>
{
    public UpdateFeaturedContentCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Featured content ID is required");

        RuleFor(x => x.Title)
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters")
            .When(x => x.Title != null);

        RuleFor(x => x.Subtitle)
            .MaximumLength(500).WithMessage("Subtitle must not exceed 500 characters")
            .When(x => x.Subtitle != null);

        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Display order must be non-negative")
            .When(x => x.DisplayOrder.HasValue);
    }
}

/// <summary>
/// Validator for CreateCourseCollectionCommand
/// </summary>
public sealed class CreateCourseCollectionCommandValidator : AbstractValidator<CreateCourseCollectionCommand>
{
    public CreateCourseCollectionCommandValidator()
    {
        RuleFor(x => x.CuratorId)
            .NotEmpty().WithMessage("Curator ID is required");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid collection type");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters")
            .When(x => x.Description != null);

        RuleFor(x => x.ImageUrl)
            .MaximumLength(2000).WithMessage("Image URL must not exceed 2000 characters")
            .When(x => x.ImageUrl != null);
    }
}

/// <summary>
/// Validator for UpdateCourseCollectionCommand
/// </summary>
public sealed class UpdateCourseCollectionCommandValidator : AbstractValidator<UpdateCourseCollectionCommand>
{
    public UpdateCourseCollectionCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Collection ID is required");

        RuleFor(x => x.Title)
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters")
            .When(x => x.Title != null);

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters")
            .When(x => x.Description != null);
    }
}

/// <summary>
/// Validator for RecordSearchCommand
/// </summary>
public sealed class RecordSearchCommandValidator : AbstractValidator<RecordSearchCommand>
{
    public RecordSearchCommandValidator()
    {
        RuleFor(x => x.Query)
            .NotEmpty().WithMessage("Search query is required")
            .MaximumLength(500).WithMessage("Search query must not exceed 500 characters");

        RuleFor(x => x.ResultCount)
            .GreaterThanOrEqualTo(0).WithMessage("Result count must be non-negative");
    }
}
