using GameGuild.Core.Shared;
using GameGuild.Modules.DataArchival.Services;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace GameGuild.Modules.DataArchival.Commands;

/// <summary>
/// Command to create a new archival policy.
/// </summary>
public record CreateArchivalPolicyCommand : IRequest<Result<ArchivalPolicyDto>>
{
    [Required]
    public Guid TenantId { get; init; }

    [Required]
    [MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; init; }

    [Required]
    [MaxLength(100)]
    public string EntityType { get; init; } = string.Empty;

    [Range(1, 3650)]
    public int RetentionDays { get; init; }

    [Range(1, 3650)]
    public int ArchiveAfterDays { get; init; }

    [Range(1, 3650)]
    public int? DeleteAfterDays { get; init; }

    [Required]
    [MaxLength(50)]
    public string StorageTier { get; init; } = "Hot";

    public bool CompressionEnabled { get; init; }

    public bool EncryptionEnabled { get; init; }

    public bool IsEnabled { get; init; } = true;
}

/// <summary>
/// Handler for CreateArchivalPolicyCommand.
/// </summary>
public class CreateArchivalPolicyCommandHandler : IRequestHandler<CreateArchivalPolicyCommand, Result<ArchivalPolicyDto>>
{
    private readonly IDataArchivalService _dataArchivalService;

    public CreateArchivalPolicyCommandHandler(IDataArchivalService dataArchivalService)
    {
        _dataArchivalService = dataArchivalService;
    }

    public async Task<Result<ArchivalPolicyDto>> Handle(CreateArchivalPolicyCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var createRequest = new CreateArchivalPolicyRequest
            {
                TenantId = request.TenantId,
                Name = request.Name,
                Description = request.Description,
                EntityType = request.EntityType,
                RetentionDays = request.RetentionDays,
                ArchiveAfterDays = request.ArchiveAfterDays,
                DeleteAfterDays = request.DeleteAfterDays,
                StorageTier = request.StorageTier,
                CompressionEnabled = request.CompressionEnabled,
                EncryptionEnabled = request.EncryptionEnabled,
                IsEnabled = request.IsEnabled
            };

            var policy = await _dataArchivalService.CreatePolicyAsync(createRequest, cancellationToken);
            return Result<ArchivalPolicyDto>.Success(policy);
        }
        catch (Exception ex)
        {
            return Result<ArchivalPolicyDto>.Failure($"Failed to create archival policy: {ex.Message}");
        }
    }
}
