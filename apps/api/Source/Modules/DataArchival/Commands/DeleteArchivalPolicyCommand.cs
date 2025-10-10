using GameGuild.Core.Shared;
using GameGuild.Modules.DataArchival.Services;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace GameGuild.Modules.DataArchival.Commands;

/// <summary>
/// Command to delete an archival policy.
/// </summary>
public record DeleteArchivalPolicyCommand : IRequest<Result<bool>>
{
    [Required]
    public Guid PolicyId { get; init; }
}

/// <summary>
/// Handler for DeleteArchivalPolicyCommand.
/// </summary>
public class DeleteArchivalPolicyCommandHandler : IRequestHandler<DeleteArchivalPolicyCommand, Result<bool>>
{
    private readonly IDataArchivalService _dataArchivalService;

    public DeleteArchivalPolicyCommandHandler(IDataArchivalService dataArchivalService)
    {
        _dataArchivalService = dataArchivalService;
    }

    public async Task<Result<bool>> Handle(DeleteArchivalPolicyCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var success = await _dataArchivalService.DeletePolicyAsync(request.PolicyId, cancellationToken);

            if (!success)
                return Result<bool>.Failure($"Archival policy with ID {request.PolicyId} not found");

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure($"Failed to delete archival policy: {ex.Message}");
        }
    }
}
