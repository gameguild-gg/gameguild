using FluentValidation;

namespace GameGuild.Payments.Commands;

/// <summary>
///     Validator for AddDisputeEvidenceCommand
/// </summary>
public sealed class AddDisputeEvidenceCommandValidator : AbstractValidator<AddDisputeEvidenceCommand>
{
    public AddDisputeEvidenceCommandValidator()
    {
        RuleFor(x => x.DisputeId).NotEmpty().WithMessage("Dispute ID is required");

        RuleFor(x => x.Title).NotEmpty().WithMessage("Title is required").MaximumLength(200).WithMessage("Title cannot exceed 200 characters");

        RuleFor(x => x.Description).NotEmpty().WithMessage("Description is required").MaximumLength(5000).WithMessage("Description cannot exceed 5000 characters");

        RuleFor(x => x.EvidenceType).NotEmpty().WithMessage("Evidence type is required");

        RuleFor(x => x.SubmittedBy).NotEmpty().WithMessage("Submitted by is required");
    }
}
