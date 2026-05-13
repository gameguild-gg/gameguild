using FluentValidation;

namespace GameGuild.Monitoring.SLA;

public sealed class GetSloViolationsQueryValidator : AbstractValidator<GetSloViolationsQuery>
{
    public GetSloViolationsQueryValidator()
    {
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

        RuleFor(x => x.Skip).GreaterThanOrEqualTo(0).WithMessage("Skip value cannot be negative");

        RuleFor(x => x.Take).GreaterThan(0).WithMessage("Take value must be greater than zero").LessThanOrEqualTo(1000).WithMessage("Take value cannot exceed 1000 records");
    }

    private static bool HasStartDate(GetSloViolationsQuery query) => query.StartDate.HasValue;

    private static bool HasEndDate(GetSloViolationsQuery query) => query.EndDate.HasValue;

    private static bool HasBothDates(GetSloViolationsQuery query) => query.StartDate.HasValue && query.EndDate.HasValue;

    private static bool HaveOrderedDates(GetSloViolationsQuery query) => query.EndDate!.Value >= query.StartDate!.Value;

    private static bool BeWithinMaximumRange(GetSloViolationsQuery query) => (query.EndDate!.Value - query.StartDate!.Value).TotalDays <= 365;
}
