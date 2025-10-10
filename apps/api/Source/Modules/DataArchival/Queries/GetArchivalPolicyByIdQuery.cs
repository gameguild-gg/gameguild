using GameGuild.Core.Shared;
using GameGuild.Modules.DataArchival.Services;
using GameGuild.CQRS;
using System.ComponentModel.DataAnnotations;

namespace GameGuild.Modules.DataArchival.Queries;

/// <summary>
/// Query to get an archival policy by ID.
/// </summary>
public record GetArchivalPolicyByIdQuery : IRequest<Result<ArchivalPolicyDto?>>
{
    [Required]
    public Guid PolicyId { get; init; }
}

/// <summary>
/// Handler for GetArchivalPolicyByIdQuery.
/// </summary>
public class GetArchivalPolicyByIdQueryHandler : IRequestHandler<GetArchivalPolicyByIdQuery, Result<ArchivalPolicyDto?>>
{
    private readonly IDataArchivalService _dataArchivalService;

    public GetArchivalPolicyByIdQueryHandler(IDataArchivalService dataArchivalService)
    {
        _dataArchivalService = dataArchivalService;
    }

    public async Task<Result<ArchivalPolicyDto?>> Handle(GetArchivalPolicyByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var policy = await _dataArchivalService.GetPolicyAsync(request.PolicyId, cancellationToken);
            return Result<ArchivalPolicyDto?>.Success(policy);
        }
        catch (Exception ex)
        {
            return Result<ArchivalPolicyDto?>.Failure($"Failed to get archival policy: {ex.Message}");
        }
    }
}
