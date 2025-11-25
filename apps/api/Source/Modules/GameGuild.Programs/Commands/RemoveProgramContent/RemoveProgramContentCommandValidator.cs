using FluentValidation;

namespace GameGuild.Modules.Programs.Commands;

/// <summary> Validator for RemoveProgramContentCommand </summary>
public class RemoveProgramContentCommandValidator : AbstractValidator<RemoveProgramContentCommand> {
    public RemoveProgramContentCommandValidator() {
        RuleFor(x => x.ProgramId)
          .NotEmpty().WithMessage("Program ID is required");

        RuleFor(x => x.ContentId)
          .NotEmpty().WithMessage("Content ID is required");
    }
}
