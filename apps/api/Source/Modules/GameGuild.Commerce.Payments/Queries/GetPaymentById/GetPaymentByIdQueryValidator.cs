using FluentValidation;

namespace GameGuild.Commerce.Payments;

public class GetPaymentByIdQueryValidator : AbstractValidator<GetPaymentByIdQuery>
{
    public GetPaymentByIdQueryValidator() { RuleFor(x => x.PaymentId).NotEmpty().WithMessage("Payment ID is required"); }
}
