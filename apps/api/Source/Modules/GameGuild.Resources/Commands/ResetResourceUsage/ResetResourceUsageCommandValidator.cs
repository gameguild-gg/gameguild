using FluentValidation;

namespace GameGuild.Resources;

public sealed class ResetResourceUsageCommandValidator : AbstractValidator<ResetResourceUsageCommand>
{
    public ResetResourceUsageCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty().WithMessage("Tenant ID is required");

        RuleFor(x => x.ResourceUsageType).IsInEnum().When(x => x.ResourceUsageType.HasValue).WithMessage("Invalid usage type when specified");
    }
}
