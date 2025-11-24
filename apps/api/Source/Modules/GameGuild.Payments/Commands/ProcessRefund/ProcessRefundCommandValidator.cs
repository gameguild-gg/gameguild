using FluentValidation;

namespace GameGuild.Payments.Commands;

/// <summary>
///     Validator for ProcessRefundCommand
/// </summary>
public sealed class ProcessRefundCommandValidator : AbstractValidator<ProcessRefundCommand>
{
    public ProcessRefundCommandValidator()
    {
        RuleFor(x => x.PaymentId).NotEmpty().WithMessage("Payment ID is required");

        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Refund amount must be greater than zero").LessThanOrEqualTo(10000).WithMessage("Refund amount cannot exceed $10,000");

        RuleFor(x => x.Reason).NotEmpty().WithMessage("Refund reason is required").MaximumLength(500).WithMessage("Refund reason cannot exceed 500 characters");
    }
}
