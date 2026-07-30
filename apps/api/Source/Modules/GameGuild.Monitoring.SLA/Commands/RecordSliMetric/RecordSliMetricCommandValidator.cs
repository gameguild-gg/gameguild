using FluentValidation;

namespace GameGuild.Monitoring.SLA;

public sealed class RecordSliMetricCommandValidator : AbstractValidator<RecordSliMetricCommand>
{
    public RecordSliMetricCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty().WithMessage("TenantId is required.");

        RuleFor(x => x.ServiceLevelObjectiveId).NotEmpty().WithMessage("ServiceLevelObjectiveId is required.");

        RuleFor(x => x.Value).GreaterThanOrEqualTo(0).WithMessage("Value must be greater than or equal to 0.").LessThanOrEqualTo(100).WithMessage("Value must not exceed 100.");

        When(x => x.ResponseTimeMs.HasValue, () => { RuleFor(x => x.ResponseTimeMs!.Value).GreaterThanOrEqualTo(0).WithMessage("ResponseTimeMs must be greater than or equal to 0."); });

        When(x => !x.IsSuccessful, () => { RuleFor(x => x.ErrorMessage).NotEmpty().WithMessage("ErrorMessage is required when IsSuccessful is false."); });
    }
}
