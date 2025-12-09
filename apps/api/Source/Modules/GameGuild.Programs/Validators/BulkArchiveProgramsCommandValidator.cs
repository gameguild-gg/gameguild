using FluentValidation;
using GameGuild.Modules.Programs.Commands;

namespace GameGuild.Modules.Programs.Validators;

/// <summary> Validator for BulkArchiveProgramsCommand </summary>
public class BulkArchiveProgramsCommandValidator : AbstractValidator<BulkArchiveProgramsCommand> {
    public BulkArchiveProgramsCommandValidator() {
        RuleFor(x => x.ProgramIds)
            .NotEmpty().WithMessage("Program IDs are required")
            .Must(ids => ids.All(id => id != Guid.Empty)).WithMessage("All Program IDs must be valid");
    }
}