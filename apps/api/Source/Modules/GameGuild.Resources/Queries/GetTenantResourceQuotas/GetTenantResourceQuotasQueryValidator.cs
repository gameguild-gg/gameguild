using FluentValidation;

namespace GameGuild.Resources.Queries;

public class GetTenantResourceQuotasQueryValidator : AbstractValidator<GetTenantResourceQuotasQuery>
{
    public GetTenantResourceQuotasQueryValidator() { RuleFor(x => x.TenantId).NotEmpty().WithMessage("Tenant ID is required"); }
}
