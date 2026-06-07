using FluentValidation;

namespace GameGuild.Commerce.Payments;

public sealed class ProcessPaymentCommandValidator : AbstractValidator<ProcessPaymentCommand>
{
    public ProcessPaymentCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty().WithMessage("Tenant ID is required");

        RuleFor(x => x.SubscriptionId).NotEmpty().WithMessage("Subscription ID is required");

        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Payment amount must be greater than zero").LessThanOrEqualTo(1000000).WithMessage("Payment amount cannot exceed $1,000,000");

        RuleFor(x => x.PaymentMethodId)
            .NotEmpty().WithMessage("Payment method ID is required")
            .MaximumLength(100).WithMessage("Payment method ID cannot exceed 100 characters")
            .Must(StripePaymentMethodIdentifier.IsValid)
            .WithMessage(StripePaymentMethodIdentifier.ValidationMessage);
    }
}
