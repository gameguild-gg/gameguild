using GameGuild;
using GameGuild.CQRS;

namespace GameGuild.Modules.ErrorTracking.Commands;

/// <summary>
/// Command to capture an error event.
/// </summary>
public record CaptureErrorCommand(
    Guid? TenantId,
    string ExceptionType,
    string Message,
    string? StackTrace,
    string Severity,
    string Environment,
    string? Release,
    Guid? UserId,
    string? Url,
    string? HttpMethod,
    string? UserAgent,
    string? IpAddress,
    Dictionary<string, string>? Tags,
    Dictionary<string, object>? ContextData,
    List<Dictionary<string, object>>? Breadcrumbs
) : IRequest<Result<Guid>>;
