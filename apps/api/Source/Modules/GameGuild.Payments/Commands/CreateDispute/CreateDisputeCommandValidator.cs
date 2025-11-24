using FluentValidation;

namespace GameGuild.Payments.Commands;

/// <summary>
///     Validator for CreateDisputeCommand
/// </summary>
public sealed class CreateDisputeCommandValidator : AbstractValidator<CreateDisputeCommand>
{
    public CreateDisputeCommandValidator()
    {
        RuleFor(x => x.PaymentId).NotEmpty().WithMessage("Payment ID is required");

        RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.Reason).NotEmpty().WithMessage("Dispute reason is required").MaximumLength(1000).WithMessage("Dispute reason cannot exceed 1000 characters");

        RuleFor(x => x.Type).NotEmpty().WithMessage("Dispute type is required");

        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Disputed amount must be greater than zero");

        RuleFor(x => x.Description).NotEmpty().WithMessage("Description is required").MaximumLength(5000).WithMessage("Description cannot exceed 5000 characters");
    }
}
