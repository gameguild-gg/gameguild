using FluentValidation;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Validator for GetSubscriptionPlanBySlugQuery
/// </summary>
public class GetSubscriptionPlanBySlugQueryValidator : AbstractValidator<GetSubscriptionPlanBySlugQuery>
{
    public GetSubscriptionPlanBySlugQueryValidator()
    {
        RuleFor(x => x.Slug)
            .NotEmpty()
            .WithMessage("Slug is required")
            .MaximumLength(50)
            .WithMessage("Slug cannot exceed 50 characters")
            .Matches("^[a-z0-9-]+$")
            .WithMessage("Slug can only contain lowercase letters, numbers, and hyphens");
    }
}
