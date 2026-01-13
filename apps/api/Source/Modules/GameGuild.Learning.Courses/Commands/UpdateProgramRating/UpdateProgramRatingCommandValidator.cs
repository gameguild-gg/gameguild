using FluentValidation;

namespace GameGuild.Programs;

/// <summary> Validator for UpdateProgramRatingCommand </summary>
public class UpdateProgramRatingCommandValidator : AbstractValidator<UpdateProgramRatingCommand> {
    public UpdateProgramRatingCommandValidator() {
        RuleFor(x => x.ProgramId)
          .NotEmpty().WithMessage("Program ID is required");

        RuleFor(x => x.UserId)
          .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.Rating)
          .InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5");

        RuleFor(x => x.Review)
          .Length(10, 1000).WithMessage("Review must be between 10 and 1000 characters")
          .When(x => !string.IsNullOrEmpty(x.Review));
    }
}
