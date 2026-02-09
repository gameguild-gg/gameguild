using GameGuild.CQRS;


namespace GameGuild.Monitoring.SLA;

/// <summary>
///     Handler for resolving SLO violations.
/// </summary>
public class ResolveSloViolationCommandHandler(ISloViolationRepository violationRepository) : ICommandHandler<ResolveSloViolationCommand, Unit>
{
    public async Task<Unit> Handle(ResolveSloViolationCommand request, CancellationToken cancellationToken)
    {
        var violation = await violationRepository.GetByIdAsync(request.ViolationId, cancellationToken).ConfigureAwait(false);

        if (violation == null) { throw new InvalidOperationException($"Violation with ID '{request.ViolationId}' not found."); }

        if (violation.TenantId != request.TenantId) { throw new UnauthorizedAccessException("You do not have permission to resolve this violation."); }

        if (violation.EndedAt.HasValue) { throw new InvalidOperationException("Violation is already resolved."); }

        // Mark as resolved
        violation.Resolve();

        // Set resolution notes by acknowledging
        if (!string.IsNullOrWhiteSpace(request.ResolutionNotes)) { violation.Notes = request.ResolutionNotes; }

        await violationRepository.UpdateAsync(violation, cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
