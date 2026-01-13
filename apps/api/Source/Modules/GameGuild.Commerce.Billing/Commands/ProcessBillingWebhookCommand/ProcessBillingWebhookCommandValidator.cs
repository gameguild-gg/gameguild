using FluentValidation;

namespace GameGuild.Commerce.Billing;

public class ProcessBillingWebhookCommandValidator : AbstractValidator<ProcessBillingWebhookCommand>
{
    public ProcessBillingWebhookCommandValidator()
    {
        RuleFor(x => x.Provider)
            .NotEmpty()
            .WithMessage("Webhook provider is required")
            .MaximumLength(50)
            .WithMessage("Webhook provider cannot exceed 50 characters")
            .Must(provider => IsValidProvider(provider))
            .WithMessage("Invalid webhook provider. Supported providers: stripe, paypal, razorpay");

        RuleFor(x => x.Payload)
            .NotEmpty()
            .WithMessage("Webhook payload is required")
            .MaximumLength(1000000) // 1MB limit
            .WithMessage("Webhook payload cannot exceed 1MB");

        RuleFor(x => x.Headers).NotNull().WithMessage("Headers are required").Must(headers => headers.Count <= 50).WithMessage("Cannot have more than 50 headers");
    }

    private static bool IsValidProvider(string provider)
    {
        var validProviders = new[ ] { "stripe", "paypal", "razorpay", "square", "adyen" };

        return validProviders.Contains(provider.ToLowerInvariant());
    }
}
