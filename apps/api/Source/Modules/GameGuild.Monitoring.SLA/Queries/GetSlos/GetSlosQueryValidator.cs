using FluentValidation;

namespace GameGuild.Monitoring.SLA;

public sealed class GetSlosQueryValidator : AbstractValidator<GetSlosQuery>
{
    public GetSlosQueryValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty().WithMessage("Tenant ID is required");

        RuleFor(x => x.ServiceName).MaximumLength(100).When(x => !string.IsNullOrEmpty(x.ServiceName)).WithMessage("Service name cannot exceed 100 characters");

        RuleFor(x => x.Skip).GreaterThanOrEqualTo(0).WithMessage("Skip value cannot be negative");

        RuleFor(x => x.Take).GreaterThan(0).WithMessage("Take value must be greater than zero").LessThanOrEqualTo(1000).WithMessage("Take value cannot exceed 1000 records");
    }
}
