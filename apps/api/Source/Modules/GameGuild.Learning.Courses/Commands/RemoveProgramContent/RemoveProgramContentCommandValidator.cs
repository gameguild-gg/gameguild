using FluentValidation;

namespace GameGuild.Learning.Courses;

/// <summary> Validator for RemoveProgramContentCommand </summary>
public class RemoveProgramContentCommandValidator : AbstractValidator<RemoveProgramContentCommand> {
    public RemoveProgramContentCommandValidator() {
        RuleFor(x => x.ProgramId)
          .NotEmpty().WithMessage("Program ID is required");

        RuleFor(x => x.ContentId)
          .NotEmpty().WithMessage("Content ID is required");
    }
}
