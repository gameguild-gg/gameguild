using FluentValidation;

namespace GameGuild.Learning.Courses;

/// <summary> Validator for DeleteProgramCommand </summary>
public sealed class DeleteProgramCommandValidator : AbstractValidator<DeleteProgramCommand> {
    public DeleteProgramCommandValidator() {
        RuleFor(x => x.Id)
          .NotEmpty().WithMessage("Program ID is required");
    }
}
