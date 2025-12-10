using FluentValidation;
using GameGuild.Modules.Programs.Commands;

namespace GameGuild.Modules.Programs.Validators;

/// <summary> Validator for ReorderProgramContentCommand </summary>
public class ReorderProgramContentCommandValidator : AbstractValidator<ReorderProgramContentCommand> {
    public ReorderProgramContentCommandValidator() {
        RuleFor(x => x.ProgramId)
            .NotEmpty().WithMessage("Program ID is required");

        RuleFor(x => x.ContentOrders)
            .NotEmpty().WithMessage("Content orders are required")
            .Must(orders => orders.All(kvp => kvp.Value >= 0))
            .WithMessage("All order values must be greater than or equal to 0");
    }
}