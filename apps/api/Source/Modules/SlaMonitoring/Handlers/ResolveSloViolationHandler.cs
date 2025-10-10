using GameGuild.CQRS;
using MediatR;
using GameGuild.Core;
using GameGuild.Modules.SlaMonitoring.Commands;
using GameGuild.Modules.SlaMonitoring.Repositories;

namespace GameGuild.Modules.SlaMonitoring.Handlers;

/// <summary>
/// Handler for resolving SLO violations.
/// </summary>
public class ResolveSloViolationHandler : IRequestHandler<ResolveSloViolationCommand, Result<Unit>>
{
    private readonly ISloViolationRepository _violationRepository;

    public ResolveSloViolationHandler(ISloViolationRepository violationRepository)
    {
        _violationRepository = violationRepository;
    }

    public async Task<Result<Unit>> Handle(ResolveSloViolationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var violation = await _violationRepository.GetByIdAsync(request.ViolationId, cancellationToken);
            if (violation == null)
                return Result<Unit>.Failure($"Violation {request.ViolationId} not found");

            violation.Resolve();

            if (!string.IsNullOrWhiteSpace(request.ResolutionNotes))
                violation.AddNote(request.ResolutionNotes);

            await _violationRepository.UpdateAsync(violation, cancellationToken);

            return Result<Unit>.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            return Result<Unit>.Failure($"Failed to resolve violation: {ex.Message}");
        }
    }
}
