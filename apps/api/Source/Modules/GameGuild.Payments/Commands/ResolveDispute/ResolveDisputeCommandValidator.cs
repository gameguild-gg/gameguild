using FluentValidation;

namespace GameGuild.Payments.Commands;

/// <summary>
///     Validator for ResolveDisputeCommand
/// </summary>
public sealed class ResolveDisputeCommandValidator : AbstractValidator<ResolveDisputeCommand>
{
    public ResolveDisputeCommandValidator()
    {
        RuleFor(x => x.DisputeId).NotEmpty().WithMessage("Dispute ID is required");

        RuleFor(x => x.Resolution).NotEmpty().WithMessage("Resolution is required");

        RuleFor(x => x.Notes).MaximumLength(2000).WithMessage("Notes cannot exceed 2000 characters").When(x => !string.IsNullOrEmpty(x.Notes));

        RuleFor(x => x.ResolvedBy).NotEmpty().WithMessage("Resolved by user ID is required").When(x => x.ResolvedBy.HasValue);
    }
}
