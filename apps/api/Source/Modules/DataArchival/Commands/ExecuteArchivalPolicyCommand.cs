using GameGuild.Core.Shared;
using GameGuild.Modules.DataArchival.Services;
using GameGuild.CQRS;
using System.ComponentModel.DataAnnotations;

namespace GameGuild.Modules.DataArchival.Commands;

/// <summary>
/// Command to execute an archival policy.
/// </summary>
public record ExecuteArchivalPolicyCommand : IRequest<Result<Guid>>
{
    [Required]
    public Guid PolicyId { get; init; }
}

/// <summary>
/// Handler for ExecuteArchivalPolicyCommand.
/// </summary>
public class ExecuteArchivalPolicyCommandHandler : IRequestHandler<ExecuteArchivalPolicyCommand, Result<Guid>>
{
    private readonly IDataArchivalService _dataArchivalService;

    public ExecuteArchivalPolicyCommandHandler(IDataArchivalService dataArchivalService)
    {
        _dataArchivalService = dataArchivalService;
    }

    public async Task<Result<Guid>> Handle(ExecuteArchivalPolicyCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var jobId = await _dataArchivalService.ExecutePolicyAsync(request.PolicyId, cancellationToken);
            return Result<Guid>.Success(jobId);
        }
        catch (InvalidOperationException ex)
        {
            return Result<Guid>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            return Result<Guid>.Failure($"Failed to execute archival policy: {ex.Message}");
        }
    }
}
