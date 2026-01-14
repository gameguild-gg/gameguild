using FluentValidation;

namespace GameGuild.Commerce.Billing;

public class ProcessPayPalWebhookCommandValidator : AbstractValidator<ProcessPayPalWebhookCommand>
{
    public ProcessPayPalWebhookCommandValidator()
    {
        RuleFor(x => x.Payload)
            .NotEmpty()
            .WithMessage("PayPal webhook payload is required")
            .MaximumLength(1000000) // 1MB limit
            .WithMessage("PayPal webhook payload cannot exceed 1MB");

        RuleFor(x => x.TransmissionId)
            .NotEmpty()
            .WithMessage("PayPal transmission ID is required");

        RuleFor(x => x.TransmissionSignature)
            .NotEmpty()
            .WithMessage("PayPal transmission signature is required");

        RuleFor(x => x.TransmissionTime)
            .NotEmpty()
            .WithMessage("PayPal transmission time is required");
    }
}
