using System.Security.Claims;
using System.Text.Encodings.Web;
using GameGuild.API.Database;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace GameGuild.API.UnitTests.Architecture;

/// <summary>
///     Host fixture for the E9 route-smoke guard: the SAME host composition recipe
///     as <see cref="WiringGapHostFixture"/> (real Program + product composition,
///     in-memory database — NOT a PostgreSQL-bound fixture), plus a synthetic
///     authentication scheme so the guard can exercise [Authorize] routes with a
///     principal. This mirrors the canonical integration-fixture pattern
///     (<c>ApiPostgreSqlFixture.ApiPostgreSqlTestAuthHandler</c>) without its
///     PostgreSQL container. Rate limiting is disabled so a full-surface sweep is
///     not tripped into 429s that would mask the wiring signal (5xx).
/// </summary>
public sealed class RouteSmokeHostFixture : WiringGapHostFixture
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["PresentationLayer:EnableRateLimiting"] = "false",
            }));
        builder.ConfigureTestServices(services =>
        {
            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = SyntheticPrincipalAuthenticationHandler.SchemeName;
                    options.DefaultChallengeScheme = SyntheticPrincipalAuthenticationHandler.SchemeName;
                    options.DefaultScheme = SyntheticPrincipalAuthenticationHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, SyntheticPrincipalAuthenticationHandler>(
                    SyntheticPrincipalAuthenticationHandler.SchemeName,
                    _ => { });
        });
    }
}

/// <summary>
///     Authenticates any request that carries an <c>Authorization</c> header and the
///     synthetic actor headers, with the same claim shape the integration test
///     handler produces (sub / tenant_id / tid).
/// </summary>
internal sealed class SyntheticPrincipalAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "RouteSmokeSynthetic";
    public const string UserIdHeader = "X-Test-User-Id";
    public const string TenantIdHeader = "X-Test-Tenant-Id";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey("Authorization"))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (!Guid.TryParse(Request.Headers[UserIdHeader].FirstOrDefault(), out var userId) ||
            !Guid.TryParse(Request.Headers[TenantIdHeader].FirstOrDefault(), out var tenantId))
        {
            return Task.FromResult(AuthenticateResult.Fail("The synthetic actor headers are invalid."));
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new("sub", userId.ToString()),
            new(ClaimTypes.Name, $"Route smoke actor {userId:N}"),
            new("tenant_id", tenantId.ToString()),
            new("tid", tenantId.ToString()),
            new(ClaimTypes.Role, "SystemAdmin"),
        };

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));
        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(principal, SchemeName)));
    }
}

[CollectionDefinition(Name)]
public sealed class RouteSmokeHostCollection : ICollectionFixture<RouteSmokeHostFixture>
{
    public const string Name = "RouteSmokeHost";
}
