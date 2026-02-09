using FluentValidation;

namespace GameGuild.Resources;

/// <summary>
///     Validator for SetUserResourceQuotaCommand
/// </summary>
public sealed class SetUserResourceQuotaCommandValidator : AbstractValidator<SetUserResourceQuotaCommand>
{
    public SetUserResourceQuotaCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.Type).IsInEnum().WithMessage("Invalid resource usage type");

        RuleFor(x => x.SoftLimit).GreaterThanOrEqualTo(0).When(x => x.SoftLimit.HasValue).WithMessage("Soft limit must be non-negative");

        RuleFor(x => x.HardLimit).GreaterThanOrEqualTo(0).When(x => x.HardLimit.HasValue).WithMessage("Hard limit must be non-negative");

        RuleFor(x => x.HardLimit).GreaterThanOrEqualTo(x => x.SoftLimit).When(x => x.SoftLimit.HasValue && x.HardLimit.HasValue).WithMessage("Hard limit must be greater than or equal to soft limit");
    }
}
