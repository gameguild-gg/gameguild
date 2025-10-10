using GameGuild.CQRS;
using Microsoft.Extensions.Logging;
using GameGuild.Core;
using GameGuild.Modules.Users.Services;

namespace GameGuild.Modules.Users.Commands;

/// <summary>
/// Handler for ExportUserDataCommand
/// </summary>
public sealed class ExportUserDataCommandHandler : IRequestHandler<ExportUserDataCommand, Result<Guid>>
{
    private readonly IGdprService _gdprService;
    private readonly ILogger<ExportUserDataCommandHandler> _logger;

    public ExportUserDataCommandHandler(
        IGdprService gdprService,
        ILogger<ExportUserDataCommandHandler> logger)
    {
        _gdprService = gdprService;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(ExportUserDataCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing GDPR data export request for user {UserId}", request.UserId);

            var export = await _gdprService.CreateExportRequestAsync(
                request.UserId,
                request.Format,
                request.IncludeMetadata,
                cancellationToken);

            _logger.LogInformation("Created GDPR export request {ExportId} for user {UserId}", export.Id, request.UserId);

            return Result<Guid>.Success(export.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create GDPR export for user {UserId}", request.UserId);
            return Result<Guid>.Failure($"Failed to create data export: {ex.Message}");
        }
    }
}

/// <summary>
/// Handler for DeleteUserDataCommand (Right to be Forgotten)
/// </summary>
public sealed class DeleteUserDataCommandHandler : IRequestHandler<DeleteUserDataCommand, Result<bool>>
{
    private readonly IGdprService _gdprService;
    private readonly ILogger<DeleteUserDataCommandHandler> _logger;

    public DeleteUserDataCommandHandler(
        IGdprService gdprService,
        ILogger<DeleteUserDataCommandHandler> logger)
    {
        _gdprService = gdprService;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(DeleteUserDataCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogWarning("Processing GDPR {Action} request for user {UserId}. Reason: {Reason}",
                request.AnonymizeInstead ? "anonymization" : "deletion", request.UserId, request.Reason);

            var success = await _gdprService.DeleteUserDataAsync(
                request.UserId,
                request.Reason,
                request.AnonymizeInstead,
                cancellationToken);

            if (success)
            {
                _logger.LogInformation("Successfully processed GDPR {Action} for user {UserId}",
                    request.AnonymizeInstead ? "anonymization" : "deletion", request.UserId);
                return Result<bool>.Success(true);
            }

            _logger.LogWarning("User {UserId} not found for GDPR {Action}",
                request.UserId, request.AnonymizeInstead ? "anonymization" : "deletion");
            return Result<bool>.Failure("User not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process GDPR {Action} for user {UserId}",
                request.AnonymizeInstead ? "anonymization" : "deletion", request.UserId);
            return Result<bool>.Failure($"Failed to process request: {ex.Message}");
        }
    }
}

/// <summary>
/// Handler for GetDataExportStatusQuery
/// </summary>
public sealed class GetDataExportStatusQueryHandler : IRequestHandler<GetDataExportStatusQuery, Result<DataExportStatusDto>>
{
    private readonly IGdprService _gdprService;
    private readonly ILogger<GetDataExportStatusQueryHandler> _logger;

    public GetDataExportStatusQueryHandler(
        IGdprService gdprService,
        ILogger<GetDataExportStatusQueryHandler> logger)
    {
        _gdprService = gdprService;
        _logger = logger;
    }

    public async Task<Result<DataExportStatusDto>> Handle(GetDataExportStatusQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var export = await _gdprService.GetExportStatusAsync(request.ExportId, cancellationToken);

            if (export == null)
            {
                _logger.LogWarning("Export {ExportId} not found", request.ExportId);
                return Result<DataExportStatusDto>.Failure("Export not found");
            }

            var dto = new DataExportStatusDto(
                export.Id,
                export.UserId,
                export.Status,
                export.RequestedAt,
                export.CompletedAt,
                export.ExportFilePath,
                export.FileSizeBytes,
                export.Format,
                export.ExpiresAt,
                export.ErrorMessage,
                export.EntityCount
            );

            return Result<DataExportStatusDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get export status for {ExportId}", request.ExportId);
            return Result<DataExportStatusDto>.Failure($"Failed to get export status: {ex.Message}");
        }
    }
}
