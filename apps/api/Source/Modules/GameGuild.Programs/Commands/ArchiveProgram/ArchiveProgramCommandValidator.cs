using FluentValidation;

namespace GameGuild.Modules.Programs.Commands;

/// <summary> Validator for ArchiveProgramCommand </summary>
public class ArchiveProgramCommandValidator : AbstractValidator<ArchiveProgramCommand> {
    public ArchiveProgramCommandValidator() {
        RuleFor(x => x.Id)
          .NotEmpty().WithMessage("Program ID is required");
    }
}
