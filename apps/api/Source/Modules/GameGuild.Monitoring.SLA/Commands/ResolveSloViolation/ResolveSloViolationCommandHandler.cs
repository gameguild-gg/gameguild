using GameGuild.CQRS;
using GameGuild.Monitoring.SLA.Abstractions;
using GameGuild.Monitoring.SLA.Entities;

namespace GameGuild.Monitoring.SLA.Commands;

/// <summary>
///     Handler for resolving SLO violations.
/// </summary>
public class ResolveSloViolationCommandHandler(ISloViolationRepository violationRepository) : ICommandHandler<ResolveSloViolationCommand, Unit>
{
    public async Task<Unit> Handle(ResolveSloViolationCommand request, CancellationToken cancellationToken)
    {
        var violation = await violationRepository.GetByIdAsync(request.ViolationId, cancellationToken);

        if (violation == null) { throw new InvalidOperationException($"Violation with ID '{request.ViolationId}' not found."); }

        if (violation.TenantId != request.TenantId) { throw new UnauthorizedAccessException("You do not have permission to resolve this violation."); }

        if (violation.EndedAt.HasValue) { throw new InvalidOperationException("Violation is already resolved."); }

        // Mark as resolved
        violation.Resolve();

        // Set resolution notes by acknowledging
        if (!string.IsNullOrWhiteSpace(request.ResolutionNotes)) { violation.Notes = request.ResolutionNotes; }

        await violationRepository.UpdateAsync(violation, cancellationToken);

        return Unit.Value;
    }
}
