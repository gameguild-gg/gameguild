using FluentValidation;

namespace GameGuild.Resources;

public sealed class GetCurrentResourceUsageSummaryQueryValidator : AbstractValidator<GetCurrentResourceUsageSummaryQuery>
{
    public GetCurrentResourceUsageSummaryQueryValidator() { RuleFor(x => x.TenantId).NotEmpty().WithMessage("Tenant ID is required"); }
}
