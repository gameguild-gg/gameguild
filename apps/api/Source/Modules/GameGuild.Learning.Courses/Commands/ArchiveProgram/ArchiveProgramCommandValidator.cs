using FluentValidation;

namespace GameGuild.Learning.Courses;

/// <summary> Validator for ArchiveProgramCommand </summary>
public sealed class ArchiveProgramCommandValidator : AbstractValidator<ArchiveProgramCommand> {
    public ArchiveProgramCommandValidator() {
        RuleFor(x => x.Id)
          .NotEmpty().WithMessage("Program ID is required");
    }
}
