using FluentValidation;
using GameGuild.Modules.Programs.Commands;

namespace GameGuild.Modules.Programs.Validators;

/// <summary> Validator for AddProgramContentCommand </summary>
public class AddProgramContentCommandValidator : AbstractValidator<AddProgramContentCommand> {
    public AddProgramContentCommandValidator() {
        RuleFor(x => x.ProgramId)
            .NotEmpty().WithMessage("Program ID is required");

        RuleFor(x => x.ContentId)
            .NotEmpty().WithMessage("Content ID is required");

        RuleFor(x => x.Order)
            .GreaterThanOrEqualTo(0).WithMessage("Order must be greater than or equal to 0");

        RuleFor(x => x.PointsReward)
            .GreaterThanOrEqualTo(0).WithMessage("Points reward must be greater than or equal to 0")
            .LessThanOrEqualTo(1000).WithMessage("Points reward cannot exceed 1000")
            .When(x => x.PointsReward.HasValue);
    }
}