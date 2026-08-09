using System.Security.Claims;
using FluentAssertions;
using GameGuild.Configuration.PresentationLayer.Authorization;
using GameGuild.Identity.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace GameGuild.Identity.Authorization.UnitTests.Services;

public sealed class AuthorizationTenantResolverTests
{
    [Fact]
    public async Task ResolveTenantIdAsync_WhenTenantMiddlewareResolvedTenant_UsesResolvedTenantBeforeClaims()
    {
        // Given
        var middlewareTenantId = Guid.NewGuid();
        var tokenTenantId = Guid.NewGuid();
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("tenant_id", tokenTenantId.ToString())],
                authenticationType: "Bearer"))
        };
        context.Items[HttpContextKeys.AuthorizationTenantId] = middlewareTenantId;
        IAuthorizationTenantResolver resolver = new AuthorizationTenantResolver(
            Options.Create(new TenancyOptions()),
            Options.Create(new AuthorizationTokenOptions()));

        // When
        var resolvedTenantId = await resolver.ResolveTenantIdAsync(context);

        // Then
        resolvedTenantId.Should().Be(middlewareTenantId.ToString());
    }

    [Fact]
    public async Task ResolveTenantIdAsync_WhenRequestHasNoTenantSource_UsesAuthenticatedTenantClaim()
    {
        // Given
        var tenantId = Guid.NewGuid();
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("tenant_id", tenantId.ToString())],
                authenticationType: "Bearer"))
        };
        IAuthorizationTenantResolver resolver = new AuthorizationTenantResolver(
            Options.Create(new TenancyOptions()),
            Options.Create(new AuthorizationTokenOptions()));

        // When
        var resolvedTenantId = await resolver.ResolveTenantIdAsync(context);

        // Then
        resolvedTenantId.Should().Be(tenantId.ToString());
    }
}
