using FluentValidation;

namespace GameGuild.Resources;

public sealed class ToggleResourceQuotaCommandValidator : AbstractValidator<ToggleResourceQuotaCommand>
{
    public ToggleResourceQuotaCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty().WithMessage("Tenant ID is required");

        RuleFor(x => x.Type).IsInEnum().WithMessage("Invalid usage type");

        // IsActive is a bool, no validation needed as it can only be true or false
    }
}
