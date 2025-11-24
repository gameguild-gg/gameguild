using FluentValidation;

namespace GameGuild.Resources.Queries;

public class CheckResourceUsageLimitsQueryValidator : AbstractValidator<CheckResourceUsageLimitsQuery>
{
    public CheckResourceUsageLimitsQueryValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty().WithMessage("Tenant ID is required");

        RuleFor(x => x.ResourceUsageType).IsInEnum().When(x => x.ResourceUsageType.HasValue).WithMessage("Invalid usage type when specified");
    }
}
