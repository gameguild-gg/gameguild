using GameGuild.Modules.DataArchival.Services;
using GameGuild.Modules.DataArchival.Repositories;
using GameGuild.CQRS;

namespace GameGuild.Modules.DataArchival.Queries;

/// <summary>
/// Query to get archival jobs with optional filtering.
/// </summary>
public record GetArchivalJobsQuery : IRequest<Result<List<ArchivalJobDto>>>
{
    public Guid? TenantId { get; init; }

    public Guid? PolicyId { get; init; }

    public string? Status { get; init; }
}

/// <summary>
/// Handler for GetArchivalJobsQuery.
/// </summary>
public class GetArchivalJobsQueryHandler : IRequestHandler<GetArchivalJobsQuery, Result<List<ArchivalJobDto>>>
{
    private readonly IArchivalJobRepository _jobRepository;

    public GetArchivalJobsQueryHandler(IArchivalJobRepository jobRepository)
    {
        _jobRepository = jobRepository;
    }

    public async Task<Result<List<ArchivalJobDto>>> Handle(GetArchivalJobsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var jobs = await _jobRepository.GetAllAsync(request.TenantId, request.PolicyId, cancellationToken);

            // Filter by status if provided
            if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<ArchivalJobStatus>(request.Status, out var status))
            {
                jobs = jobs.Where(j => j.Status == status).ToList();
            }

            var jobDtos = jobs.Select(j => new ArchivalJobDto
            {
                Id = j.Id,
                PolicyId = j.PolicyId,
                TenantId = j.TenantId,
                Status = j.Status.ToString(),
                StartedAt = j.StartedAt,
                CompletedAt = j.CompletedAt,
                ItemsArchived = j.ItemsArchived,
                ItemsDeleted = j.ItemsDeleted,
                ErrorMessage = j.ErrorMessage
            }).ToList();

            return Result<List<ArchivalJobDto>>.Success(jobDtos);
        }
        catch (Exception ex)
        {
            return Result<List<ArchivalJobDto>>.Failure($"Failed to get archival jobs: {ex.Message}");
        }
    }
}
