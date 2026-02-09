using FluentValidation;

namespace GameGuild.Resources;

public sealed class ResetResourceQuotaCommandValidator : AbstractValidator<ResetResourceQuotaCommand>
{
    public ResetResourceQuotaCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty().WithMessage("Tenant ID is required");

        RuleFor(x => x.Type).IsInEnum().WithMessage("Invalid usage type");
    }
}
