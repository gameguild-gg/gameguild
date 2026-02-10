using FluentValidation;

namespace GameGuild.Resources;

public sealed class GetResourceUsageByTypeQueryValidator : AbstractValidator<GetResourceUsageByTypeQuery>
{
    public GetResourceUsageByTypeQueryValidator()
    {
        RuleFor(x => x.ResourceUsageType).IsInEnum().WithMessage("Invalid usage type");

        RuleFor(x => x.StartDate).NotEmpty().WithMessage("Start date is required").LessThanOrEqualTo(SystemClock.UtcNow).WithMessage("Start date cannot be in the future");

        RuleFor(x => x.EndDate)
            .NotEmpty()
            .WithMessage("End date is required")
            .GreaterThan(x => x.StartDate)
            .WithMessage("End date must be after start date")
            .LessThanOrEqualTo(SystemClock.UtcNow)
            .WithMessage("End date cannot be in the future");

        RuleFor(x => x).Must(x => (x.EndDate - x.StartDate).TotalDays <= 366).WithMessage("Date range cannot exceed 366 days");
    }
}
