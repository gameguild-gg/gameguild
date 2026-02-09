using FluentValidation;

namespace GameGuild.Resources;

public sealed class GetResourceQuotaQueryValidator : AbstractValidator<GetResourceQuotaQuery>
{
    public GetResourceQuotaQueryValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty().WithMessage("Tenant ID is required");

        RuleFor(x => x.Type).IsInEnum().WithMessage("Invalid usage type");
    }
}
