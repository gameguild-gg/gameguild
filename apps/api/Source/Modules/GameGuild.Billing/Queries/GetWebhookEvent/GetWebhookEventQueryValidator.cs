using FluentValidation;

namespace GameGuild.Billing.Queries;

public class GetWebhookEventQueryValidator : AbstractValidator<GetWebhookEventQuery>
{
    public GetWebhookEventQueryValidator() { RuleFor(x => x.EventId).NotEmpty().WithMessage("Event ID is required").MaximumLength(100).WithMessage("Event ID cannot exceed 100 characters"); }
}
