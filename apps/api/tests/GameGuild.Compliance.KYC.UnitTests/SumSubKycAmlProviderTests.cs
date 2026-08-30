using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace GameGuild.Compliance.KYC.Tests;

public sealed class SumSubKycAmlProviderTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 24, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateApplicantSignsExactRequestAndReturnsApplicant()
    {
        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"id\":\"applicant-1\",\"externalUserId\":\"opaque-user\"}")
        });
        var provider = CreateProvider(handler);

        var applicant = await provider.CreateApplicantAsync(
            new KycAmlApplicantRequest("opaque-user", "basic-level"),
            CancellationToken.None);

        applicant.Should().Be(new KycAmlApplicant("applicant-1", "opaque-user", KycAmlState.ApplicantPending));
        handler.Requests.Should().ContainSingle();
        var request = handler.Requests[0];
        request.Method.Should().Be(HttpMethod.Post);
        request.PathAndQuery.Should().Be("/resources/applicants?levelName=basic-level");
        request.AppToken.Should().Be("app-token");
        request.Timestamp.Should().Be("1787572800");
        request.Signature.Should().Be(SumSubRequestSigning.Sign(
            "secret-key", request.Timestamp, "POST", request.PathAndQuery, request.Body));
    }

    [Fact]
    public async Task CreateApplicantIsIdempotentWhenProviderReportsConflict()
    {
        var handler = new RecordingHandler(
            new HttpResponseMessage(HttpStatusCode.Conflict),
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"id\":\"applicant-existing\",\"externalUserId\":\"opaque user\"}")
            });
        var provider = CreateProvider(handler);

        var applicant = await provider.CreateApplicantAsync(
            new KycAmlApplicantRequest("opaque user", "basic-level"),
            CancellationToken.None);

        applicant.ApplicantId.Should().Be("applicant-existing");
        handler.Requests.Should().HaveCount(2);
        handler.Requests[1].PathAndQuery.Should()
            .Be("/resources/applicants/-;externalUserId=opaque%20user/one");
    }

    [Fact]
    public async Task AccessTokenAndStatusUseSignedProviderRequests()
    {
        var handler = new RecordingHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"token\":\"access-token\",\"userId\":\"opaque-user\"}")
            },
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"id\":\"applicant-1\",\"externalUserId\":\"opaque-user\",\"info\":{\"country\":\"BRA\"},\"review\":{\"reviewStatus\":\"completed\",\"reviewResult\":{\"reviewAnswer\":\"GREEN\"}}}")
            });
        var provider = CreateProvider(handler);

        var token = await provider.CreateAccessTokenAsync("opaque-user", "basic-level", 600, CancellationToken.None);
        var status = await provider.GetStatusAsync("applicant-1", CancellationToken.None);

        token.Should().Be(new KycAmlAccessToken("access-token", "opaque-user"));
        status.State.Should().Be(KycAmlState.Approved);
        status.ApplicantId.Should().Be("applicant-1");
        status.JurisdictionCode.Should().Be("BRA");
        handler.Requests[0].PathAndQuery.Should().Be("/resources/accessTokens/sdk");
        handler.Requests[1].PathAndQuery.Should().Be("/resources/applicants/applicant-1/one");
    }

    [Theory]
    [InlineData("pending", null, KycAmlState.ApplicantPending)]
    [InlineData("queued", null, KycAmlState.InReview)]
    [InlineData("completed", "RED", KycAmlState.Rejected)]
    [InlineData("completed", "YELLOW", KycAmlState.NeedsReview)]
    [InlineData("completed", "ERROR", KycAmlState.NeedsReview)]
    public async Task StatusMappingIsFailClosed(string reviewStatus, string? reviewAnswer, KycAmlState expected)
    {
        var answer = reviewAnswer is null ? "" : $",\"reviewResult\":{{\"reviewAnswer\":\"{reviewAnswer}\"}}";
        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent($"{{\"id\":\"applicant-1\",\"externalUserId\":\"opaque-user\",\"review\":{{\"reviewStatus\":\"{reviewStatus}\"{answer}}}}}")
        });

        var status = await CreateProvider(handler).GetStatusAsync("applicant-1", CancellationToken.None);

        status.State.Should().Be(expected);
    }

    [Fact]
    public void WebhookVerificationRequiresStrongDigestAndFreshTimestamp()
    {
        var provider = CreateProvider(new RecordingHandler());
        var payload = Encoding.UTF8.GetBytes("{\"type\":\"applicantReviewed\"}");
        var digest = SumSubWebhookSigning.Sign("webhook-secret", "HMAC_SHA256_HEX", payload);

        provider.VerifyWebhook(payload, digest, "HMAC_SHA256_HEX", Now.AddMinutes(-1), Now).Should().BeTrue();
        provider.VerifyWebhook(payload, digest, "HMAC_SHA256_HEX", Now.AddMinutes(1), Now).Should().BeFalse();
        provider.VerifyWebhook(payload, digest, "HMAC_SHA256_HEX", Now.AddMinutes(-6), Now).Should().BeFalse();
        provider.VerifyWebhook(payload, digest, "HMAC_SHA1_HEX", Now, Now).Should().BeFalse();
        provider.VerifyWebhook(payload, "00", "HMAC_SHA256_HEX", Now, Now).Should().BeFalse();
        provider.VerifyWebhook(payload, new string('0', digest.Length), "HMAC_SHA256_HEX", Now, Now).Should().BeFalse();
        provider.VerifyWebhook(payload, SumSubWebhookSigning.Sign("webhook-secret", "HMAC_SHA512_HEX", payload),
            "HMAC_SHA512_HEX", Now, Now).Should().BeTrue();
    }

    [Fact]
    public async Task MissingCredentialsRemainFailClosedUntilOperationTime()
    {
        var provider = new SumSubKycAmlProvider(
            new HttpClient(new RecordingHandler()),
            Options.Create(new SumSubKycAmlOptions()),
            new FixedTimeProvider(Now));

        await FluentActions.Awaiting(() => provider.GetStatusAsync("applicant", CancellationToken.None))
            .Should().ThrowAsync<SumSubNotConfiguredException>();
    }

    [Fact]
    public async Task ProviderValidatesConstructionConfigurationAndProtocolBoundaries()
    {
        Action nullClient = () => new SumSubKycAmlProvider(null!, Options.Create(ValidOptions()), new FixedTimeProvider(Now));
        Action nullOptions = () => new SumSubKycAmlProvider(new HttpClient(new RecordingHandler()), null!, new FixedTimeProvider(Now));
        Action nullTime = () => new SumSubKycAmlProvider(new HttpClient(new RecordingHandler()), Options.Create(ValidOptions()), null!);
        nullClient.Should().Throw<ArgumentNullException>();
        nullOptions.Should().Throw<ArgumentNullException>();
        nullTime.Should().Throw<ArgumentNullException>();

        await FluentActions.Awaiting(() => CreateProvider(new RecordingHandler()).CreateApplicantAsync(null!, CancellationToken.None))
            .Should().ThrowAsync<ArgumentNullException>();

        foreach (var options in new[]
                 {
                     ValidOptions(baseUrl: " "),
                     ValidOptions(appToken: " "),
                     ValidOptions(secretKey: " "),
                     ValidOptions(webhookSecret: " "),
                     ValidOptions(webhookTolerance: TimeSpan.Zero)
                 })
        {
            var provider = new SumSubKycAmlProvider(
                new HttpClient(new RecordingHandler()), Options.Create(options), new FixedTimeProvider(Now));
            await FluentActions.Awaiting(() => provider.GetStatusAsync("applicant", CancellationToken.None))
                .Should().ThrowAsync<SumSubNotConfiguredException>();
        }

        var emptyApplicant = CreateProvider(new RecordingHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("null", Encoding.UTF8, "application/json")
        }));
        await FluentActions.Awaiting(() => emptyApplicant.CreateApplicantAsync(
                new KycAmlApplicantRequest("user", "level"), CancellationToken.None))
            .Should().ThrowAsync<SumSubProtocolException>();

        var emptyToken = CreateProvider(new RecordingHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("null", Encoding.UTF8, "application/json")
        }));
        await FluentActions.Awaiting(() => emptyToken.CreateAccessTokenAsync("user", "level", 60, CancellationToken.None))
            .Should().ThrowAsync<SumSubProtocolException>();

        FluentActions.Invoking(() => SumSubWebhookSigning.Sign("secret", "unsupported", [1, 2, 3]))
            .Should().Throw<ArgumentOutOfRangeException>();
        new SumSubProtocolException("protocol").Message.Should().Be("protocol");
    }

    [Fact]
    public async Task StatusMappingFailsClosedWhenReviewOrReviewResultIsMissing()
    {
        var provider = CreateProvider(new RecordingHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"id\":\"a\",\"externalUserId\":\"u\"}")
            },
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"id\":\"a\",\"externalUserId\":\"u\",\"review\":{\"reviewStatus\":\"completed\"}}")
            }));

        (await provider.GetStatusAsync("a", CancellationToken.None)).State.Should().Be(KycAmlState.InReview);
        (await provider.GetStatusAsync("a", CancellationToken.None)).State.Should().Be(KycAmlState.NeedsReview);
    }

    [Fact]
    public void CompositionRegistersDurableKycServicesAndSumSubAdapter()
    {
        var services = new ServiceCollection();

        services.AddKycComposition(new ConfigurationBuilder().Build());

        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IKycRepository) && descriptor.ImplementationType == typeof(KycRepository));
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IKycService) && descriptor.ImplementationType == typeof(KycService));
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IKycAmlProvider));
        new KycModule().Name.Should().Be("Compliance.KYC");
    }

    private static SumSubKycAmlProvider CreateProvider(RecordingHandler handler) => new(
        new HttpClient(handler),
        Options.Create(ValidOptions()),
        new FixedTimeProvider(Now));

    private static SumSubKycAmlOptions ValidOptions(
        string baseUrl = "https://api.sumsub.test",
        string appToken = "app-token",
        string secretKey = "secret-key",
        string webhookSecret = "webhook-secret",
        TimeSpan? webhookTolerance = null) => new()
    {
        BaseUrl = baseUrl,
        AppToken = appToken,
        SecretKey = secretKey,
        WebhookSecret = webhookSecret,
        WebhookTolerance = webhookTolerance ?? TimeSpan.FromMinutes(5)
    };

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class RecordingHandler(params HttpResponseMessage[] responses) : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new(responses);

        public List<RecordedRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add(new RecordedRequest(
                request.Method,
                request.RequestUri!.PathAndQuery,
                body,
                request.Headers.GetValues("X-App-Token").Single(),
                request.Headers.GetValues("X-App-Access-Ts").Single(),
                request.Headers.GetValues("X-App-Access-Sig").Single()));
            return _responses.Count == 0
                ? new HttpResponseMessage(HttpStatusCode.InternalServerError)
                : _responses.Dequeue();
        }
    }

    private sealed record RecordedRequest(
        HttpMethod Method,
        string PathAndQuery,
        string Body,
        string AppToken,
        string Timestamp,
        string Signature);
}
