using FluentValidation;

namespace GameGuild.Resources;

public sealed class CheckResourceQuotaQueryValidator : AbstractValidator<CheckResourceQuotaQuery>
{
    public CheckResourceQuotaQueryValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty().WithMessage("Tenant ID is required");

        RuleFor(x => x.Type).IsInEnum().WithMessage("Invalid usage type");

        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Amount must be greater than zero");
    }
}
