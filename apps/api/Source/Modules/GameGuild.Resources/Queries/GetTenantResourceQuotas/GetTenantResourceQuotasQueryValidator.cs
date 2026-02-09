using FluentValidation;

namespace GameGuild.Resources;

public sealed class GetTenantResourceQuotasQueryValidator : AbstractValidator<GetTenantResourceQuotasQuery>
{
    public GetTenantResourceQuotasQueryValidator() { RuleFor(x => x.TenantId).NotEmpty().WithMessage("Tenant ID is required"); }
}
