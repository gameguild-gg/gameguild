using FluentValidation;

namespace GameGuild.Commerce.Payments;

public class RetryPaymentCommandValidator : AbstractValidator<RetryPaymentCommand>
{
    public RetryPaymentCommandValidator() { RuleFor(x => x.PaymentId).NotEmpty().WithMessage("Payment ID is required"); }
}
