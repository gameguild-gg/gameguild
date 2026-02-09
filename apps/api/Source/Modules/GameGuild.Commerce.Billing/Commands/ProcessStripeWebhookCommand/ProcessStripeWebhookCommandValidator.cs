using FluentValidation;

namespace GameGuild.Commerce.Billing;

public sealed class ProcessStripeWebhookCommandValidator : AbstractValidator<ProcessStripeWebhookCommand>
{
    public ProcessStripeWebhookCommandValidator()
    {
        RuleFor(x => x.Payload)
            .NotEmpty()
            .WithMessage("Webhook payload is required")
            .MaximumLength(1000000) // 1MB limit
            .WithMessage("Webhook payload cannot exceed 1MB");

        RuleFor(x => x.Signature).NotEmpty().WithMessage("Webhook signature is required").MaximumLength(500).WithMessage("Webhook signature cannot exceed 500 characters");
    }
}
