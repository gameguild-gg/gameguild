using GameGuild.Identity.Authentication;
using GameGuild.Identity.Authorization;

namespace GameGuild.Projects;

/// <summary> Stub: Effective permission for a resource </summary>
public class EffectivePermission {
    public Guid ResourceId { get; set; }
    public string ResourceType { get; set; } = string.Empty;
    public PermissionType[] Permissions { get; set; } = [];
    public bool IsOwner { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

/// <summary> Stub: Result of an invitation operation </summary>
public class InvitationResult {
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? InvitationId { get; set; }
}

/// <summary> Stub: Result of a permission update operation </summary>
public class PermissionUpdateResult {
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary> Stub: Result of a share operation </summary>
public class ShareResult {
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
}

/// <summary> Stub: Request to invite a user to a resource </summary>
public class InviteUserRequest {
    public string Email { get; set; } = string.Empty;
    public PermissionType[] Permissions { get; set; } = [];
    public DateTime? ExpiresAt { get; set; }
    public string? Message { get; set; }
    public bool RequireAcceptance { get; set; } = true;
}

/// <summary> Stub: Request to share a resource with multiple users </summary>
public class ShareResourceRequest {
    public string[] UserEmails { get; set; } = [];
    public Guid[] UserIds { get; set; } = [];
    public PermissionType[] Permissions { get; set; } = [];
    public DateTime? ExpiresAt { get; set; }
    public string? Message { get; set; }
    public bool RequireAcceptance { get; set; } = true;
    public bool NotifyUsers { get; set; } = true;
}

/// <summary> Stub: Permission resolver interface </summary>
public interface IPermissionResolver {
    Task<IEnumerable<EffectivePermission>> GetEffectivePermissionsAsync<T>(Guid userId, Guid? tenantId, Guid resourceId, string resourceType);
    Task<bool> CanGrantPermissionsAsync(Guid userId, Guid? tenantId, PermissionType[] permissions, Guid? resourceId = null);
}

/// <summary> Stub: Resource user info for permission listing </summary>
public class ResourceUserInfo {
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public PermissionType[] Permissions { get; set; } = [];
    public DateTime GrantedAt { get; set; }
    public string GrantedByUserName { get; set; } = string.Empty;
    public bool IsOwner { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

/// <summary> Stub: Resource permission service interface </summary>
public interface IResourcePermissionService {
    Task<IEnumerable<ResourceUserInfo>> GetResourceUsersAsync(string resourceType, Guid resourceId, Guid requestingUserId);
    Task<InvitationResult> InviteUserToResourceAsync(string resourceType, Guid resourceId, InviteUserRequest request, Guid invitingUserId);
    Task<PermissionUpdateResult> UpdateUserPermissionsAsync(string resourceType, Guid resourceId, Guid userId, PermissionType[] permissions, Guid updatingUserId, DateTime? expiresAt = null);
    Task<PermissionUpdateResult> RemoveUserAccessAsync(string resourceType, Guid resourceId, Guid userId, Guid removingUserId);
    Task<ShareResult> ShareResourceAsync(string resourceType, Guid resourceId, ShareResourceRequest request, Guid sharingUserId);
}

/// <summary> Stub: Attribute to require project-level permission </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class RequireProjectPermissionAttribute : Attribute {
    public PermissionType Permission { get; }
    
    public RequireProjectPermissionAttribute(PermissionType permission) {
        Permission = permission;
    }
}
