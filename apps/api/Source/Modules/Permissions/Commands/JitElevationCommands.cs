using GameGuild.CQRS;

namespace GameGuild.Modules.Permissions.Commands;

/// <summary>
/// Command to request JIT elevation
/// </summary>
public class RequestJitElevationCommand : IRequest<JitElevationRequest>
{
    public Guid RequesterId { get; init; }
    public Guid? TenantId { get; init; }
    public PermissionType Permission { get; init; }
    public string Justification { get; init; } = string.Empty;
    public int DurationMinutes { get; init; }
    public string? ResourceType { get; init; }
    public Guid? ResourceId { get; init; }
    public DateTime? StartsAt { get; init; }
    public bool RequiresApproval { get; init; } = true;
}

/// <summary>
/// Command to approve JIT elevation
/// </summary>
public class ApproveJitElevationCommand : IRequest<JitElevationRequest>
{
    public Guid RequestId { get; init; }
    public Guid ReviewerId { get; init; }
    public string? Comments { get; init; }
}

/// <summary>
/// Command to deny JIT elevation
/// </summary>
public class DenyJitElevationCommand : IRequest<JitElevationRequest>
{
    public Guid RequestId { get; init; }
    public Guid ReviewerId { get; init; }
    public string? Comments { get; init; }
}

/// <summary>
/// Command to revoke JIT elevation
/// </summary>
public class RevokeJitElevationCommand : IRequest<bool>
{
    public Guid RequestId { get; init; }
    public Guid ReviewerId { get; init; }
    public string? Reason { get; init; }
}
