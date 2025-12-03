using FluentValidation;

namespace GameGuild.Modules.Programs.Commands;

/// <summary> Validator for DeleteProgramCommand </summary>
public class DeleteProgramCommandValidator : AbstractValidator<DeleteProgramCommand> {
    public DeleteProgramCommandValidator() {
        RuleFor(x => x.Id)
          .NotEmpty().WithMessage("Program ID is required");
    }
}
