using System.Security.Claims;
using GameGuild.Identity.Context.Actors;

namespace GameGuild.API.Context;

/// <summary>
///     Bridges the shared module request-context contract to the HTTP actor context.
/// </summary>
public sealed class RequestContextAccessor(
    IActorContextAccessor actorContextAccessor,
    IHttpContextAccessor httpContextAccessor) : IRequestContextAccessor
{
    private static readonly string[] UserIdClaimTypes =
    [
        ClaimTypes.NameIdentifier,
        "sub",
        "user_id",
        "userId",
        "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"
    ];

    private static readonly string[] TenantIdClaimTypes =
    [
        "tenant",
        "tenant_id",
        "tenantId",
        "TenantId"
    ];

    public Guid? CurrentUserId =>
        actorContextAccessor.ActorContext.SubjectIdAsGuid ?? TryGetClaimGuid(UserIdClaimTypes);

    public Guid? CurrentTenantId =>
        actorContextAccessor.ActorContext.TenantId ??
        TryGetClaimGuid(TenantIdClaimTypes) ??
        TryGetHeaderGuid("X-Tenant-Id");

    public bool IsAuthenticated =>
        actorContextAccessor.ActorContext.IsAuthenticated ||
        httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;

    public bool HasTenantContext => CurrentTenantId.HasValue;

    public Task<UserInfo?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var userId = CurrentUserId;
        if (!userId.HasValue || !IsAuthenticated)
        {
            return Task.FromResult<UserInfo?>(null);
        }

        var actor = actorContextAccessor.ActorContext;
        var principal = httpContextAccessor.HttpContext?.User;
        var email = actor.GetAttribute("email")
                    ?? principal?.FindFirstValue(ClaimTypes.Email)
                    ?? principal?.FindFirstValue("email")
                    ?? string.Empty;
        var name = actor.GetAttribute("name")
                   ?? principal?.FindFirstValue(ClaimTypes.Name)
                   ?? principal?.Identity?.Name
                   ?? email;

        return Task.FromResult<UserInfo?>(new UserInfo(userId.Value, email, name, true, CurrentTenantId));
    }

    public Task<TenantInfo?> GetCurrentTenantAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var tenantId = CurrentTenantId;
        if (!tenantId.HasValue)
        {
            return Task.FromResult<TenantInfo?>(null);
        }

        var tenantName = TryGetClaimValue("tenant_name", "tenantName", "TenantName")
                         ?? TryGetHeaderValue("X-Tenant-Name")
                         ?? "Current tenant";
        var tenantSlug = TryGetClaimValue("tenant_slug", "tenantSlug", "TenantSlug")
                         ?? TryGetHeaderValue("X-Tenant-Slug")
                         ?? tenantId.Value.ToString("D");

        return Task.FromResult<TenantInfo?>(new TenantInfo(tenantId.Value, tenantName, tenantSlug, true));
    }

    private Guid? TryGetClaimGuid(params string[] claimTypes)
    {
        var value = TryGetClaimValue(claimTypes);
        return Guid.TryParse(value, out var guid) ? guid : null;
    }

    private string? TryGetClaimValue(params string[] claimTypes)
    {
        var principal = httpContextAccessor.HttpContext?.User;
        if (principal is null)
        {
            return null;
        }

        foreach (var claimType in claimTypes)
        {
            var value = principal.FindFirstValue(claimType);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private Guid? TryGetHeaderGuid(string headerName)
    {
        var value = TryGetHeaderValue(headerName);
        return Guid.TryParse(value, out var guid) ? guid : null;
    }

    private string? TryGetHeaderValue(string headerName)
    {
        if (httpContextAccessor.HttpContext?.Request.Headers.TryGetValue(headerName, out var values) != true)
        {
            return null;
        }

        var value = values.FirstOrDefault();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
