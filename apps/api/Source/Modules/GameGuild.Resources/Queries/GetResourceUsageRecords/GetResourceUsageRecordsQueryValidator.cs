using FluentValidation;

namespace GameGuild.Resources;

public sealed class GetResourceUsageRecordsQueryValidator : AbstractValidator<GetResourceUsageRecordsQuery>
{
    public GetResourceUsageRecordsQueryValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty().WithMessage("Tenant ID is required");

        RuleFor(x => x.ResourceUsageType).IsInEnum().When(x => x.ResourceUsageType.HasValue).WithMessage("Invalid usage type when specified");

        RuleFor(x => x.StartDate).LessThanOrEqualTo(DateTime.UtcNow).When(x => x.StartDate.HasValue).WithMessage("Start date cannot be in the future");

        RuleFor(x => x.EndDate).LessThanOrEqualTo(DateTime.UtcNow).When(x => x.EndDate.HasValue).WithMessage("End date cannot be in the future");

        RuleFor(x => x)
            .Must(x => !x.StartDate.HasValue || !x.EndDate.HasValue || x.EndDate >= x.StartDate)
            .WithMessage("End date must be after or equal to start date")
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue);

        RuleFor(x => x)
            .Must(x => !x.StartDate.HasValue || !x.EndDate.HasValue || (x.EndDate.Value - x.StartDate.Value).TotalDays <= 366)
            .WithMessage("Date range cannot exceed 366 days")
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue);
    }
}
