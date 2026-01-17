using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http;
using System.Text;
using Xunit;

namespace GameGuild.Commerce.Billing.UnitTests.Services;

public class PayPalSignatureVerificationServiceTests
{
    [Fact]
    public async Task VerifySignatureAsync_Should_Fail_When_WebhookId_Missing()
    {
        var service = CreateService(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));

        var result = await service.VerifySignatureAsync(
            "",
            "tx",
            "time",
            "sig",
            null,
            null,
            "{}",
            CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Webhook ID not configured");
    }

    [Fact]
    public async Task VerifySignatureAsync_Should_Fail_When_Token_Request_Fails()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("error", Encoding.UTF8, "application/json")
        });
        var service = CreateService(handler);

        var result = await service.VerifySignatureAsync(
            "wh",
            "tx",
            "time",
            "sig",
            null,
            null,
            "{}",
            CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("access token");
    }

    [Fact]
    public async Task VerifySignatureAsync_Should_Return_Success_When_Verified()
    {
        var handler = new SequenceHttpMessageHandler(new[]
        {
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"access_token\":\"token\",\"token_type\":\"Bearer\",\"expires_in\":3600}", Encoding.UTF8, "application/json")
            },
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"verification_status\":\"SUCCESS\"}", Encoding.UTF8, "application/json")
            }
        });

        var service = CreateService(handler);

        var result = await service.VerifySignatureAsync(
            "wh",
            "tx",
            "time",
            "sig",
            null,
            null,
            "{}",
            CancellationToken.None);

        result.IsValid.Should().BeTrue();
        result.VerificationStatus.Should().Be("SUCCESS");
    }

    [Fact]
    public async Task VerifySignatureAsync_Should_Return_Failed_When_Verification_Fails()
    {
        var handler = new SequenceHttpMessageHandler(new[]
        {
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"access_token\":\"token\",\"token_type\":\"Bearer\",\"expires_in\":3600}", Encoding.UTF8, "application/json")
            },
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"verification_status\":\"FAILURE\"}", Encoding.UTF8, "application/json")
            }
        });

        var service = CreateService(handler);

        var result = await service.VerifySignatureAsync(
            "wh",
            "tx",
            "time",
            "sig",
            null,
            null,
            "{}",
            CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.VerificationStatus.Should().Be("FAILURE");
    }

    private static PayPalSignatureVerificationService CreateService(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler);
        var options = Options.Create(new PayPalSettings
        {
            ClientId = "id",
            ClientSecret = "secret",
            Environment = "sandbox"
        });

        return new PayPalSignatureVerificationService(httpClient, options, NullLogger<PayPalSignatureVerificationService>.Instance);
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler = handler;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_handler(request));
    }

    private sealed class SequenceHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses;

        public SequenceHttpMessageHandler(IEnumerable<HttpResponseMessage> responses)
        {
            _responses = new Queue<HttpResponseMessage>(responses);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = _responses.Count > 0 ? _responses.Dequeue() : new HttpResponseMessage(HttpStatusCode.InternalServerError);
            return Task.FromResult(response);
        }
    }
}