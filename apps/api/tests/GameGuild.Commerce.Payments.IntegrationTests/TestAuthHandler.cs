using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.Commerce.Payments.IntegrationTests;

public sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";
    public static readonly Guid DefaultUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid DefaultTenantId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (Request.Headers.ContainsKey("X-Test-Unauthenticated"))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var subjectId = Request.Headers.TryGetValue("X-Test-Subject", out var subjectValues)
            ? subjectValues.ToString()
            : DefaultUserId.ToString();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, subjectId),
            new Claim(ClaimTypes.Name, "Integration Test User"),
        };

        if (!Request.Headers.ContainsKey("X-Test-No-Tenant"))
        {
            var tenantId = Request.Headers.TryGetValue("X-Test-Tenant", out var tenantValues)
                ? tenantValues.ToString()
                : DefaultTenantId.ToString();
            claims.Add(new Claim("tenant_id", tenantId));
        }

        foreach (var role in ReadHeaderValues("X-Test-Roles"))
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        foreach (var permission in ReadHeaderValues("X-Test-Permissions"))
        {
            claims.Add(new Claim("permission", permission));
        }

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));

        IEnumerable<string> ReadHeaderValues(string headerName)
        {
            if (!Request.Headers.TryGetValue(headerName, out var values)) return [];

            return values
                .SelectMany(value => value?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [])
                .Where(value => !string.IsNullOrWhiteSpace(value));
        }
    }
}
