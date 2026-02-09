using FluentValidation;

namespace GameGuild.Resources;

/// <summary>
///     Validator for RecordUserResourceUsageCommand
/// </summary>
public sealed class RecordUserResourceUsageCommandValidator : AbstractValidator<RecordUserResourceUsageCommand>
{
    public RecordUserResourceUsageCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.ResourceUsageType).IsInEnum().WithMessage("Invalid resource usage type");

        RuleFor(x => x.Count).GreaterThan(0).WithMessage("Count must be greater than 0");

        RuleFor(x => x.PeriodStart).LessThanOrEqualTo(x => x.PeriodEnd).WithMessage("Period start must be before or equal to period end");
    }
}
