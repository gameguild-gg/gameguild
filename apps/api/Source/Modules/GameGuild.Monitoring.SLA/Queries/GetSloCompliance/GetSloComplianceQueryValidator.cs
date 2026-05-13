using FluentValidation;

namespace GameGuild.Monitoring.SLA;

public sealed class GetSloComplianceQueryValidator : AbstractValidator<GetSloComplianceQuery>
{
    public GetSloComplianceQueryValidator()
    {
        RuleFor(x => x.SloId).NotEmpty().WithMessage("SLO ID is required");

        RuleFor(x => x.TenantId).NotEmpty().WithMessage("Tenant ID is required");

        RuleFor(x => x.StartDate).LessThanOrEqualTo(DateTimeOffset.UtcNow).When(HasStartDate).WithMessage("Start date cannot be in the future");

        RuleFor(x => x.EndDate).LessThanOrEqualTo(DateTimeOffset.UtcNow).When(HasEndDate).WithMessage("End date cannot be in the future");

        RuleFor(x => x)
            .Must(HaveOrderedDates)
            .WithMessage("End date must be after or equal to start date")
            .When(HasBothDates);

        RuleFor(x => x)
            .Must(BeWithinMaximumRange)
            .WithMessage("Date range cannot exceed 365 days")
            .When(HasBothDates);
    }

    private static bool HasStartDate(GetSloComplianceQuery query) => query.StartDate.HasValue;

    private static bool HasEndDate(GetSloComplianceQuery query) => query.EndDate.HasValue;

    private static bool HasBothDates(GetSloComplianceQuery query) => query.StartDate.HasValue && query.EndDate.HasValue;

    private static bool HaveOrderedDates(GetSloComplianceQuery query) => query.EndDate!.Value >= query.StartDate!.Value;

    private static bool BeWithinMaximumRange(GetSloComplianceQuery query) => (query.EndDate!.Value - query.StartDate!.Value).TotalDays <= 365;
}
