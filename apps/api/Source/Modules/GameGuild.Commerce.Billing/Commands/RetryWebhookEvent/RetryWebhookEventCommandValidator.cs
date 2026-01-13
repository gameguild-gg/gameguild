using FluentValidation;

namespace GameGuild.Commerce.Billing;

public class RetryWebhookEventCommandValidator : AbstractValidator<RetryWebhookEventCommand>
{
    public RetryWebhookEventCommandValidator() { RuleFor(x => x.EventId).NotEmpty().WithMessage("Event ID is required").MaximumLength(100).WithMessage("Event ID cannot exceed 100 characters"); }
}
