using Microsoft.AspNetCore.Mvc.Filters;

namespace GameGuild.API.Authorization;

/// <summary>
///     Marker attribute to specify required permissions on controllers or actions.
///     Used in conjunction with PermissionAuthorizationFilter to enforce permission-based access control.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequiresPermissionAttribute : Attribute, IFilterMetadata
{
    /// <summary>
    ///     Creates a new RequiresPermission attribute with the specified permission name
    /// </summary>
    /// <param name="name">The permission name required</param>
    public RequiresPermissionAttribute(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Permission name cannot be null or whitespace.", nameof(name));

        Name = name;
    }

    /// <summary>
    ///     The permission name required to access the resource (e.g., "Admin.Dashboard", "Users.Read")
    /// </summary>
    public string Name { get; }
}
