using GameGuild.CQRS;
using GameGuild.Core;

namespace GameGuild.Modules.Users.Commands;

/// <summary>
/// Command to export all personal data for a user (GDPR right to data portability)
/// </summary>
public sealed record ExportUserDataCommand(
    Guid UserId,
    string Format = "JSON",
    bool IncludeMetadata = true
) : IRequest<Result<Guid>>;

/// <summary>
/// Command to delete all personal data for a user (GDPR right to be forgotten)
/// </summary>
public sealed record DeleteUserDataCommand(
    Guid UserId,
    string Reason,
    bool AnonymizeInstead = false
) : IRequest<Result<bool>>;

/// <summary>
/// Query to get personal data export status
/// </summary>
public sealed record GetDataExportStatusQuery(
    Guid ExportId
) : IRequest<Result<DataExportStatusDto>>;

/// <summary>
/// DTO for data export status
/// </summary>
public sealed record DataExportStatusDto(
    Guid Id,
    Guid UserId,
    DataExportStatus Status,
    DateTime RequestedAt,
    DateTime? CompletedAt,
    string? ExportFilePath,
    long? FileSizeBytes,
    string Format,
    DateTime? ExpiresAt,
    string? ErrorMessage,
    int EntityCount
);
