using FluentValidation;

namespace GameGuild.Monitoring.SLA;

public sealed class GetErrorBudgetQueryValidator : AbstractValidator<GetErrorBudgetQuery>
{
    public GetErrorBudgetQueryValidator()
    {
        RuleFor(x => x.SloId).NotEmpty().WithMessage("SLO ID is required");

        RuleFor(x => x.TenantId).NotEmpty().WithMessage("Tenant ID is required");
    }
}
