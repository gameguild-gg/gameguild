using FluentValidation;

namespace GameGuild.Billing.Commands;

public class ProcessPayPalWebhookCommandValidator : AbstractValidator<ProcessPayPalWebhookCommand>
{
    public ProcessPayPalWebhookCommandValidator()
    {
        RuleFor(x => x.Payload)
            .NotEmpty()
            .WithMessage("PayPal webhook payload is required")
            .MaximumLength(1000000) // 1MB limit
            .WithMessage("PayPal webhook payload cannot exceed 1MB");
    }
}
