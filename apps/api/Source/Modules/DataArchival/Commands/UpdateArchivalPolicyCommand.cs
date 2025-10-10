using GameGuild.Modules.DataArchival.Services;
using GameGuild.CQRS;
using System.ComponentModel.DataAnnotations;

namespace GameGuild.Modules.DataArchival.Commands;

/// <summary>
/// Command to update an existing archival policy.
/// </summary>
public record UpdateArchivalPolicyCommand : IRequest<Result<ArchivalPolicyDto>>
{
    [Required]
    public Guid PolicyId { get; init; }

    [MaxLength(200)]
    public string? Name { get; init; }

    [MaxLength(1000)]
    public string? Description { get; init; }

    [Range(1, 3650)]
    public int? RetentionDays { get; init; }

    [Range(1, 3650)]
    public int? ArchiveAfterDays { get; init; }

    [Range(1, 3650)]
    public int? DeleteAfterDays { get; init; }

    [MaxLength(50)]
    public string? StorageTier { get; init; }

    public bool? CompressionEnabled { get; init; }

    public bool? EncryptionEnabled { get; init; }

    public bool? IsEnabled { get; init; }
}

/// <summary>
/// Handler for UpdateArchivalPolicyCommand.
/// </summary>
public class UpdateArchivalPolicyCommandHandler : IRequestHandler<UpdateArchivalPolicyCommand, Result<ArchivalPolicyDto>>
{
    private readonly IDataArchivalService _dataArchivalService;

    public UpdateArchivalPolicyCommandHandler(IDataArchivalService dataArchivalService)
    {
        _dataArchivalService = dataArchivalService;
    }

    public async Task<Result<ArchivalPolicyDto>> Handle(UpdateArchivalPolicyCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var updateRequest = new UpdateArchivalPolicyRequest
            {
                Name = request.Name,
                Description = request.Description,
                RetentionDays = request.RetentionDays,
                ArchiveAfterDays = request.ArchiveAfterDays,
                DeleteAfterDays = request.DeleteAfterDays,
                StorageTier = request.StorageTier,
                CompressionEnabled = request.CompressionEnabled,
                EncryptionEnabled = request.EncryptionEnabled,
                IsEnabled = request.IsEnabled
            };

            var policy = await _dataArchivalService.UpdatePolicyAsync(request.PolicyId, updateRequest, cancellationToken);
            
            if (policy == null)
                return Result<ArchivalPolicyDto>.Failure($"Archival policy with ID {request.PolicyId} not found");

            return Result<ArchivalPolicyDto>.Success(policy);
        }
        catch (Exception ex)
        {
            return Result<ArchivalPolicyDto>.Failure($"Failed to update archival policy: {ex.Message}");
        }
    }
}
