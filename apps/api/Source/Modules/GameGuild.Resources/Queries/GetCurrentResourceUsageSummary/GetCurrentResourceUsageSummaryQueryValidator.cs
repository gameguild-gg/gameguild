using FluentValidation;

namespace GameGuild.Resources.Queries;

public class GetCurrentResourceUsageSummaryQueryValidator : AbstractValidator<GetCurrentResourceUsageSummaryQuery>
{
    public GetCurrentResourceUsageSummaryQueryValidator() { RuleFor(x => x.TenantId).NotEmpty().WithMessage("Tenant ID is required"); }
}
