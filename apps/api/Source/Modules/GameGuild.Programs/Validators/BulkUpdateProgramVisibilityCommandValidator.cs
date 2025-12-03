using FluentValidation;
using GameGuild.Modules.Programs.Commands;

namespace GameGuild.Modules.Programs.Validators;

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