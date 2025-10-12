using GameGuild.CQRS;

namespace GameGuild.Modules.Tenants;

/// <summary> Command to export tenant data </summary>
public class ExportTenantDataCommand : ICommand<Result<TenantExportResult>>, IAuthorizedRequest
{
    public Guid TenantId { get; init; }
    public ExportFormat Format { get; init; } = ExportFormat.Json;
    public bool IncludeMembers { get; init; } = true;
    public bool IncludeSettings { get; init; } = true;
    public bool IncludeDomains { get; init; } = true;
    public bool IncludeStatistics { get; init; } = false;
    public string[ ]? RequiredRoles { get; } = null;
    public string[ ]? RequiredPermissions { get; } = ["EXPORT"];
}

/// <summary> Command to import tenant data </summary>
public class ImportTenantDataCommand : ICommand<Result<TenantImportResult>>, IAuthorizedRequest
{
    public string ImportData { get; init; } = string.Empty;
    public ExportFormat Format { get; init; } = ExportFormat.Json;
    public bool OverwriteExisting { get; init; } = false;
    public bool ValidateOnly { get; init; } = false;
    public string[ ]? RequiredRoles { get; } = null;
    public string[ ]? RequiredPermissions { get; } = ["IMPORT"];
}

/// <summary> Command to bulk export multiple tenants </summary>
public class BulkExportTenantsCommand : ICommand<Result<BulkExportResult>>, IAuthorizedRequest
{
    public IEnumerable<Guid> TenantIds { get; init; } = Enumerable.Empty<Guid>();
    public ExportFormat Format { get; init; } = ExportFormat.Json;
    public bool IncludeMembers { get; init; } = true;
    public bool IncludeSettings { get; init; } = true;
    public bool IncludeDomains { get; init; } = true;
    public bool IncludeStatistics { get; init; } = false;
    public string[ ]? RequiredRoles { get; } = null;
    public string[ ]? RequiredPermissions { get; } = ["BULK_EXPORT"];
}

/// <summary> Export format options </summary>
public enum ExportFormat
{
    Json,
    Xml,
    Csv,
    Excel
}

/// <summary> Result of tenant export operation </summary>
public class TenantExportResult
{
    public Guid TenantId { get; init; }
    public string TenantName { get; init; } = string.Empty;
    public string ExportedData { get; init; } = string.Empty;
    public ExportFormat Format { get; init; }
    public DateTime ExportedAt { get; init; }
    public long DataSize { get; init; }
    public string? DownloadUrl { get; init; }
}

/// <summary> Result of tenant import operation </summary>
public class TenantImportResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public int TenantsProcessed { get; init; }
    public int TenantsImported { get; init; }
    public int TenantsSkipped { get; init; }
    public int TenantsUpdated { get; init; }
    public IEnumerable<ImportValidationError> ValidationErrors { get; init; } = Enumerable.Empty<ImportValidationError>();
    public IEnumerable<Guid> ImportedTenantIds { get; init; } = Enumerable.Empty<Guid>();
}

/// <summary> Result of bulk export operation </summary>
public class BulkExportResult
{
    public int TotalRequested { get; init; }
    public int SuccessfulExports { get; init; }
    public int FailedExports { get; init; }
    public string? ArchiveUrl { get; init; }
    public long TotalSize { get; init; }
    public IEnumerable<BulkOperationError> Errors { get; init; } = Enumerable.Empty<BulkOperationError>();
}

/// <summary> Import validation error </summary>
public class ImportValidationError
{
    public string Field { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? Value { get; init; }
    public int? LineNumber { get; init; }
}