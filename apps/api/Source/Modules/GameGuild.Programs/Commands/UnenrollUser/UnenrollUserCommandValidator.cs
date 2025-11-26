using FluentValidation;

namespace GameGuild.Modules.Programs.Commands;

/// <summary> Validator for UnenrollUserCommand </summary>
public class UnenrollUserCommandValidator : AbstractValidator<UnenrollUserCommand> {
    public UnenrollUserCommandValidator() {
        RuleFor(x => x.ProgramId)
          .NotEmpty().WithMessage("Program ID is required");

        RuleFor(x => x.UserId)
          .NotEmpty().WithMessage("User ID is required");
    }
}
