using FluentValidation;

namespace GameGuild.Payments.Commands;

/// <summary>
///     Validator for ReconcileLedgerCommand
/// </summary>
public sealed class ReconcileLedgerCommandValidator : AbstractValidator<ReconcileLedgerCommand>
{
    public ReconcileLedgerCommandValidator()
    {
        RuleFor(x => x.EntryId).NotEmpty().WithMessage("Entry ID is required");

        RuleFor(x => x.ReconciledBy).NotEmpty().WithMessage("Reconciled by user ID is required");

        RuleFor(x => x.Notes).MaximumLength(1000).WithMessage("Notes cannot exceed 1000 characters").When(x => !string.IsNullOrEmpty(x.Notes));
    }
}
