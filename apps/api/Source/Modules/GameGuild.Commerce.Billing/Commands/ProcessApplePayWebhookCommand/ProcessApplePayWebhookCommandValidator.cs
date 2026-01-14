using FluentValidation;

namespace GameGuild.Commerce.Billing;

/// <summary>
///     Validator for ProcessApplePayWebhookCommand
/// </summary>
public class ProcessApplePayWebhookCommandValidator : AbstractValidator<ProcessApplePayWebhookCommand>
{
    public ProcessApplePayWebhookCommandValidator()
    {
        RuleFor(x => x.Payload)
            .NotEmpty()
            .WithMessage("Payload is required");

        RuleFor(x => x.MerchantId)
            .NotEmpty()
            .WithMessage("Merchant ID is required");

        RuleFor(x => x.Signature)
            .NotEmpty()
            .WithMessage("Signature is required");
    }
}
