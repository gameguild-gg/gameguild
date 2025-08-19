using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Common.Results;

/// <summary>
/// Represents a permission denied result with a standardized error response.
/// </summary>
public class PermissionDeniedResult : ObjectResult
{
    /// <summary>
    /// Initializes a new instance of the PermissionDeniedResult class with permission and resource type.
    /// </summary>
    /// <param name="permission">The required permission that was denied.</param>
    /// <param name="resourceType">The type of resource being accessed (default: "resource").</param>
    public PermissionDeniedResult(string permission, string resourceType = "resource")
        : base(new
        {
            error = "Permission denied",
            message = $"You do not have the required '{permission}' permission to access this {resourceType}.",
            statusCode = 403,
            timestamp = DateTime.UtcNow,
            details = new
            {
                requiredPermission = permission,
                resourceType = resourceType
            }
        })
    {
        StatusCode = 403;
    }

    /// <summary>
    /// Initializes a new instance of the PermissionDeniedResult class with a custom message.
    /// </summary>
    /// <param name="customMessage">The custom error message to display.</param>
    public PermissionDeniedResult(string customMessage)
        : base(new
        {
            error = "Permission denied",
            message = customMessage,
            statusCode = 403,
            timestamp = DateTime.UtcNow
        })
    {
        StatusCode = 403;
    }
}