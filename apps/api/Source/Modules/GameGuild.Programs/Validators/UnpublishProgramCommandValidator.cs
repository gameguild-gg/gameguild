using FluentValidation;
using GameGuild.Modules.Programs.Commands;

namespace GameGuild.Modules.Programs.Validators;

/// <summary> Validator for UnpublishProgramCommand </summary>
public class UnpublishProgramCommandValidator : AbstractValidator<UnpublishProgramCommand> {
    public UnpublishProgramCommandValidator() {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Program ID is required");
    }
}