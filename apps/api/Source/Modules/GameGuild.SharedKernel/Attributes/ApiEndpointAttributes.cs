using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Attributes;

/// <summary>
/// Marks an endpoint as publicly accessible without authentication.
/// Use this in conjunction with [AllowAnonymous] for clear documentation.
/// </summary>
/// <remarks>
/// This attribute serves as documentation and can be used by OpenAPI generators
/// to clearly indicate which endpoints are public. Always pair with [AllowAnonymous].
/// </remarks>
/// <example>
/// [HttpGet]
/// [PublicEndpoint("Returns publicly available course catalog")]
/// [AllowAnonymous]
/// public async Task&lt;ActionResult&gt; GetPublicCourses() { ... }
/// </example>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class PublicEndpointAttribute : Attribute
{
    /// <summary>
    /// Description of why this endpoint is public
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Whether the endpoint returns different data for authenticated vs anonymous users
    /// </summary>
    public bool EnhancedForAuthenticated { get; set; }

    /// <summary>
    /// Rate limit category for anonymous access (e.g., "standard", "limited", "generous")
    /// </summary>
    public string? RateLimitCategory { get; set; }

    public PublicEndpointAttribute(string description = "Public endpoint - no authentication required")
    {
        Description = description;
    }
}

/// <summary>
/// Marks an endpoint as requiring authentication.
/// Use this for documentation when endpoints require a valid JWT token.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class AuthenticatedEndpointAttribute : Attribute
{
    /// <summary>
    /// Description of the authentication requirement
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Required permission codes (for reference/documentation)
    /// </summary>
    public string[]? RequiredPermissions { get; set; }

    /// <summary>
    /// Whether the endpoint requires tenant context
    /// </summary>
    public bool RequiresTenant { get; set; } = true;

    public AuthenticatedEndpointAttribute(string description = "Requires valid authentication token")
    {
        Description = description;
    }
}

/// <summary>
/// Produces standardized API response types for OpenAPI documentation.
/// Extends ProducesResponseTypeAttribute with common error responses.
/// </summary>
public class ApiResponseAttribute : ProducesResponseTypeAttribute
{
    public ApiResponseAttribute(Type type, int statusCode) : base(type, statusCode) { }
    
    public ApiResponseAttribute(int statusCode) : base(statusCode) { }
}

/// <summary>
/// Marks an endpoint as supporting idempotency via Idempotency-Key header.
/// Clients should include this header for safe retries on network failures.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class IdempotentAttribute : Attribute
{
    /// <summary>
    /// How long the idempotency key should be valid
    /// </summary>
    public int CacheDurationMinutes { get; set; } = 1440; // 24 hours

    /// <summary>
    /// Description of idempotency behavior
    /// </summary>
    public string Description { get; }

    public IdempotentAttribute(string description = "Supports Idempotency-Key header for safe retries")
    {
        Description = description;
    }
}
