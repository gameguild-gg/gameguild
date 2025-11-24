using FluentValidation;

namespace GameGuild.Payments.Commands;

public class RetryPaymentCommandValidator : AbstractValidator<RetryPaymentCommand>
{
    public RetryPaymentCommandValidator() { RuleFor(x => x.PaymentId).NotEmpty().WithMessage("Payment ID is required"); }
}
