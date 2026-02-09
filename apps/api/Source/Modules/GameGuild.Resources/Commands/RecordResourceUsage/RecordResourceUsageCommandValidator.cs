using FluentValidation;

namespace GameGuild.Resources;

public sealed class RecordResourceUsageCommandValidator : AbstractValidator<RecordResourceUsageCommand>
{
    public RecordResourceUsageCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty().WithMessage("Tenant ID is required");

        RuleFor(x => x.ResourceUsageType).IsInEnum().WithMessage("Invalid usage type");

        RuleFor(x => x.Count).GreaterThan(0).WithMessage("Usage count must be greater than zero");

        RuleFor(x => x.PeriodStart).NotEmpty().WithMessage("Period start date is required");

        RuleFor(x => x.PeriodEnd).NotEmpty().WithMessage("Period end date is required").GreaterThan(x => x.PeriodStart).WithMessage("Period end must be after period start");

        RuleFor(x => x.Metadata).MaximumLength(1000).When(x => !string.IsNullOrEmpty(x.Metadata)).WithMessage("Metadata cannot exceed 1000 characters");
    }
}
