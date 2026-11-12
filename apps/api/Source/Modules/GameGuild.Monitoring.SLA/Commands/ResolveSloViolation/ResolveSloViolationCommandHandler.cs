using GameGuild.CQRS;


namespace GameGuild.Monitoring.SLA;

/// <summary>
///     Handler for resolving SLO violations.
/// </summary>
public sealed class ResolveSloViolationCommandHandler(ISloViolationRepository violationRepository) : ICommandHandler<ResolveSloViolationCommand, Unit>
{
    public async Task<Unit> Handle(ResolveSloViolationCommand request, CancellationToken cancellationToken)
    {
        var violation = await violationRepository.GetByIdAsync(request.ViolationId, cancellationToken).ConfigureAwait(false);

        if (violation == null) { throw new EntityNotFoundException("SloViolation", request.ViolationId); }

        if (violation.TenantId != request.TenantId) { throw new AccessDeniedException("You do not have permission to resolve this violation."); }

        if (violation.EndedAt.HasValue) { throw new BusinessRuleViolationException("SloViolation.AlreadyResolved", "Violation is already resolved."); }

        // Mark as resolved
        violation.Resolve();

        // Set resolution notes by acknowledging
        if (!string.IsNullOrWhiteSpace(request.ResolutionNotes)) { violation.Notes = request.ResolutionNotes; }

        await violationRepository.UpdateAsync(violation, cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
