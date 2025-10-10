using GameGuild.Core.Shared;
using GameGuild.Modules.DataArchival.Services;
using GameGuild.CQRS;
using System.ComponentModel.DataAnnotations;

namespace GameGuild.Modules.DataArchival.Commands;

/// <summary>
/// Command to cancel an archival job.
/// </summary>
public record CancelArchivalJobCommand : IRequest<Result<bool>>
{
    [Required]
    public Guid JobId { get; init; }
}

/// <summary>
/// Handler for CancelArchivalJobCommand.
/// </summary>
public class CancelArchivalJobCommandHandler : IRequestHandler<CancelArchivalJobCommand, Result<bool>>
{
    private readonly IArchivalJobRepository _jobRepository;

    public CancelArchivalJobCommandHandler(IArchivalJobRepository jobRepository)
    {
        _jobRepository = jobRepository;
    }

    public async Task<Result<bool>> Handle(CancelArchivalJobCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var job = await _jobRepository.GetByIdAsync(request.JobId, cancellationToken);

            if (job == null)
                return Result<bool>.Failure($"Archival job with ID {request.JobId} not found");

            if (job.Status == ArchivalJobStatus.Completed || job.Status == ArchivalJobStatus.Failed)
                return Result<bool>.Failure($"Cannot cancel job with status {job.Status}");

            job.Status = ArchivalJobStatus.Cancelled;
            job.CompletedAt = DateTime.UtcNow;

            await _jobRepository.UpdateAsync(job, cancellationToken);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure($"Failed to cancel archival job: {ex.Message}");
        }
    }
}
