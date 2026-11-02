using System.Diagnostics;
using System.Net;
using System.Text.Json;
using FluentAssertions;
using GameGuild.Configuration.PresentationLayer.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

/// <summary>
///     OAuth provider failure scenarios (issue #1125): the provider token/user endpoints can
///     return 5xx, hang until the HTTP timeout, or answer with a malformed / API-changed
///     payload. Every case must surface as a well-known exception type from the provider
///     layer instead of undefined behavior — the API edge maps these to error responses and
///     <see cref="OAuthAuthService" /> records the failed attempt, so a provider outage can
///     never half-complete a sign-in (no user, session, or token is created from a failed
///     provider exchange).
/// </summary>
public sealed class OAuthProviderErrorPathTests
{
    private readonly Mock<HttpMessageHandler> _handlerMock = new();
    private readonly Mock<IConfiguration> _configurationMock = new();

    private static AuthenticationOptions GitHubOptions() => new()
    {
        ExternalProviders = new ExternalProviderOptions
        {
            Providers = new Dictionary<string, OAuthProviderOptions>
            {
                ["github"] = new()
                {
                    Enabled = true,
                    ClientId = "test-client-id",
                    ClientSecret = "test-client-secret"
                }
            }
        }
    };

    private OAuthService CreateService(HttpClient? httpClient = null) => new(
        httpClient ?? new HttpClient(_handlerMock.Object),
        _configurationMock.Object,
        NullLogger<OAuthService>.Instance,
        GitHubOptions());

    private void RespondWith(HttpStatusCode statusCode, string body) => _handlerMock.Protected()
        .Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>())
        .ReturnsAsync(new HttpResponseMessage(statusCode) { Content = new StringContent(body) });

    // ── Token endpoint: provider outage (5xx) ─────────────────────────────

    [Fact]
    public async Task TokenEndpointReturns500_SurfacesHttpRequestExceptionNotACrash()
    {
        RespondWith(HttpStatusCode.InternalServerError, "upstream unavailable");
        var sut = CreateService();

        var act = () => sut.HandleCallbackAsync("github", "code", "csrf-state", "https://app.example.test/callback");

        (await act.Should().ThrowAsync<HttpRequestException>())
            .Which.Message.Should().Contain("500");
    }

    [Fact]
    public async Task TokenEndpointReturns503_SurfacesHttpRequestExceptionNotACrash()
    {
        RespondWith(HttpStatusCode.ServiceUnavailable, "provider maintenance");
        var sut = CreateService();

        var act = () => sut.HandleCallbackAsync("github", "code", "csrf-state", "https://app.example.test/callback");

        (await act.Should().ThrowAsync<HttpRequestException>())
            .Which.Message.Should().Contain("503");
    }

    // ── Token endpoint: provider hangs until the HTTP timeout ─────────────

    [Fact]
    public async Task TokenEndpointTimesOut_SurfacesTaskCanceledException()
    {
        // A handler that never answers: the HttpClient timeout cancels the request.
        using var httpClient = new HttpClient(new NeverRespondingHandler()) { Timeout = TimeSpan.FromMilliseconds(200) };
        var sut = CreateService(httpClient);

        var act = () => sut.HandleCallbackAsync("github", "code", "csrf-state", "https://app.example.test/callback");

        // TaskCanceledException is the documented HttpClient timeout surface; the service
        // must not swallow it into a fake success or an opaque wrapper.
        await act.Should().ThrowAsync<TaskCanceledException>();
    }

    // ── Token endpoint: malformed response body ───────────────────────────

    [Theory]
    [InlineData("<html><body>Gateway error</body></html>")]
    [InlineData("not json at all")]
    [InlineData("")] // connection-level truncation
    public async Task TokenEndpointReturnsMalformedBody_SurfacesJsonException(string body)
    {
        RespondWith(HttpStatusCode.OK, body);
        var sut = CreateService();

        var act = () => sut.HandleCallbackAsync("github", "code", "csrf-state", "https://app.example.test/callback");

        await act.Should().ThrowAsync<JsonException>();
    }

    [Fact]
    public async Task TokenEndpointChangesApiResponseShape_MissingAccessToken_ThrowsDeterministically()
    {
        // "Provider changes API version": HTTP 200, valid JSON, but the expected
        // access_token member is gone — the failure must be deterministic, not a null-ref.
        RespondWith(HttpStatusCode.OK, """{"token_type":"bearer","scope":"read:user"}""");
        var sut = CreateService();

        var act = () => sut.HandleCallbackAsync("github", "code", "csrf-state", "https://app.example.test/callback");

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── User profile endpoint failures (token exchange already succeeded) ──

    [Fact]
    public async Task UserEndpointReturns500_SurfacesHttpRequestException()
    {
        RespondWith(HttpStatusCode.InternalServerError, "boom");
        var sut = CreateService();

        var act = () => sut.GetUserProfileAsync("github", "provider-access-token");

        (await act.Should().ThrowAsync<HttpRequestException>())
            .Which.Message.Should().Contain("500");
    }

    [Fact]
    public async Task UserEndpointReturnsMalformedPayload_SurfacesJsonException()
    {
        RespondWith(HttpStatusCode.OK, "{\"id\":123,"); // truncated mid-object
        var sut = CreateService();

        var act = () => sut.GetUserProfileAsync("github", "provider-access-token");

        await act.Should().ThrowAsync<JsonException>();
    }

    [Fact]
    public async Task UserEndpointReturnsEmptyArrayInsteadOfObject_SurfacesJsonException()
    {
        // Another "API changed" shape: array where an object is required.
        RespondWith(HttpStatusCode.OK, "[]");
        var sut = CreateService();

        var act = () => sut.GetUserProfileAsync("github", "provider-access-token");

        await act.Should().ThrowAsync<JsonException>();
    }

    /// <summary>An HTTP handler that never produces a response, forcing the client timeout.</summary>
    private sealed class NeverRespondingHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
            throw new UnreachableException();
        }
    }
}
