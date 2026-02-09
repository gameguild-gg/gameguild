using FluentValidation;

namespace GameGuild.Learning.Courses;

/// <summary> Validator for DeleteProgramRatingCommand </summary>
public sealed class DeleteProgramRatingCommandValidator : AbstractValidator<DeleteProgramRatingCommand> {
    public DeleteProgramRatingCommandValidator() {
        RuleFor(x => x.ProgramId)
          .NotEmpty().WithMessage("Program ID is required");

        RuleFor(x => x.UserId)
          .NotEmpty().WithMessage("User ID is required");
    }
}
