using FluentValidation;

namespace GameGuild.Resources.Commands;

public class DeleteResourceQuotaCommandValidator : AbstractValidator<DeleteResourceQuotaCommand>
{
    public DeleteResourceQuotaCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty().WithMessage("Tenant ID is required");

        RuleFor(x => x.Type).IsInEnum().WithMessage("Invalid usage type");
    }
}
