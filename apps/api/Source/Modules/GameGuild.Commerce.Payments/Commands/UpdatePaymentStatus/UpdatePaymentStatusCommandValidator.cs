using FluentValidation;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Validator for UpdatePaymentStatusCommand
/// </summary>
public sealed class UpdatePaymentStatusCommandValidator : AbstractValidator<UpdatePaymentStatusCommand>
{
    public UpdatePaymentStatusCommandValidator()
    {
        RuleFor(x => x.PaymentId).NotEmpty().WithMessage("Payment ID is required");

        RuleFor(x => x.Status).NotEmpty().WithMessage("Status is required");

        RuleFor(x => x.TransactionId).MaximumLength(100).WithMessage("Transaction ID cannot exceed 100 characters").When(x => !string.IsNullOrEmpty(x.TransactionId));
    }
}
