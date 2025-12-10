using FluentValidation;
using GameGuild.Modules.Programs.Commands;


namespace GameGuild.Modules.Programs.Validators;

/// <summary>
/// FluentValidation validators for Program CQRS commands
/// </summary>

// ===== CRUD COMMAND VALIDATORS =====

/// <summary> Validator for CreateProgramCommand </summary>
public class CreateProgramCommandValidator : AbstractValidator<CreateProgramCommand> {
    public CreateProgramCommandValidator() {
        RuleFor(x => x.Title)
          .NotEmpty().WithMessage("Program title is required")
          .Length(3, 255).WithMessage("Program title must be between 3 and 255 characters");

        RuleFor(x => x.Description)
          .NotEmpty().WithMessage("Program description is required")
          .Length(10, 2000).WithMessage("Program description must be between 10 and 2000 characters");

        RuleFor(x => x.Summary)
          .Length(10, 500).WithMessage("Program summary must be between 10 and 500 characters")
          .When(x => !string.IsNullOrEmpty(x.Summary));

        RuleFor(x => x.Thumbnail)
          .Must(BeValidUrl).WithMessage("Thumbnail must be a valid URL")
          .When(x => !string.IsNullOrEmpty(x.Thumbnail));

        RuleFor(x => x.VideoShowcaseUrl)
          .Must(BeValidUrl).WithMessage("Video showcase URL must be a valid URL")
          .When(x => !string.IsNullOrEmpty(x.VideoShowcaseUrl));

        RuleFor(x => x.EstimatedHours)
          .GreaterThan(0).WithMessage("Estimated hours must be greater than 0")
          .LessThanOrEqualTo(1000).WithMessage("Estimated hours cannot exceed 1000")
          .When(x => x.EstimatedHours.HasValue);

        RuleFor(x => x.Category)
          .IsInEnum().WithMessage("Invalid program category");

        RuleFor(x => x.Difficulty)
          .IsInEnum().WithMessage("Invalid program difficulty");

        RuleFor(x => x.EnrollmentStatus)
          .IsInEnum().WithMessage("Invalid enrollment status");

        RuleFor(x => x.MaxEnrollments)
          .GreaterThan(0).WithMessage("Maximum enrollments must be greater than 0")
          .LessThanOrEqualTo(10000).WithMessage("Maximum enrollments cannot exceed 10,000")
          .When(x => x.MaxEnrollments.HasValue);

        RuleFor(x => x.EnrollmentDeadline)
          .GreaterThan(DateTime.UtcNow).WithMessage("Enrollment deadline must be in the future")
          .When(x => x.EnrollmentDeadline.HasValue);
    }

    private static bool BeValidUrl(string? url) {
        return Uri.TryCreate(url, UriKind.Absolute, out var result) &&
               (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }
}

// ===== STATUS COMMAND VALIDATORS =====

// ===== ENROLLMENT COMMAND VALIDATORS =====

// ===== CONTENT MANAGEMENT COMMAND VALIDATORS =====

// ===== RATING COMMAND VALIDATORS =====

// ===== WISHLIST COMMAND VALIDATORS =====

// ===== BULK OPERATIONS COMMAND VALIDATORS =====