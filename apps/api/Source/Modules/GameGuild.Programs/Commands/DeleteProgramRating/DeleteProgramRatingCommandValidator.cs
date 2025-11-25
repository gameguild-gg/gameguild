using FluentValidation;

namespace GameGuild.Modules.Programs.Commands;

/// <summary> Validator for DeleteProgramRatingCommand </summary>
public class DeleteProgramRatingCommandValidator : AbstractValidator<DeleteProgramRatingCommand> {
    public DeleteProgramRatingCommandValidator() {
        RuleFor(x => x.ProgramId)
          .NotEmpty().WithMessage("Program ID is required");

        RuleFor(x => x.UserId)
          .NotEmpty().WithMessage("User ID is required");
    }
}
