using FluentValidation;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Validator for HasActiveSubscriptionQuery
/// </summary>
public sealed class HasActiveSubscriptionQueryValidator : AbstractValidator<HasActiveSubscriptionQuery>
{
    public HasActiveSubscriptionQueryValidator() { RuleFor(x => x.TenantId).NotEmpty().WithMessage("TenantId is required"); }
}
