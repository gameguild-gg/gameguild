using FluentValidation;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Validator for CancelDisputeCommand
/// </summary>
public sealed class CancelDisputeCommandValidator : AbstractValidator<CancelDisputeCommand>
{
    public CancelDisputeCommandValidator()
    {
        RuleFor(x => x.DisputeId).NotEmpty().WithMessage("Dispute ID is required");

        RuleFor(x => x.Reason).NotEmpty().WithMessage("Cancellation reason is required").MaximumLength(1000).WithMessage("Cancellation reason cannot exceed 1000 characters");
    }
}
