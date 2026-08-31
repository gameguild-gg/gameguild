using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace GameGuild.Compliance.KYC;

public enum KycAmlState
{
    Created = 1,
    ApplicantPending = 2,
    InReview = 3,
    Approved = 4,
    Rejected = 5,
    NeedsReview = 6,
    Expired = 7
}

public sealed record KycAmlApplicantRequest(string ExternalUserId, string LevelName);
public sealed record KycAmlApplicant(string ApplicantId, string ExternalUserId, KycAmlState State);
public sealed record KycAmlAccessToken(string Token, string ExternalUserId);
public sealed record KycAmlStatus(
    string ApplicantId,
    string ExternalUserId,
    KycAmlState State,
    string? JurisdictionCode = null);

public interface IKycAmlProvider
{
    Task<KycAmlApplicant> CreateApplicantAsync(
        KycAmlApplicantRequest request,
        CancellationToken cancellationToken);

    Task<KycAmlAccessToken> CreateAccessTokenAsync(
        string externalUserId,
        string levelName,
        int lifetimeSeconds,
        CancellationToken cancellationToken);

    Task<KycAmlStatus> GetStatusAsync(
        string applicantId,
        CancellationToken cancellationToken);

    bool VerifyWebhook(
        ReadOnlySpan<byte> rawPayload,
        string suppliedDigest,
        string digestAlgorithm,
        DateTimeOffset issuedAt,
        DateTimeOffset receivedAt);
}

public sealed class SumSubKycAmlOptions
{
    public const string SectionName = "Compliance:KycAml:SumSub";

    public string BaseUrl { get; set; } = string.Empty;
    public string AppToken { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public TimeSpan WebhookTolerance { get; set; }
}

public sealed class SumSubKycAmlProvider : IKycAmlProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;
    private readonly SumSubKycAmlOptions _options;
    private readonly TimeProvider _timeProvider;

    public SumSubKycAmlProvider(
        HttpClient httpClient,
        IOptions<SumSubKycAmlOptions> options,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        _httpClient = httpClient;
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    public async Task<KycAmlApplicant> CreateApplicantAsync(
        KycAmlApplicantRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var path = "/resources/applicants?levelName=" + Uri.EscapeDataString(request.LevelName);
        var body = JsonSerializer.Serialize(new ApplicantRequest(request.ExternalUserId), JsonOptions);
        using var response = await SendAsync(HttpMethod.Post, path, body, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            var existingPath = "/resources/applicants/-;externalUserId=" +
                               Uri.EscapeDataString(request.ExternalUserId) + "/one";
            using var existing = await SendAsync(HttpMethod.Get, existingPath, string.Empty, cancellationToken);
            existing.EnsureSuccessStatusCode();
            return ToApplicant(await ReadApplicantAsync(existing, cancellationToken));
        }

        response.EnsureSuccessStatusCode();
        return ToApplicant(await ReadApplicantAsync(response, cancellationToken));
    }

    public async Task<KycAmlAccessToken> CreateAccessTokenAsync(
        string externalUserId,
        string levelName,
        int lifetimeSeconds,
        CancellationToken cancellationToken)
    {
        var body = JsonSerializer.Serialize(
            new AccessTokenRequest(externalUserId, levelName, lifetimeSeconds),
            JsonOptions);
        using var response = await SendAsync(
            HttpMethod.Post,
            "/resources/accessTokens/sdk",
            body,
            cancellationToken);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<AccessTokenResponse>(JsonOptions, cancellationToken)
                      ?? throw new SumSubProtocolException("SumSub returned an empty access-token response.");
        return new KycAmlAccessToken(payload.Token, payload.UserId);
    }

    public async Task<KycAmlStatus> GetStatusAsync(
        string applicantId,
        CancellationToken cancellationToken)
    {
        var path = "/resources/applicants/" + Uri.EscapeDataString(applicantId) + "/one";
        using var response = await SendAsync(HttpMethod.Get, path, string.Empty, cancellationToken);
        response.EnsureSuccessStatusCode();
        var applicant = await ReadApplicantAsync(response, cancellationToken);
        var jurisdiction = SumSubApplicantJurisdiction.Normalize(applicant.Info?.Country)
            ?? SumSubApplicantJurisdiction.Normalize(applicant.Country)
            ?? SumSubApplicantJurisdiction.Normalize(applicant.FixedInfo?.Country);
        return new KycAmlStatus(
            applicant.Id,
            applicant.ExternalUserId,
            MapState(applicant.Review),
            jurisdiction);
    }

    public bool VerifyWebhook(
        ReadOnlySpan<byte> rawPayload,
        string suppliedDigest,
        string digestAlgorithm,
        DateTimeOffset issuedAt,
        DateTimeOffset receivedAt)
    {
        EnsureConfigured();
        if (issuedAt > receivedAt || receivedAt - issuedAt > _options.WebhookTolerance)
            return false;
        if (digestAlgorithm is not ("HMAC_SHA256_HEX" or "HMAC_SHA512_HEX"))
            return false;

        var expected = SumSubWebhookSigning.Sign(_options.WebhookSecret, digestAlgorithm, rawPayload);
        if (expected.Length != suppliedDigest.Length)
            return false;
        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(expected),
            Encoding.ASCII.GetBytes(suppliedDigest.ToLowerInvariant()));
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string pathAndQuery,
        string body,
        CancellationToken cancellationToken)
    {
        EnsureConfigured();
        var timestamp = _timeProvider.GetUtcNow().ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture);
        var request = new HttpRequestMessage(method, new Uri(new Uri(_options.BaseUrl), pathAndQuery));
        if (body.Length != 0)
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        request.Headers.Add("X-App-Token", _options.AppToken);
        request.Headers.Add("X-App-Access-Ts", timestamp);
        request.Headers.Add(
            "X-App-Access-Sig",
            SumSubRequestSigning.Sign(_options.SecretKey, timestamp, method.Method, pathAndQuery, body));
        return await _httpClient.SendAsync(request, cancellationToken);
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl) ||
            string.IsNullOrWhiteSpace(_options.AppToken) ||
            string.IsNullOrWhiteSpace(_options.SecretKey) ||
            string.IsNullOrWhiteSpace(_options.WebhookSecret) ||
            _options.WebhookTolerance <= TimeSpan.Zero)
            throw new SumSubNotConfiguredException(
                "SumSub KYC/AML remains disabled until its endpoint, credentials and webhook policy are configured.");
    }

    private static async Task<ApplicantResponse> ReadApplicantAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken) =>
        await response.Content.ReadFromJsonAsync<ApplicantResponse>(JsonOptions, cancellationToken)
        ?? throw new SumSubProtocolException("SumSub returned an empty applicant response.");

    private static KycAmlApplicant ToApplicant(ApplicantResponse applicant) =>
        new(applicant.Id, applicant.ExternalUserId, KycAmlState.ApplicantPending);

    private static KycAmlState MapState(ReviewResponse? review)
    {
        if (review?.ReviewStatus == "pending")
            return KycAmlState.ApplicantPending;
        if (review?.ReviewStatus != "completed")
            return KycAmlState.InReview;

        return review.ReviewResult?.ReviewAnswer switch
        {
            "GREEN" => KycAmlState.Approved,
            "RED" => KycAmlState.Rejected,
            _ => KycAmlState.NeedsReview
        };
    }

    private sealed record ApplicantRequest([property: JsonPropertyName("externalUserId")] string ExternalUserId);
    private sealed record AccessTokenRequest(
        [property: JsonPropertyName("userId")] string UserId,
        [property: JsonPropertyName("levelName")] string LevelName,
        [property: JsonPropertyName("ttlInSecs")] int TtlInSecs);
    private sealed record AccessTokenResponse(
        [property: JsonPropertyName("token")] string Token,
        [property: JsonPropertyName("userId")] string UserId);
    private sealed record ApplicantResponse(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("externalUserId")] string ExternalUserId,
        [property: JsonPropertyName("review")] ReviewResponse? Review,
        [property: JsonPropertyName("country")] string? Country,
        [property: JsonPropertyName("info")] ApplicantInfoResponse? Info,
        [property: JsonPropertyName("fixedInfo")] ApplicantInfoResponse? FixedInfo);
    private sealed record ApplicantInfoResponse(
        [property: JsonPropertyName("country")] string? Country);
    private sealed record ReviewResponse(
        [property: JsonPropertyName("reviewStatus")] string ReviewStatus,
        [property: JsonPropertyName("reviewResult")] ReviewResultResponse? ReviewResult);
    private sealed record ReviewResultResponse(
        [property: JsonPropertyName("reviewAnswer")] string ReviewAnswer);
}

public static class SumSubRequestSigning
{
    public static string Sign(
        string secretKey,
        string timestamp,
        string method,
        string pathAndQuery,
        string body)
    {
        var payload = timestamp + method.ToUpperInvariant() + pathAndQuery + body;
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
    }
}

public static class SumSubWebhookSigning
{
    public static string Sign(string secret, string digestAlgorithm, ReadOnlySpan<byte> payload)
    {
        using HMAC hmac = digestAlgorithm switch
        {
            "HMAC_SHA256_HEX" => new HMACSHA256(Encoding.UTF8.GetBytes(secret)),
            "HMAC_SHA512_HEX" => new HMACSHA512(Encoding.UTF8.GetBytes(secret)),
            _ => throw new ArgumentOutOfRangeException(nameof(digestAlgorithm), digestAlgorithm, "Unsupported SumSub digest algorithm.")
        };
        return Convert.ToHexString(hmac.ComputeHash(payload.ToArray())).ToLowerInvariant();
    }
}

public sealed class SumSubNotConfiguredException(string message) : InvalidOperationException(message);
public sealed class SumSubProtocolException(string message) : InvalidOperationException(message);
