using FluentValidation;

namespace GameGuild.Commerce.Billing;

public sealed class ProcessBraintreeWebhookCommandValidator : AbstractValidator<ProcessBraintreeWebhookCommand>
{
    public ProcessBraintreeWebhookCommandValidator()
    {
        RuleFor(x => x.BtSignature)
            .NotEmpty()
            .WithMessage("Braintree bt_signature is required")
            .MaximumLength(512)
            .WithMessage("Braintree bt_signature cannot exceed 512 characters");

        RuleFor(x => x.BtPayload)
            .NotEmpty()
            .WithMessage("Braintree bt_payload is required")
            .MaximumLength(1000000) // 1MB limit
            .WithMessage("Braintree bt_payload cannot exceed 1MB");
    }
}
