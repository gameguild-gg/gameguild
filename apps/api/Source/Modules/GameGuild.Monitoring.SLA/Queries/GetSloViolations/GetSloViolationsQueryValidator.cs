using FluentValidation;

namespace GameGuild.Monitoring.SLA;

public sealed class GetSloViolationsQueryValidator : AbstractValidator<GetSloViolationsQuery>
{
    public GetSloViolationsQueryValidator()
    {
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

        RuleFor(x => x.Skip).GreaterThanOrEqualTo(0).WithMessage("Skip value cannot be negative");

        RuleFor(x => x.Take).GreaterThan(0).WithMessage("Take value must be greater than zero").LessThanOrEqualTo(1000).WithMessage("Take value cannot exceed 1000 records");
    }
}
