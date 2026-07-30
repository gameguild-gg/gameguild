using FluentValidation;

namespace GameGuild.Monitoring.SLA;

public sealed class UpdateSloCommandValidator : AbstractValidator<UpdateSloCommand>
{
    public UpdateSloCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Id is required.");

        RuleFor(x => x.TenantId).NotEmpty().WithMessage("TenantId is required.");

        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.").MaximumLength(200).WithMessage("Name must not exceed 200 characters.");

        RuleFor(x => x.ServiceName).NotEmpty().WithMessage("ServiceName is required.").MaximumLength(200).WithMessage("ServiceName must not exceed 200 characters.");

        RuleFor(x => x.TargetPercentage).GreaterThan(0).WithMessage("TargetPercentage must be greater than 0.").LessThanOrEqualTo(100).WithMessage("TargetPercentage must not exceed 100.");

        RuleFor(x => x.TimeWindowDays).GreaterThan(0).WithMessage("TimeWindowDays must be greater than 0.").LessThanOrEqualTo(365).WithMessage("TimeWindowDays must not exceed 365 days.");

        RuleFor(x => x.ErrorBudgetPercentage)
            .GreaterThanOrEqualTo(0)
            .WithMessage("ErrorBudgetPercentage must be greater than or equal to 0.")
            .LessThanOrEqualTo(100)
            .WithMessage("ErrorBudgetPercentage must not exceed 100.");

        RuleFor(x => x.AlertThresholdPercentage).GreaterThan(0).WithMessage("AlertThresholdPercentage must be greater than 0.").LessThanOrEqualTo(100).WithMessage("AlertThresholdPercentage must not exceed 100.");
    }
}
