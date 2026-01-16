using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Identity.Context.Actors;

/// <summary>
///     Extension methods for tenant access validation in controllers.
///     Provides DRY tenant validation logic to prevent cross-tenant attacks.
/// </summary>
public static class TenantValidationExtensions
{
    /// <summary>
    ///     Validates that the authenticated user has access to the specified tenant.
    ///     This prevents cross-tenant attacks where a malicious user crafts requests with another tenant's ID.
    /// </summary>
    /// <param name="actorContextAccessor">The actor context accessor</param>
    /// <param name="requestedTenantId">The TenantId from the request body</param>
    /// <param name="operation">Description of the operation for error messages</param>
    /// <returns>A validation result indicating success or failure with error details</returns>
    public static TenantValidationResult ValidateTenantAccess(
        this IActorContextAccessor actorContextAccessor,
        Guid requestedTenantId,
        string operation)
    {
        var actorContext = actorContextAccessor.ActorContext;

        // Allow anonymous access only in development/testing (controlled by AllowAnonymous attribute)
        // For authenticated requests, validate tenant access
        if (!actorContext.IsAuthenticated)
        {
            return TenantValidationResult.Success();
        }

        // User must have a tenant context
        if (!actorContext.TenantId.HasValue)
        {
            return TenantValidationResult.Forbidden(
                $"User is not associated with any tenant for {operation}");
        }

        // Request TenantId must match authenticated user's tenant
        if (actorContext.TenantId.Value != requestedTenantId)
        {
            return TenantValidationResult.CrossTenantDenied(
                actorContext.TenantId.Value,
                requestedTenantId,
                operation);
        }

        return TenantValidationResult.Success();
    }

    /// <summary>
    ///     Validates tenant access and returns an IActionResult if validation fails.
    ///     This is a convenience method for controllers that need to return early on validation failure.
    /// </summary>
    /// <param name="actorContextAccessor">The actor context accessor</param>
    /// <param name="requestedTenantId">The TenantId from the request body</param>
    /// <param name="operation">Description of the operation for error messages</param>
    /// <returns>An error response if validation fails, null if validation passes</returns>
    public static IActionResult? ValidateTenantAccessAsActionResult(
        this IActorContextAccessor actorContextAccessor,
        Guid requestedTenantId,
        string operation)
    {
        var result = actorContextAccessor.ValidateTenantAccess(requestedTenantId, operation);
        return result.ToActionResult();
    }
}

/// <summary>
///     Result of tenant access validation.
/// </summary>
public sealed class TenantValidationResult
{
    private TenantValidationResult(bool isValid, string? errorMessage, int? statusCode, object? errorDetails)
    {
        IsValid = isValid;
        ErrorMessage = errorMessage;
        StatusCode = statusCode;
        ErrorDetails = errorDetails;
    }

    /// <summary>
    ///     Whether the validation passed.
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    ///     The error message if validation failed.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    ///     The HTTP status code to return if validation failed.
    /// </summary>
    public int? StatusCode { get; }

    /// <summary>
    ///     Additional error details to include in the response.
    /// </summary>
    public object? ErrorDetails { get; }

    /// <summary>
    ///     Creates a successful validation result.
    /// </summary>
    public static TenantValidationResult Success() => new(true, null, null, null);

    /// <summary>
    ///     Creates a forbidden validation result (user has no tenant context).
    /// </summary>
    public static TenantValidationResult Forbidden(string message) =>
        new(false, message, StatusCodes.Status403Forbidden, null);

    /// <summary>
    ///     Creates a cross-tenant denied validation result.
    /// </summary>
    public static TenantValidationResult CrossTenantDenied(
        Guid userTenantId,
        Guid requestedTenantId,
        string operation) =>
        new(false,
            $"User belongs to tenant {userTenantId} but attempted to {operation} for tenant {requestedTenantId}",
            StatusCodes.Status403Forbidden,
            new
            {
                error = "Cross-tenant access denied",
                message = $"User belongs to tenant {userTenantId} but attempted to {operation} for tenant {requestedTenantId}",
                code = "TENANT_MISMATCH"
            });

    /// <summary>
    ///     Converts this result to an IActionResult if validation failed.
    /// </summary>
    /// <returns>An error response if validation fails, null if validation passes</returns>
    public IActionResult? ToActionResult()
    {
        if (IsValid)
            return null;

        if (ErrorDetails != null)
        {
            return new ObjectResult(ErrorDetails)
            {
                StatusCode = StatusCode
            };
        }

        return new ObjectResult(new { error = ErrorMessage })
        {
            StatusCode = StatusCode
        };
    }
}
