using Microsoft.AspNetCore.Http;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     HTTP context-based implementation of authorization tenant context.
/// </summary>
public sealed class HttpAuthorizationTenantContext : IAuthorizationTenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    ///     Initializes a new instance of <see cref="HttpAuthorizationTenantContext"/>.
    /// </summary>
    public HttpAuthorizationTenantContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public string? TenantId
    {
        get => _httpContextAccessor.HttpContext?.Items[HttpContextKeys.AuthorizationTenantId] as string;
        set
        {
            if (_httpContextAccessor.HttpContext is not null)
            {
                _httpContextAccessor.HttpContext.Items[HttpContextKeys.AuthorizationTenantId] = value;
            }
        }
    }

    /// <summary>
    ///     Sets the tenant ID for the current request.
    /// </summary>
    /// <param name="tenantId">The tenant ID to set.</param>
    public void SetTenantId(string? tenantId) => TenantId = tenantId;
}
