using FluentValidation;

namespace GameGuild.Monitoring.SLA;

public sealed class GetSloByIdQueryValidator : AbstractValidator<GetSloByIdQuery>
{
    public GetSloByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("SLO ID is required");

        RuleFor(x => x.TenantId).NotEmpty().WithMessage("Tenant ID is required");
    }
}
