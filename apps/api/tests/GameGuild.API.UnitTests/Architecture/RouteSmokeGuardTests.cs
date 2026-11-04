using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GameGuild.API.UnitTests.Architecture;

/// <summary>
///     Execution guard E9 — every advertised route must survive a smoke request.
///     Issue #1203 found 31 of 177 authorization endpoints dead while
///     IMediator-mocked unit tests kept CI green: nothing ever exercised the real
///     route → handler wiring. This guard boots the real architecture-test host
///     (same composition as <see cref="WiringGapHostFixture"/> — in-memory store,
///     NOT a PostgreSQL-bound fixture), enumerates EVERY route from
///     <see cref="EndpointDataSource"/>, and requests each one twice — anonymous
///     and with a synthetic principal. 401/403/400/422/404 etc. are acceptable
///     per route class (auth gates, validation, planned-unavailable contracts);
///     ANY 5xx (or transport failure/timeout) is a wiring gap: dead handler,
///     missing DI registration, or resolve-time explosion — the 15×500 class.
///     Route classes that need special setup (multipart signatures, provider
///     webhooks, readiness probes) are allowlisted below with comments; genuine
///     5xx findings are ratcheted in wiring-gap-baseline.txt as <c>E9|</c> entries.
/// </summary>
[Collection(RouteSmokeHostCollection.Name)]
public sealed class RouteSmokeGuardTests(RouteSmokeHostFixture host)
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task EveryHandlerBoundRoute_AnswersWithout5xx_AnonymousAndSyntheticPrincipal()
    {
        var dataSource = host.Services.GetRequiredService<EndpointDataSource>();
        var routes = dataSource.Endpoints
            .OfType<RouteEndpoint>()
            .Select(endpoint => new
            {
                Method = endpoint.Metadata.OfType<HttpMethodMetadata>().FirstOrDefault()?.HttpMethods.FirstOrDefault(),
                endpoint.RoutePattern,
            })
            .Where(route => route.Method is not null) // non-HTTP endpoints (SignalR hubs / sockets) are not requestable
            .Select(route => (route.Method!, MaterializePath(route.RoutePattern)))
            .Distinct()
            .Where(route => !ExcludedRouteClasses.IsMatch(route.Item1, route.Item2))
            .OrderBy(route => route.Item2, StringComparer.Ordinal)
            .ThenBy(route => route.Item1, StringComparer.Ordinal)
            .ToList();

        routes.Should().HaveCountGreaterThanOrEqualTo(400,
            "the routed API surface is large; a much smaller count means endpoint enumeration is broken");

        using var client = host.CreateClient();
        client.Timeout = RequestTimeout;

        var violations = new List<string>();
        foreach (var (method, path) in routes)
        {
            foreach (var principalPass in new[] { "anon", "authed" })
            {
                using var request = new HttpRequestMessage(new HttpMethod(method), path);
                if (principalPass == "authed")
                {
                    request.Headers.Add("Authorization", "Bearer route-smoke");
                    request.Headers.Add(SyntheticPrincipalAuthenticationHandler.UserIdHeader, Guid.NewGuid().ToString());
                    request.Headers.Add(SyntheticPrincipalAuthenticationHandler.TenantIdHeader, Guid.NewGuid().ToString());
                }

                if (method is "POST" or "PUT" or "PATCH")
                {
                    // An empty JSON object binds where a body is expected and is ignored where
                    // none is; model-binding failures answer 400/415, which are acceptable.
                    request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
                }

                HttpStatusCode? statusCode;
                string failureDetail;
                try
                {
                    using var response = await client.SendAsync(request);
                    statusCode = response.StatusCode;
                    failureDetail = ((int)response.StatusCode).ToString();
                }
                catch (Exception exception) when (
                    exception is HttpRequestException or TaskCanceledException or OperationCanceledException)
                {
                    statusCode = null;
                    failureDetail = exception is TaskCanceledException or OperationCanceledException
                        ? "timeout"
                        : "transport:" + exception.GetType().Name;
                }

                var failed = statusCode is null || (int)statusCode >= 500;
                if (failed)
                {
                    violations.Add(WiringGapBaseline.Id(
                        WiringGapBaseline.GuardIds.RouteSmoke, $"{method} {path}|{principalPass}|{failureDetail}"));
                }
            }
        }

        WiringGapBaseline.AssertNoNewGaps(
            WiringGapBaseline.GuardIds.RouteSmoke,
            "every handler-bound route enumerated from EndpointDataSource must answer the smoke request without a " +
            "5xx (or timeout/transport failure), both anonymous and with a synthetic principal — 401/403/400/422 are " +
            "acceptable per route class. A 5xx here means dead or mis-wired handler machinery (the 15x500 class of #1203)",
            violations);
        WiringGapBaseline.AssertNoStaleEntries(
            WiringGapBaseline.GuardIds.RouteSmoke, violations);
    }

    /// <summary>
    ///     Renders a concrete request path for a route pattern's raw template:
    ///     api-version parameters materialize as <c>v1</c>, id-like parameters as a
    ///     zero GUID, everything else as <c>1</c>. Route-parameter defaults are not
    ///     applied — the substitution only has to bind, never to find data.
    /// </summary>
    private static string MaterializePath(RoutePattern pattern)
    {
        var template = pattern.RawText ?? "/";
        var builder = new StringBuilder();
        foreach (var segment in template.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            // Catch-all segments ("{**rest}") add nothing requestable.
            var materialized = Regex.Replace(segment, @"\{(\*\*)?(?<name>[A-Za-z][A-Za-z0-9]*)(?::[^}]*)?\??\}", match =>
            {
                var name = match.Groups["name"].Value;
                if (name.Equals("version", StringComparison.OrdinalIgnoreCase))
                {
                    return "1";
                }

                return name.Contains("id", StringComparison.OrdinalIgnoreCase)
                    ? "00000000-0000-0000-0000-000000000000"
                    : "1";
            });
            if (materialized.Length == 0 || materialized == "**")
            {
                continue;
            }

            builder.Append('/').Append(materialized);
        }

        return builder.Length == 0 ? "/" : builder.ToString();
    }

    /// <summary>
    ///     Route classes that need setup a wiring smoke cannot provide. Every entry
    ///     needs a comment; the class check is a prefix/method match so it stays
    ///     coarse on purpose.
    /// </summary>
    private static class ExcludedRouteClasses
    {
        private static readonly (string Prefix, string Comment)[] Prefixes =
        [
            // Health/readiness probes DELIBERATELY answer 503 when a readiness check
            // (payment provider, economy capability, billing inbox) is not ready —
            // in the smoke host those external dependencies are absent by design, so
            // 503 here is availability reporting, not a wiring gap.
            ("/health", "readiness probes report 503 by design"),
            ("/api/health", "readiness probes report 503 by design"),
        ];

        public static bool IsMatch(string method, string path)
        {
            foreach (var (prefix, _) in Prefixes)
            {
                if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
