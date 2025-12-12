using FluentValidation;

namespace GameGuild.Payments.Commands;

/// <summary>
///     Validator for UpdateDisputeStatusCommand
/// </summary>
public sealed class UpdateDisputeStatusCommandValidator : AbstractValidator<UpdateDisputeStatusCommand>
{
    public UpdateDisputeStatusCommandValidator()
    {
        RuleFor(x => x.DisputeId).NotEmpty().WithMessage("Dispute ID is required");

        RuleFor(x => x.NewStatus).NotEmpty().WithMessage("New status is required");

        RuleFor(x => x.DueDate).GreaterThan(DateTime.UtcNow).WithMessage("Due date must be in the future").When(x => x.DueDate.HasValue);
    }
}
