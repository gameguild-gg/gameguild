using FluentValidation;

namespace GameGuild.Modules.Programs.Commands;

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
