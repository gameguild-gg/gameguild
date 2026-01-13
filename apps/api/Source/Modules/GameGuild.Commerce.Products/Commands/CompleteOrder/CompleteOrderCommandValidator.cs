using FluentValidation;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Validator for CompleteOrderCommand
/// </summary>
public class CompleteOrderCommandValidator : AbstractValidator<CompleteOrderCommand>
{
    public CompleteOrderCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty()
            .WithMessage("Order ID is required.");

        RuleFor(x => x.PaymentProviderReference)
            .MaximumLength(200)
            .WithMessage("Payment provider reference cannot exceed 200 characters.")
            .When(x => !string.IsNullOrEmpty(x.PaymentProviderReference));

        RuleFor(x => x.PaymentMethod)
            .MaximumLength(50)
            .WithMessage("Payment method cannot exceed 50 characters.")
            .When(x => !string.IsNullOrEmpty(x.PaymentMethod));
    }
}
