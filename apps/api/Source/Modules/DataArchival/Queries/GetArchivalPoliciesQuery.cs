using GameGuild.Core.Shared;
using GameGuild.Modules.DataArchival.Services;
using GameGuild.CQRS;

namespace GameGuild.Modules.DataArchival.Queries;

/// <summary>
/// Query to get archival policies with optional filtering.
/// </summary>
public record GetArchivalPoliciesQuery : IRequest<Result<List<ArchivalPolicyDto>>>
{
    public Guid? TenantId { get; init; }

    public string? EntityType { get; init; }
}

/// <summary>
/// Handler for GetArchivalPoliciesQuery.
/// </summary>
public class GetArchivalPoliciesQueryHandler : IRequestHandler<GetArchivalPoliciesQuery, Result<List<ArchivalPolicyDto>>>
{
    private readonly IDataArchivalService _dataArchivalService;

    public GetArchivalPoliciesQueryHandler(IDataArchivalService dataArchivalService)
    {
        _dataArchivalService = dataArchivalService;
    }

    public async Task<Result<List<ArchivalPolicyDto>>> Handle(GetArchivalPoliciesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var policies = await _dataArchivalService.GetPoliciesAsync(request.TenantId, request.EntityType, cancellationToken);
            return Result<List<ArchivalPolicyDto>>.Success(policies);
        }
        catch (Exception ex)
        {
            return Result<List<ArchivalPolicyDto>>.Failure($"Failed to get archival policies: {ex.Message}");
        }
    }
}
