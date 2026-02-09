using FluentValidation;

namespace GameGuild.Monitoring.SLA;

public sealed class GetSloComplianceQueryValidator : AbstractValidator<GetSloComplianceQuery>
{
    public GetSloComplianceQueryValidator()
    {
        RuleFor(x => x.SloId).NotEmpty().WithMessage("SLO ID is required");

        RuleFor(x => x.TenantId).NotEmpty().WithMessage("Tenant ID is required");

        RuleFor(x => x.StartDate).LessThanOrEqualTo(DateTimeOffset.UtcNow).When(x => x.StartDate.HasValue).WithMessage("Start date cannot be in the future");

        RuleFor(x => x.EndDate).LessThanOrEqualTo(DateTimeOffset.UtcNow).When(x => x.EndDate.HasValue).WithMessage("End date cannot be in the future");

        RuleFor(x => x)
            .Must(x => !x.StartDate.HasValue || !x.EndDate.HasValue || x.EndDate >= x.StartDate)
            .WithMessage("End date must be after or equal to start date")
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue);

        RuleFor(x => x)
            .Must(x => !x.StartDate.HasValue || !x.EndDate.HasValue || (x.EndDate.Value - x.StartDate.Value).TotalDays <= 365)
            .WithMessage("Date range cannot exceed 365 days")
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue);
    }
}
