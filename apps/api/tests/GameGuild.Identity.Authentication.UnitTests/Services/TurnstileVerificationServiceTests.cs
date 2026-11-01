using System.Net;
using FluentAssertions;
using GameGuild.Configuration.ApplicationLayer;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

/// <summary>
///     Server-side Turnstile verification semantics (issue #1082): unset secret disables
///     the gate, missing/oversized tokens and explicit Cloudflare rejections fail
///     closed, and transport failures fail open.
/// </summary>
public class TurnstileVerificationServiceTests
{
    private const string VerifyUrl = "https://turnstile.example.test/verify";

    [Fact]
    public async Task VerifyAsync_UnsetSecret_IsDisabledAndSucceeds()
    {
        var sut = BuildSut(new AuthenticationSecurityOptions { TurnstileSecretKey = string.Empty }, Responds("{\"success\":true}"));

        var result = await sut.VerifyAsync("token");

        result.Success.Should().BeTrue();
        result.Reason.Should().Be("disabled");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task VerifyAsync_MissingToken_FailsClosed(string? token)
    {
        var sut = BuildSut(SecretConfigured(), Responds("{\"success\":true}"));

        var result = await sut.VerifyAsync(token);

        result.Success.Should().BeFalse();
        result.Reason.Should().Be("missing-token");
    }

    [Fact]
    public async Task VerifyAsync_OversizedToken_FailsClosed()
    {
        var sut = BuildSut(SecretConfigured(), Responds("{\"success\":true}"));

        var result = await sut.VerifyAsync(new string('x', 2049));

        result.Success.Should().BeFalse();
        result.Reason.Should().Be("missing-token");
    }

    [Fact]
    public async Task VerifyAsync_CloudflareAccepts_Succeeds()
    {
        var handler = CapturingHandler(Responds("{\"success\":true}"));
        var sut = BuildSut(SecretConfigured(), handler);

        var result = await sut.VerifyAsync("token", "203.0.113.7");

        result.Success.Should().BeTrue();
        result.Reason.Should().BeNull();
        handler.LastRequestBody.Should().Contain("response=token");
        handler.LastRequestBody.Should().Contain("remoteip=203.0.113.7");
    }

    [Fact]
    public async Task VerifyAsync_CloudflareRejects_FailsClosed()
    {
        var sut = BuildSut(SecretConfigured(), Responds("{\"success\":false,\"error-codes\":[\"invalid-input-response\"]}"));

        var result = await sut.VerifyAsync("token");

        result.Success.Should().BeFalse();
        result.Reason.Should().Be("verify-failed");
    }

    [Fact]
    public async Task VerifyAsync_NetworkFailure_FailsOpen()
    {
        var handler = CapturingHandler(_ => throw new HttpRequestException("siteverify unreachable"));
        var sut = BuildSut(SecretConfigured(), handler);

        var result = await sut.VerifyAsync("token");

        result.Success.Should().BeTrue();
        result.Reason.Should().Be("verify-unavailable");
    }

    [Fact]
    public async Task VerifyAsync_UnreadableBody_FailsOpen()
    {
        var sut = BuildSut(SecretConfigured(), Responds("<html>gateway error</html>", HttpStatusCode.BadGateway));

        var result = await sut.VerifyAsync("token");

        result.Success.Should().BeTrue();
        result.Reason.Should().Be("verify-unavailable");
    }

    private static AuthenticationSecurityOptions SecretConfigured() => new()
    {
        TurnstileSecretKey = "test-secret",
        TurnstileSiteVerifyUrl = VerifyUrl
    };

    private static TurnstileVerificationService BuildSut(AuthenticationSecurityOptions options, HttpMessageHandler handler)
    {
        var client = new HttpClient(handler);
        var factory = Mock.Of<IHttpClientFactory>(f => f.CreateClient(It.IsAny<string>()) == client);
        return new TurnstileVerificationService(factory, options, NullLogger<TurnstileVerificationService>.Instance);
    }

    private static Func<HttpRequestMessage, HttpResponseMessage> Responds(string body, HttpStatusCode status = HttpStatusCode.OK) =>
        _ => new HttpResponseMessage(status) { Content = new StringContent(body) };

    private static CapturingHandler CapturingHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) => new(respond);

    private sealed class CapturingHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public string? LastRequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Content is not null)
            {
                LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
            }

            return respond(request);
        }
    }
}
