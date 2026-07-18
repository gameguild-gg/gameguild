using GameGuild.Identity.Authorization;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace GameGuild.Commerce.Payments.IntegrationTests;

internal sealed class TestAuthorizationTenantResolver : IAuthorizationTenantResolver
{
    public string? ResolveFromRequest(HttpContext context)
    {
        if (context.Request.Headers.ContainsKey("X-Test-No-Tenant")) return null;
        return context.Request.Headers.TryGetValue("X-Test-Tenant", out var values)
            ? values.ToString()
            : TestAuthHandler.DefaultTenantId.ToString();
    }

    public string? ResolveFromClaims(ClaimsPrincipal principal) =>
        principal.FindFirstValue("tenant_id");

    public string? GetUserDefaultTenant(ClaimsPrincipal principal) =>
        principal.FindFirstValue("tenant_id");

    public Task<string?> ResolveTenantIdAsync(
        HttpContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ResolveFromRequest(context));
}
