using FluentValidation;

namespace GameGuild.Subscriptions.Queries;

/// <summary>
///     Validator for HasActiveSubscriptionQuery
/// </summary>
public class HasActiveSubscriptionQueryValidator : AbstractValidator<HasActiveSubscriptionQuery>
{
    public HasActiveSubscriptionQueryValidator() { RuleFor(x => x.TenantId).NotEmpty().WithMessage("TenantId is required"); }
}
