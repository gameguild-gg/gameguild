using FluentValidation;
using GameGuild.Modules.Programs.Commands;

namespace GameGuild.Modules.Programs.Validators;

/// <summary> Validator for DeleteProgramCommand </summary>
public class DeleteProgramCommandValidator : AbstractValidator<DeleteProgramCommand> {
    public DeleteProgramCommandValidator() {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Program ID is required");
    }
}