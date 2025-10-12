using GameGuild.CQRS;

namespace GameGuild.Modules.Tenants;

/// <summary> Command to bulk activate multiple tenants </summary>
public class BulkActivateTenantsCommand : ICommand<Result<BulkOperationResult>>, IAuthorizedRequest
{
    public IEnumerable<Guid> TenantIds { get; init; } = Enumerable.Empty<Guid>();
    public string[ ]? RequiredRoles { get; } = null;
    public string[ ]? RequiredPermissions { get; } = ["BULK_MODIFY"];
}

/// <summary> Command to bulk deactivate multiple tenants </summary>
public class BulkDeactivateTenantsCommand : ICommand<Result<BulkOperationResult>>, IAuthorizedRequest
{
    public IEnumerable<Guid> TenantIds { get; init; } = Enumerable.Empty<Guid>();
    public string[ ]? RequiredRoles { get; } = null;
    public string[ ]? RequiredPermissions { get; } = ["BULK_MODIFY"];
}

/// <summary> Command to bulk archive multiple tenants </summary>
public class BulkArchiveTenantsCommand : ICommand<Result<BulkOperationResult>>, IAuthorizedRequest
{
    public IEnumerable<Guid> TenantIds { get; init; } = Enumerable.Empty<Guid>();
    public string Reason { get; init; } = string.Empty;
    public string[ ]? RequiredRoles { get; } = null;
    public string[ ]? RequiredPermissions { get; } = ["BULK_MODIFY"];
}

/// <summary> Command to bulk delete multiple tenants </summary>
public class BulkDeleteTenantsCommand : ICommand<Result<BulkOperationResult>>, IAuthorizedRequest
{
    public IEnumerable<Guid> TenantIds { get; init; } = Enumerable.Empty<Guid>();
    public bool HardDelete { get; init; } = false;
    public string[ ]? RequiredRoles { get; } = null;
    public string[ ]? RequiredPermissions { get; } = ["BULK_DELETE"];
}

/// <summary> Command to bulk update tenant settings </summary>
public class BulkUpdateTenantSettingsCommand : ICommand<Result<BulkOperationResult>>, IAuthorizedRequest
{
    public IEnumerable<Guid> TenantIds { get; init; } = Enumerable.Empty<Guid>();
    public string? DefaultLanguage { get; init; }
    public string? DefaultTimezone { get; init; }
    public string? DefaultCurrency { get; init; }
    public bool? AllowUserRegistration { get; init; }
    public bool? RequireRegistrationApproval { get; init; }
    public bool? RequireTwoFactorAuth { get; init; }
    public bool? EnableAuditLogging { get; init; }
    public bool? EnableApiAccess { get; init; }
    public string[ ]? RequiredRoles { get; } = null;
    public string[ ]? RequiredPermissions { get; } = ["BULK_MODIFY"];
}

/// <summary> Result of bulk operations </summary>
public class BulkOperationResult
{
    public int TotalRequested { get; init; }
    public int SuccessfulOperations { get; init; }
    public int FailedOperations { get; init; }
    public IEnumerable<BulkOperationError> Errors { get; init; } = Enumerable.Empty<BulkOperationError>();
    public bool IsComplete => FailedOperations == 0;
    public double SuccessRate => TotalRequested > 0 ? (double)SuccessfulOperations / TotalRequested : 0;
}

/// <summary> Error details for failed bulk operations </summary>
public class BulkOperationError
{
    public Guid TenantId { get; init; }
    public string TenantName { get; init; } = string.Empty;
    public string ErrorMessage { get; init; } = string.Empty;
    public string ErrorCode { get; init; } = string.Empty;
}