using FluentValidation;

namespace GameGuild.Monitoring.SLA;

/// <summary>
///     Validator for ResolveSloViolationCommand.
/// </summary>
public sealed class ResolveSloViolationCommandValidator : AbstractValidator<ResolveSloViolationCommand>
{
    public ResolveSloViolationCommandValidator()
    {
        RuleFor(x => x.ViolationId).NotEmpty().WithMessage("ViolationId is required.");

        RuleFor(x => x.TenantId).NotEmpty().WithMessage("TenantId is required.");

        RuleFor(x => x.ResolutionNotes).MaximumLength(2000).WithMessage("Resolution notes cannot exceed 2000 characters.");
    }
}
