using FluentValidation;
using GameGuild.Modules.Programs.Commands;

namespace GameGuild.Modules.Programs.Validators;

/// <summary> Validator for RateProgramCommand </summary>
public class RateProgramCommandValidator : AbstractValidator<RateProgramCommand> {
    public RateProgramCommandValidator() {
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