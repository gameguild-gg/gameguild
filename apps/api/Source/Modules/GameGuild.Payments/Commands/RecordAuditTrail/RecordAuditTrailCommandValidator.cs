using FluentValidation;

namespace GameGuild.Payments.Commands;

/// <summary>
///     Validator for RecordAuditTrailCommand
/// </summary>
public sealed class RecordAuditTrailCommandValidator : AbstractValidator<RecordAuditTrailCommand>
{
    public RecordAuditTrailCommandValidator()
    {
        RuleFor(x => x.EntityId).NotEmpty().WithMessage("Entity ID is required");

        RuleFor(x => x.EntityType).NotEmpty().WithMessage("Entity type is required").MaximumLength(100).WithMessage("Entity type cannot exceed 100 characters");

        RuleFor(x => x.Action).NotEmpty().WithMessage("Action is required").MaximumLength(50).WithMessage("Action cannot exceed 50 characters");

        RuleFor(x => x.ChangedBy).NotEmpty().WithMessage("Changed by user ID is required");

        RuleFor(x => x.OldValue).MaximumLength(2000).WithMessage("Old value cannot exceed 2000 characters").When(x => !string.IsNullOrEmpty(x.OldValue));

        RuleFor(x => x.NewValue).MaximumLength(2000).WithMessage("New value cannot exceed 2000 characters").When(x => !string.IsNullOrEmpty(x.NewValue));

        RuleFor(x => x.Reason).MaximumLength(500).WithMessage("Reason cannot exceed 500 characters").When(x => !string.IsNullOrEmpty(x.Reason));
    }
}
