using FluentValidation;

namespace GameGuild.Commerce.Billing;

public sealed class ProcessGooglePayWebhookCommandValidator : AbstractValidator<ProcessGooglePayWebhookCommand>
{
    public ProcessGooglePayWebhookCommandValidator()
    {
        RuleFor(x => x.Payload)
            .NotEmpty()
            .WithMessage("Google Pay webhook payload is required")
            .MaximumLength(1000000) // 1MB limit
            .WithMessage("Google Pay webhook payload cannot exceed 1MB");

        RuleFor(x => x.AuthHeader).NotEmpty().WithMessage("Authorization header is required for Google Pay webhooks").MaximumLength(2000).WithMessage("Authorization header cannot exceed 2000 characters");

        RuleFor(x => x.ProjectId)
            .NotEmpty()
            .WithMessage("Google Cloud Project ID is required")
            .MaximumLength(100)
            .WithMessage("Project ID cannot exceed 100 characters")
            .Matches("^[a-z][a-z0-9\\-]{4,28}[a-z0-9]$")
            .WithMessage("Project ID must be a valid Google Cloud project identifier");
    }
}
