using GameGuild.Core.Shared;
using GameGuild.Modules.DataArchival.Services;
using GameGuild.CQRS;
using System.ComponentModel.DataAnnotations;

namespace GameGuild.Modules.DataArchival.Queries;

/// <summary>
/// Query to get the status of an archival job.
/// </summary>
public record GetArchivalJobStatusQuery : IRequest<Result<ArchivalJobDto?>>
{
    [Required]
    public Guid JobId { get; init; }
}

/// <summary>
/// Handler for GetArchivalJobStatusQuery.
/// </summary>
public class GetArchivalJobStatusQueryHandler : IRequestHandler<GetArchivalJobStatusQuery, Result<ArchivalJobDto?>>
{
    private readonly IDataArchivalService _dataArchivalService;

    public GetArchivalJobStatusQueryHandler(IDataArchivalService dataArchivalService)
    {
        _dataArchivalService = dataArchivalService;
    }

    public async Task<Result<ArchivalJobDto?>> Handle(GetArchivalJobStatusQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var job = await _dataArchivalService.GetJobStatusAsync(request.JobId, cancellationToken);
            return Result<ArchivalJobDto?>.Success(job);
        }
        catch (Exception ex)
        {
            return Result<ArchivalJobDto?>.Failure($"Failed to get archival job status: {ex.Message}");
        }
    }
}
