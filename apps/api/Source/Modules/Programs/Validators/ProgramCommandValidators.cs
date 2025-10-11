using FluentValidation;
using GameGuild.Modules.Contents;
using GameGuild.Source.Modules.Programs.Commands;
using GameGuild.Source.Modules.Programs.Models;
using ProgramAvailabilityStatus = GameGuild.Source.Modules.Programs.Models.EnrollmentStatus;

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

/// <summary> Validator for UpdateProgramCommand </summary>
public class UpdateProgramCommandValidator : AbstractValidator<UpdateProgramCommand> {
    public UpdateProgramCommandValidator() {
        RuleFor(x => x.Id)
          .NotEmpty().WithMessage("Program ID is required");

        RuleFor(x => x.Title)
          .Length(3, 255).WithMessage("Program title must be between 3 and 255 characters")
          .When(x => !string.IsNullOrEmpty(x.Title));

        RuleFor(x => x.Description)
          .Length(10, 2000).WithMessage("Program description must be between 10 and 2000 characters")
          .When(x => !string.IsNullOrEmpty(x.Description));

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
          .IsInEnum().WithMessage("Invalid program category")
          .When(x => x.Category.HasValue);

        RuleFor(x => x.Difficulty)
          .IsInEnum().WithMessage("Invalid program difficulty")
          .When(x => x.Difficulty.HasValue);

        RuleFor(x => x.EnrollmentStatus)
          .IsInEnum().WithMessage("Invalid enrollment status")
          .When(x => x.EnrollmentStatus.HasValue);

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

/// <summary> Validator for DeleteProgramCommand </summary>
public class DeleteProgramCommandValidator : AbstractValidator<DeleteProgramCommand> {
    public DeleteProgramCommandValidator() {
        RuleFor(x => x.Id)
          .NotEmpty().WithMessage("Program ID is required");
    }
}

// ===== STATUS COMMAND VALIDATORS =====

/// <summary> Validator for PublishProgramCommand </summary>
public class PublishProgramCommandValidator : AbstractValidator<PublishProgramCommand> {
    public PublishProgramCommandValidator() {
        RuleFor(x => x.Id)
          .NotEmpty().WithMessage("Program ID is required");
    }
}

/// <summary> Validator for UnpublishProgramCommand </summary>
public class UnpublishProgramCommandValidator : AbstractValidator<UnpublishProgramCommand> {
    public UnpublishProgramCommandValidator() {
        RuleFor(x => x.Id)
          .NotEmpty().WithMessage("Program ID is required");
    }
}

/// <summary> Validator for ArchiveProgramCommand </summary>
public class ArchiveProgramCommandValidator : AbstractValidator<ArchiveProgramCommand> {
    public ArchiveProgramCommandValidator() {
        RuleFor(x => x.Id)
          .NotEmpty().WithMessage("Program ID is required");
    }
}

/// <summary> Validator for RestoreProgramCommand </summary>
public class RestoreProgramCommandValidator : AbstractValidator<RestoreProgramCommand> {
    public RestoreProgramCommandValidator() {
        RuleFor(x => x.Id)
          .NotEmpty().WithMessage("Program ID is required");
    }
}

// ===== ENROLLMENT COMMAND VALIDATORS =====

/// <summary> Validator for EnrollUserCommand </summary>
public class EnrollUserCommandValidator : AbstractValidator<EnrollUserCommand> {
    public EnrollUserCommandValidator() {
        RuleFor(x => x.ProgramId)
          .NotEmpty().WithMessage("Program ID is required");

        RuleFor(x => x.UserId)
          .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.EnrollmentDate)
          .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("Enrollment date cannot be in the future")
          .When(x => x.EnrollmentDate.HasValue);
    }
}

/// <summary> Validator for UnenrollUserCommand </summary>
public class UnenrollUserCommandValidator : AbstractValidator<UnenrollUserCommand> {
    public UnenrollUserCommandValidator() {
        RuleFor(x => x.ProgramId)
          .NotEmpty().WithMessage("Program ID is required");

        RuleFor(x => x.UserId)
          .NotEmpty().WithMessage("User ID is required");
    }
}

/// <summary> Validator for UpdateEnrollmentStatusCommand </summary>
public class UpdateEnrollmentStatusCommandValidator : AbstractValidator<UpdateEnrollmentStatusCommand> {
    public UpdateEnrollmentStatusCommandValidator() {
        RuleFor(x => x.ProgramId)
          .NotEmpty().WithMessage("Program ID is required");

        RuleFor(x => x.Status)
          .IsInEnum().WithMessage("Invalid enrollment status");

        RuleFor(x => x.MaxEnrollments)
          .GreaterThan(0).WithMessage("Maximum enrollments must be greater than 0")
          .LessThanOrEqualTo(10000).WithMessage("Maximum enrollments cannot exceed 10,000")
          .When(x => x.MaxEnrollments.HasValue);

        RuleFor(x => x.EnrollmentDeadline)
          .GreaterThan(DateTime.UtcNow).WithMessage("Enrollment deadline must be in the future")
          .When(x => x.EnrollmentDeadline.HasValue);
    }
}

// ===== CONTENT MANAGEMENT COMMAND VALIDATORS =====

/// <summary> Validator for AddProgramContentCommand </summary>
public class AddProgramContentCommandValidator : AbstractValidator<AddProgramContentCommand> {
    public AddProgramContentCommandValidator() {
        RuleFor(x => x.ProgramId)
          .NotEmpty().WithMessage("Program ID is required");

        RuleFor(x => x.ContentId)
          .NotEmpty().WithMessage("Content ID is required");

        RuleFor(x => x.Order)
          .GreaterThanOrEqualTo(0).WithMessage("Order must be greater than or equal to 0");

        RuleFor(x => x.PointsReward)
          .GreaterThanOrEqualTo(0).WithMessage("Points reward must be greater than or equal to 0")
          .LessThanOrEqualTo(1000).WithMessage("Points reward cannot exceed 1000")
          .When(x => x.PointsReward.HasValue);
    }
}

/// <summary> Validator for RemoveProgramContentCommand </summary>
public class RemoveProgramContentCommandValidator : AbstractValidator<RemoveProgramContentCommand> {
    public RemoveProgramContentCommandValidator() {
        RuleFor(x => x.ProgramId)
          .NotEmpty().WithMessage("Program ID is required");

        RuleFor(x => x.ContentId)
          .NotEmpty().WithMessage("Content ID is required");
    }
}

/// <summary> Validator for ReorderProgramContentCommand </summary>
public class ReorderProgramContentCommandValidator : AbstractValidator<ReorderProgramContentCommand> {
    public ReorderProgramContentCommandValidator() {
        RuleFor(x => x.ProgramId)
          .NotEmpty().WithMessage("Program ID is required");

        RuleFor(x => x.ContentOrders)
          .NotEmpty().WithMessage("Content orders are required")
          .Must(orders => orders.All(kvp => kvp.Value >= 0))
          .WithMessage("All order values must be greater than or equal to 0");
    }
}

// ===== RATING COMMAND VALIDATORS =====

/// <summary> Validator for RateProgramCommand </summary>
public class RateProgramCommandValidator : AbstractValidator<RateProgramCommand> {
    public RateProgramCommandValidator() {
        RuleFor(x => x.ProgramId)
          .NotEmpty().WithMessage("Program ID is required");

        RuleFor(x => x.UserId)
          .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.Rating)
          .InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5");

        RuleFor(x => x.Review)
          .Length(10, 1000).WithMessage("Review must be between 10 and 1000 characters")
          .When(x => !string.IsNullOrEmpty(x.Review));
    }
}

/// <summary> Validator for UpdateProgramRatingCommand </summary>
public class UpdateProgramRatingCommandValidator : AbstractValidator<UpdateProgramRatingCommand> {
    public UpdateProgramRatingCommandValidator() {
        RuleFor(x => x.ProgramId)
          .NotEmpty().WithMessage("Program ID is required");

        RuleFor(x => x.UserId)
          .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.Rating)
          .InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5");

        RuleFor(x => x.Review)
          .Length(10, 1000).WithMessage("Review must be between 10 and 1000 characters")
          .When(x => !string.IsNullOrEmpty(x.Review));
    }
}

/// <summary> Validator for DeleteProgramRatingCommand </summary>
public class DeleteProgramRatingCommandValidator : AbstractValidator<DeleteProgramRatingCommand> {
    public DeleteProgramRatingCommandValidator() {
        RuleFor(x => x.ProgramId)
          .NotEmpty().WithMessage("Program ID is required");

        RuleFor(x => x.UserId)
          .NotEmpty().WithMessage("User ID is required");
    }
}

// ===== WISHLIST COMMAND VALIDATORS =====

/// <summary> Validator for AddToWishlistCommand </summary>
public class AddToWishlistCommandValidator : AbstractValidator<AddToWishlistCommand> {
    public AddToWishlistCommandValidator() {
        RuleFor(x => x.ProgramId)
          .NotEmpty().WithMessage("Program ID is required");

        RuleFor(x => x.UserId)
          .NotEmpty().WithMessage("User ID is required");
    }
}

/// <summary> Validator for RemoveFromWishlistCommand </summary>
public class RemoveFromWishlistCommandValidator : AbstractValidator<RemoveFromWishlistCommand> {
    public RemoveFromWishlistCommandValidator() {
        RuleFor(x => x.ProgramId)
          .NotEmpty().WithMessage("Program ID is required");

        RuleFor(x => x.UserId)
          .NotEmpty().WithMessage("User ID is required");
    }
}

// ===== BULK OPERATIONS COMMAND VALIDATORS =====

/// <summary> Validator for BulkUpdateProgramVisibilityCommand </summary>
public class BulkUpdateProgramVisibilityCommandValidator : AbstractValidator<BulkUpdateProgramVisibilityCommand> {
    public BulkUpdateProgramVisibilityCommandValidator() {
        RuleFor(x => x.ProgramIds)
          .NotEmpty().WithMessage("Program IDs are required")
          .Must(ids => ids.All(id => id != Guid.Empty)).WithMessage("All Program IDs must be valid");

        RuleFor(x => x.Visibility)
          .IsInEnum().WithMessage("Invalid visibility level");
    }
}

/// <summary> Validator for BulkArchiveProgramsCommand </summary>
public class BulkArchiveProgramsCommandValidator : AbstractValidator<BulkArchiveProgramsCommand> {
    public BulkArchiveProgramsCommandValidator() {
        RuleFor(x => x.ProgramIds)
          .NotEmpty().WithMessage("Program IDs are required")
          .Must(ids => ids.All(id => id != Guid.Empty)).WithMessage("All Program IDs must be valid");
    }
}
