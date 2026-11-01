using System.Net;
using System.Text;
using FluentAssertions;
using GameGuild.Notifications.Services.WhatsApp;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace GameGuild.Notifications.UnitTests.Services.WhatsApp;

/// <summary>
///     Stub HTTP transport capturing the outgoing request and replaying a canned response.
/// </summary>
internal sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
{
    public HttpRequestMessage? LastRequest { get; private set; }

    public string? LastRequestBody { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        LastRequestBody = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        return responder(request);
    }
}

/// <summary>
///     WhatsApp Business Cloud API adapter tests (issues #54/#55): payload shape against
///     mocked HTTP, credential resolution strictly from the injected environment reader
///     (placeholder values only — never real credentials), fail-closed validation of
///     recipients/templates, provider error surfacing and endpoint SSRF rules.
/// </summary>
public sealed class WhatsAppBusinessApiProviderTests
{
    private const string TestAppId = "unit-test-app-id";
    private const string TestPhoneNumberId = "unit-test-phone-number-id";
    private const string TestAccessToken = "unit-test-access-token";

    private static readonly Func<string, string?> EnvironmentWithCredentials = name => name switch
    {
        WhatsAppSettings.AppIdEnvironmentVariable => TestAppId,
        WhatsAppSettings.PhoneNumberIdEnvironmentVariable => TestPhoneNumberId,
        WhatsAppSettings.AccessTokenEnvironmentVariable => TestAccessToken,
        _ => null
    };

    private static WhatsAppSettings EnabledSettings(
        string baseUrl = "https://graph.unit-test.example",
        string[]? allowlist = null) => new()
    {
        Enabled = true,
        ApiBaseUrl = baseUrl,
        TemplateAllowlist = [.. (allowlist ?? ["campaign_update"])]
    };

    private static WhatsAppEndpointGuard GuardFor(string host) => new(_ => host switch
    {
        "graph.unit-test.example" => [IPAddress.Parse("157.240.1.35")],
        _ => throw new System.Net.Sockets.SocketException()
    });

    private static HttpResponseMessage Ok(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };

    private static HttpResponseMessage ProviderError(string message, HttpStatusCode status = HttpStatusCode.BadRequest) =>
        new(status)
        {
            Content = new StringContent(
                "{\"error\":{\"message\":\"" + message + "\",\"type\":\"OAuthException\",\"code\":400}}",
                Encoding.UTF8,
                "application/json")
        };

    private static (WhatsAppBusinessApiProvider Provider, StubHttpMessageHandler Handler) CreateProvider(
        WhatsAppSettings? settings = null,
        Func<HttpRequestMessage, HttpResponseMessage>? responder = null,
        Func<string, string?>? environment = null)
    {
        var handler = new StubHttpMessageHandler(responder ?? (_ => Ok("""{"messages":[{"id":"wamid.TEST123"}]}""")));
        var options = Options.Create(settings ?? EnabledSettings());
        var provider = new WhatsAppBusinessApiProvider(
            options,
            new HttpClient(handler),
            GuardFor("graph.unit-test.example"),
            environment ?? EnvironmentWithCredentials,
            NullLogger<WhatsAppBusinessApiProvider>.Instance);
        return (provider, handler);
    }

    [Fact]
    public void IsConfigured_TrueWhenEnabledAndCredentialsPresent()
    {
        var (provider, _) = CreateProvider();
        provider.IsConfigured.Should().BeTrue();
    }

    [Fact]
    public void IsConfigured_FalseWhenDisabled()
    {
        var (provider, _) = CreateProvider(new WhatsAppSettings { Enabled = false });
        provider.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public async Task SendMessage_PostsCloudApiShapeAndReturnsWamid()
    {
        var (provider, handler) = CreateProvider();

        var result = await provider.SendMessageAsync(new WhatsAppOutboundMessage("+15551234567", "Hello there"));

        result.IsSuccess.Should().BeTrue();
        result.Value.ProviderMessageId.Should().Be("wamid.TEST123");
        handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
        handler.LastRequest.RequestUri!.ToString().Should().Be(
            $"https://graph.unit-test.example/v21.0/{TestPhoneNumberId}/messages");
        handler.LastRequest.Headers.Authorization!.Scheme.Should().Be("Bearer");
        handler.LastRequest.Headers.Authorization.Parameter.Should().Be(TestAccessToken);
        handler.LastRequestBody.Should().Contain("\"messaging_product\":\"whatsapp\"");
        handler.LastRequestBody.Should().Contain("\"to\":\"15551234567\"");
        handler.LastRequestBody.Should().Contain("\"type\":\"text\"");
        handler.LastRequestBody.Should().Contain("Hello there");
        // The app id is not part of the send payload (it identifies the app, not the message).
        handler.LastRequestBody.Should().NotContain(TestAppId);
    }

    [Fact]
    public async Task SendMessage_TruncatesBodyToWhatsAppLimit()
    {
        var (provider, handler) = CreateProvider();

        var result = await provider.SendMessageAsync(new WhatsAppOutboundMessage("+15551234567", new string('x', 5000)));

        result.IsSuccess.Should().BeTrue();
        handler.LastRequestBody!.Length.Should().BeLessThan(5000);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-number")]
    [InlineData("+12")]
    [InlineData("+123456789012345678")]
    public async Task SendMessage_RejectsNonE164DestinationsWithoutHttpCall(string phone)
    {
        var (provider, handler) = CreateProvider();

        var result = await provider.SendMessageAsync(new WhatsAppOutboundMessage(phone, "body"));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("WhatsApp.InvalidRecipient");
        handler.LastRequest.Should().BeNull();
    }

    [Fact]
    public async Task SendTemplate_RejectsTemplatesOutsideTheAllowlist()
    {
        var (provider, handler) = CreateProvider();

        var result = await provider.SendTemplateAsync(new WhatsAppTemplateMessage("+15551234567", "not_allowlisted", null, null));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("WhatsApp.TemplateNotAllowed");
        handler.LastRequest.Should().BeNull();
    }

    [Fact]
    public async Task SendTemplate_MapsPlaceholdersInSortedKeyOrder()
    {
        var (provider, handler) = CreateProvider();

        var result = await provider.SendTemplateAsync(new WhatsAppTemplateMessage(
            "+15551234567",
            "campaign_update",
            "pt_BR",
            new Dictionary<string, string> { ["beta"] = "second", ["alpha"] = "first" }));

        result.IsSuccess.Should().BeTrue();
        handler.LastRequestBody.Should().Contain("\"name\":\"campaign_update\"");
        handler.LastRequestBody.Should().Contain("\"code\":\"pt_BR\"");
        handler.LastRequestBody.Should().Contain("\"type\":\"template\"");
        var body = handler.LastRequestBody!;
        body.IndexOf("first", StringComparison.Ordinal)
            .Should().BeLessThan(body.IndexOf("second", StringComparison.Ordinal),
                "placeholder positions are the sorted key order");
    }

    [Fact]
    public async Task SendMessage_SurfacesProviderRejection()
    {
        var (provider, _) = CreateProvider(responder: _ => ProviderError("Invalid OAuth access token"));

        var result = await provider.SendMessageAsync(new WhatsAppOutboundMessage("+15551234567", "body"));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("WhatsApp.ProviderRejected");
        result.Error.Description.Should().Contain("Invalid OAuth access token");
    }

    [Fact]
    public async Task SendMessage_FailsClosedOnNetworkFailure()
    {
        var (provider, _) = CreateProvider(responder: _ => throw new HttpRequestException("connection refused"));

        var result = await provider.SendMessageAsync(new WhatsAppOutboundMessage("+15551234567", "body"));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("WhatsApp.ProviderUnavailable");
    }

    [Fact]
    public async Task Calls_ThrowWhenCredentialsAreMissing()
    {
        var (provider, _) = CreateProvider(environment: _ => null);

        var act = () => provider.SendMessageAsync(new WhatsAppOutboundMessage("+15551234567", "body"));
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Theory]
    [InlineData("http://localhost:5432")]      // localhost name
    [InlineData("http://api.internal.example")] // internal suffix + private resolution
    [InlineData("https://127.0.0.1")]           // loopback literal
    [InlineData("https://10.1.2.3")]            // private literal
    [InlineData("https://192.168.0.10")]        // private literal
    [InlineData("ftp://graph.unit-test.example")] // non-http scheme
    public async Task SendMessage_RejectsDisallowedEndpoints(string baseUrl)
    {
        var settings = EnabledSettings(baseUrl);
        // IP-literal hosts resolve to themselves; names resolve to a private address so
        // every case exercises a different rejection rule but none may reach the wire.
        var guard = new WhatsAppEndpointGuard(host =>
            IPAddress.TryParse(host, out var literal) ? [literal] : [IPAddress.Parse("10.9.8.7")]);

        var handler = new StubHttpMessageHandler(_ => Ok("""{"messages":[{"id":"wamid.TEST123"}]}"""));
        var provider = new WhatsAppBusinessApiProvider(
            Options.Create(settings),
            new HttpClient(handler),
            guard,
            EnvironmentWithCredentials,
            NullLogger<WhatsAppBusinessApiProvider>.Instance);

        var act = () => provider.SendMessageAsync(new WhatsAppOutboundMessage("+15551234567", "body"));
        await act.Should().ThrowAsync<InvalidOperationException>();
        handler.LastRequest.Should().BeNull("no request may leave before the endpoint guard passes");
    }

    [Fact]
    public async Task VerifyNumber_RejectsMalformedNumbersLocally()
    {
        var (provider, handler) = CreateProvider();

        var result = await provider.VerifyNumberAsync("not-a-number");

        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeFalse();
        handler.LastRequest.Should().BeNull();
    }

    [Fact]
    public async Task VerifyNumber_ConfirmsChannelForWellFormedNumbers()
    {
        var (provider, handler) = CreateProvider();

        var result = await provider.VerifyNumberAsync("+15551234567");

        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeTrue();
        result.Value.CanonicalPhoneNumber.Should().Be("+15551234567");
        handler.LastRequest!.RequestUri!.ToString().Should().Contain($"v21.0/{TestPhoneNumberId}");
    }

    [Fact]
    public async Task GetMessageStatus_ParsesStatusAndErrorFields()
    {
        var (provider, _) = CreateProvider(responder: _ => Ok(
            """{"messages":[{"id":"wamid.TEST123","status":"failed","errors":[{"code":131026,"title":"Undeliverable"}]}]}"""));

        var result = await provider.GetMessageStatusAsync("wamid.TEST123");

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("failed");
        result.Value.ErrorCode.Should().Be("131026");
        result.Value.ErrorMessage.Should().Be("Undeliverable");
    }

    [Fact]
    public async Task GetMessageStatus_FailsWhenProviderOmitsStatus()
    {
        var (provider, _) = CreateProvider(responder: _ => Ok("""{"messages":[{"id":"wamid.TEST123"}]}"""));

        var result = await provider.GetMessageStatusAsync("wamid.TEST123");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("WhatsApp.StatusUnavailable");
    }
}
