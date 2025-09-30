using GameGuild.Modules.Permissions;
using GameGuild.Modules.Permissions.Policies;

namespace GameGuild.Modules.Permissions.Factories;

/// <summary>
/// Factory for creating common permission policies
/// </summary>
public class PermissionPolicyFactory
{
    /// <summary>
    /// Create a basic authenticated user policy
    /// </summary>
    public static IPermissionPolicy CreateAuthenticatedUserPolicy()
    {
        return new PermissionPolicyBuilder()
            .RequireAuthenticated()
            .Build();
    }

    /// <summary>
    /// Create a tenant admin policy
    /// </summary>
    public static IPermissionPolicy CreateTenantAdminPolicy()
    {
        return new PermissionPolicyBuilder()
            .RequireAuthenticated()
            .RequireTenant()
            .RequireAnyPermission(PermissionType.Admin, PermissionType.Manage)
            .Build();
    }

    /// <summary>
    /// Create a content moderator policy
    /// </summary>
    public static IPermissionPolicy CreateContentModeratorPolicy()
    {
        return new PermissionPolicyBuilder()
            .RequireAuthenticated()
            .RequireTenant()
            .RequireAnyPermission(PermissionType.Review, PermissionType.Ban, PermissionType.Delete)
            .Build();
    }

    /// <summary>
    /// Create a resource owner policy
    /// </summary>
    public static IPermissionPolicy CreateResourceOwnerPolicy(string resourceIdParameter = "id")
    {
        return new PermissionPolicyBuilder()
            .RequireAuthenticated()
            .RequireTenant()
            .Build();
    }

    /// <summary>
    /// Create a read-only user policy
    /// </summary>
    public static IPermissionPolicy CreateReadOnlyUserPolicy()
    {
        return new PermissionPolicyBuilder()
            .RequireAuthenticated()
            .RequireTenant()
            .RequireAnyPermission(PermissionType.Read)
            .Build();
    }

    /// <summary>
    /// Create a custom policy with specific permissions
    /// </summary>
    public static IPermissionPolicy CreateCustomPolicy(
        PermissionType[] requiredPermissions,
        bool requireAllPermissions = false)
    {
        PermissionPolicyBuilder builder = new PermissionPolicyBuilder()
            .RequireAuthenticated()
            .RequireTenant();

        if (requireAllPermissions)
        {
            builder.RequireAllPermissions(requiredPermissions);
        }
        else
        {
            builder.RequireAnyPermission(requiredPermissions);
        }

        return builder.Build();
    }
}