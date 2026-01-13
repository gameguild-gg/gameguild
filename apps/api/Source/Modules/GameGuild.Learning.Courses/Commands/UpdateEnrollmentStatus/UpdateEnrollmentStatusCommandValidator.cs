using FluentValidation;

namespace GameGuild.Learning.Courses;

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
