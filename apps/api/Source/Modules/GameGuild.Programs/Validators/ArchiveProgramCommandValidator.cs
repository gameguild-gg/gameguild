using FluentValidation;
using GameGuild.Modules.Programs.Commands;

namespace GameGuild.Modules.Programs.Validators;

/// <summary> Validator for ArchiveProgramCommand </summary>
public class ArchiveProgramCommandValidator : AbstractValidator<ArchiveProgramCommand> {
    public ArchiveProgramCommandValidator() {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Program ID is required");
    }
}