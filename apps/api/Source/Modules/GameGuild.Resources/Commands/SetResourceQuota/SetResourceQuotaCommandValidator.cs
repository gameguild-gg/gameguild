using FluentValidation;

namespace GameGuild.Resources.Commands;

public class SetResourceQuotaCommandValidator : AbstractValidator<SetResourceQuotaCommand>
{
    public SetResourceQuotaCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty().WithMessage("Tenant ID is required");

        RuleFor(x => x.Type).IsInEnum().WithMessage("Invalid usage type");

        RuleFor(x => x.SoftLimit).GreaterThan(0).When(x => x.SoftLimit.HasValue).WithMessage("Soft limit must be greater than zero when specified");

        RuleFor(x => x.HardLimit).GreaterThan(0).When(x => x.HardLimit.HasValue).WithMessage("Hard limit must be greater than zero when specified");

        RuleFor(x => x).Must(x => !x.SoftLimit.HasValue || !x.HardLimit.HasValue || x.SoftLimit <= x.HardLimit).WithMessage("Soft limit cannot exceed hard limit").When(x => x.SoftLimit.HasValue && x.HardLimit.HasValue);

        RuleFor(x => x.Period).IsInEnum().WithMessage("Invalid quota period");

        RuleFor(x => x.ResetTime).Must(x => x >= TimeSpan.Zero && x < TimeSpan.FromDays(1)).When(x => x.ResetTime.HasValue).WithMessage("Reset time must be between 00:00:00 and 23:59:59");
    }
}
