using FluentValidation;

namespace GameGuild.Learning.Experience.LearningPaths;

/// <summary>
/// Validator for CreateLearningPathCommand
/// </summary>
public sealed class CreateLearningPathCommandValidator : AbstractValidator<CreateLearningPathCommand>
{
    public CreateLearningPathCommandValidator()
    {
        RuleFor(x => x.CreatorId)
            .NotEmpty().WithMessage("Creator ID is required");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters");

        RuleFor(x => x.Difficulty)
            .IsInEnum().WithMessage("Invalid difficulty level");

        RuleFor(x => x.Description)
            .MaximumLength(4000).WithMessage("Description must not exceed 4000 characters")
            .When(x => x.Description != null);

        RuleFor(x => x.ImageUrl)
            .MaximumLength(1000).WithMessage("Image URL must not exceed 1000 characters")
            .When(x => x.ImageUrl != null);

        RuleFor(x => x.EstimatedHours)
            .GreaterThanOrEqualTo(0).WithMessage("Estimated hours must be non-negative");
    }
}

/// <summary>
/// Validator for UpdateLearningPathCommand
/// </summary>
public sealed class UpdateLearningPathCommandValidator : AbstractValidator<UpdateLearningPathCommand>
{
    public UpdateLearningPathCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Learning path ID is required");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title must not be empty")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters")
            .When(x => x.Title != null);

        RuleFor(x => x.Description)
            .MaximumLength(4000).WithMessage("Description must not exceed 4000 characters")
            .When(x => x.Description != null);

        RuleFor(x => x.ImageUrl)
            .MaximumLength(1000).WithMessage("Image URL must not exceed 1000 characters")
            .When(x => x.ImageUrl != null);

        RuleFor(x => x.EstimatedHours)
            .GreaterThanOrEqualTo(0).WithMessage("Estimated hours must be non-negative")
            .When(x => x.EstimatedHours.HasValue);

        RuleFor(x => x.Difficulty)
            .IsInEnum().WithMessage("Invalid difficulty level")
            .When(x => x.Difficulty.HasValue);
    }
}

/// <summary>
/// Validator for AddCourseToPathCommand
/// </summary>
public sealed class AddCourseToPathCommandValidator : AbstractValidator<AddCourseToPathCommand>
{
    public AddCourseToPathCommandValidator()
    {
        RuleFor(x => x.LearningPathId)
            .NotEmpty().WithMessage("Learning path ID is required");

        RuleFor(x => x.CourseId)
            .NotEmpty().WithMessage("Course ID is required");

        RuleFor(x => x.Order)
            .GreaterThanOrEqualTo(0).WithMessage("Order must be non-negative");
    }
}

/// <summary>
/// Validator for EnrollInPathCommand
/// </summary>
public sealed class EnrollInPathCommandValidator : AbstractValidator<EnrollInPathCommand>
{
    public EnrollInPathCommandValidator()
    {
        RuleFor(x => x.LearningPathId)
            .NotEmpty().WithMessage("Learning path ID is required");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");
    }
}

/// <summary>
/// Validator for UpdatePathProgressCommand
/// </summary>
public sealed class UpdatePathProgressCommandValidator : AbstractValidator<UpdatePathProgressCommand>
{
    public UpdatePathProgressCommandValidator()
    {
        RuleFor(x => x.LearningPathId)
            .NotEmpty().WithMessage("Learning path ID is required");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.CoursesCompleted)
            .GreaterThanOrEqualTo(0).WithMessage("Courses completed must be non-negative");
    }
}

/// <summary>
/// Validator for ReorderPathCoursesCommand
/// </summary>
public sealed class ReorderPathCoursesCommandValidator : AbstractValidator<ReorderPathCoursesCommand>
{
    public ReorderPathCoursesCommandValidator()
    {
        RuleFor(x => x.LearningPathId)
            .NotEmpty().WithMessage("Learning path ID is required");

        RuleFor(x => x.Courses)
            .NotEmpty().WithMessage("At least one course must be specified");

        RuleForEach(x => x.Courses).ChildRules(course =>
        {
            course.RuleFor(x => x.CourseId).NotEmpty().WithMessage("Course ID is required");
            course.RuleFor(x => x.Order).GreaterThanOrEqualTo(0).WithMessage("Order must be non-negative");
        });

        RuleFor(x => x.Courses)
            .Must(courses => courses.Select(course => course.CourseId).Distinct().Count() == courses.Count())
            .WithMessage("Each course must be specified once")
            .Must(courses => courses.Select(course => course.Order).Distinct().Count() == courses.Count())
            .WithMessage("Each course order must be unique");
    }
}
