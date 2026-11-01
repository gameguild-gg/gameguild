using System.Text.Json;

using FluentAssertions;
using GameGuild.Commerce.Billing;
using Xunit;

namespace GameGuild.Commerce.Billing.UnitTests.Security;

/// <summary>
///     Unit tests for the raw-delivery redaction pass (issue #408: capture raw payload and
///     all headers). Secrets must never survive literally; signature and authorization
///     headers must remain verifiable-by-hash; PII masking follows the RedactPii switch.
///     All sample values below are synthetic fixtures, never real credentials.
/// </summary>
public class WebhookRawDeliveryRedactorTests
{
    private const string SampleSignature = "t=1770000000,v1=5257a869e7ecebec0ba365d6d1faa2e0d1c1e8a7b6c5d4e3f2a1b0c9d8e7f6a5";
    private const string SampleBearerToken = "ya29.a0ARrdaM-synthetic-access-token-value-000000";
    private const string SampleEmail = "player@example.org";

    [Fact]
    public void RedactHeaders_Hashes_Signature_And_Authorization_Headers()
    {
        var headers = new Dictionary<string, string>
        {
            ["Stripe-Signature"] = SampleSignature,
            ["Authorization"] = $"Bearer {SampleBearerToken}",
            ["User-Agent"] = "Stripe/1.0 (+https://stripe.com/docs/webhooks)"
        };

        var redacted = WebhookRawDeliveryRedactor.RedactHeaders(headers, redactPii: true);
        var document = JsonSerializer.Deserialize<Dictionary<string, string>>(redacted)!;

        redacted.Should().NotContain(SampleSignature);
        redacted.Should().NotContain(SampleBearerToken);
        document["Stripe-Signature"].Should().StartWith(WebhookRawDeliveryRedactor.HashPrefix);
        document["Authorization"].Should().StartWith(WebhookRawDeliveryRedactor.HashPrefix);
        document["User-Agent"].Should().Be("Stripe/1.0 (+https://stripe.com/docs/webhooks)");
    }

    [Fact]
    public void RedactHeaders_Keeps_Non_Sensitive_Provider_Headers_Verbatim()
    {
        var headers = new Dictionary<string, string>
        {
            ["PayPal-Transmission-Id"] = "tid_9f8e7d6c",
            ["PayPal-Transmission-Time"] = "2026-10-31T12:00:00Z",
            ["PayPal-Auth-Algo"] = "auth-algo-sha256-rsa"
        };

        var redacted = WebhookRawDeliveryRedactor.RedactHeaders(headers, redactPii: true);
        var document = JsonSerializer.Deserialize<Dictionary<string, string>>(redacted)!;

        document["PayPal-Transmission-Id"].Should().Be("tid_9f8e7d6c");
        document["PayPal-Transmission-Time"].Should().Be("2026-10-31T12:00:00Z");
        document["PayPal-Auth-Algo"].Should().Be("auth-algo-sha256-rsa");
    }

    [Fact]
    public void RedactHeaders_Masks_Emails_In_Header_Values_When_Pii_Redaction_Is_On()
    {
        var headers = new Dictionary<string, string>
        {
            ["X-Contact"] = $"billing disputes <{SampleEmail}>"
        };

        var redacted = WebhookRawDeliveryRedactor.RedactHeaders(headers, redactPii: true);

        redacted.Should().NotContain(SampleEmail);
        redacted.Should().Contain(WebhookRawDeliveryRedactor.EmailPrefix);
    }

    [Fact]
    public void Hashed_Signature_Headers_Remain_Verifiable_By_Hash()
    {
        var headers = new Dictionary<string, string>
        {
            ["Stripe-Signature"] = SampleSignature
        };

        var redacted = WebhookRawDeliveryRedactor.RedactHeaders(headers, redactPii: true);
        var document = JsonSerializer.Deserialize<Dictionary<string, string>>(redacted)!;
        var stored = document["Stripe-Signature"];

        // Anyone holding the raw signature value can re-verify it against the stored hash.
        WebhookRawDeliveryRedactor.VerifyHash(SampleSignature, stored).Should().BeTrue();
        WebhookRawDeliveryRedactor.VerifyHash("t=1,v1=deadbeef", stored).Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("sha256:nothex")]
    [InlineData("plain-value")]
    public void VerifyHash_Never_Matches_Non_Hash_Stored_Values(string stored)
    {
        WebhookRawDeliveryRedactor.VerifyHash("any-candidate", stored).Should().BeFalse();
    }

    [Fact]
    public void RedactPayload_Masks_Emails_Deterministically_When_Pii_Redaction_Is_On()
    {
        const string payload = """
            {"type":"invoice.paid","customer":{"email":"player@example.org","name":"Ada"},"billing_email":"player@example.org"}
            """;

        var redacted = WebhookRawDeliveryRedactor.RedactPayload(payload, redactPii: true);

        redacted.Should().NotContain(SampleEmail);
        // Deterministic token: the same address maps to the same pseudonym, so occurrences
        // stay correlatable across rows.
        var tokens = redacted.Split("\"").Where(part => part.StartsWith(WebhookRawDeliveryRedactor.EmailPrefix, StringComparison.Ordinal)).ToList();
        tokens.Should().HaveCount(2).And.OnlyContain(token => token == tokens[0]);
        redacted.Should().Contain("Ada");
    }

    [Fact]
    public void RedactPayload_Keeps_Emails_When_Pii_Redaction_Is_Off_But_Still_Hashes_Secrets()
    {
        const string payload = """
            {"type":"payment.succeeded","customer":{"email":"player@example.org"},"credential":"Bearer ya29.a0ARrdaM-synthetic-token-1234567890"}
            """;

        var redacted = WebhookRawDeliveryRedactor.RedactPayload(payload, redactPii: false);

        redacted.Should().Contain(SampleEmail, "RedactPii=false only disables PII masking");
        redacted.Should().NotContain("ya29.a0ARrdaM-synthetic-token-1234567890", "secrets are hashed unconditionally");
        redacted.Should().Contain(WebhookRawDeliveryRedactor.HashPrefix);
    }

    [Fact]
    public void RedactPayload_Hashes_Provider_Secret_Keys_In_Json_Values()
    {
        // Construct at runtime: literal provider-secret prefixes trip secret scanners.
        var fakeSecretKey = string.Concat("sk", "_live_", "SYNTHETICKEY000000000000000");
        var fakeWhsec = string.Concat("wh", "sec_", "SYNTHETICWHSEC0000000000000");
        var payload = "{\"type\":\"account.updated\",\"keys\":{\"secret\":\"" + fakeSecretKey + "\",\"webhook\":\"" + fakeWhsec + "\"}}";

        var redacted = WebhookRawDeliveryRedactor.RedactPayload(payload, redactPii: true);

        redacted.Should().NotContain(fakeSecretKey);
        redacted.Should().NotContain(fakeWhsec);
        redacted.Should().Contain(WebhookRawDeliveryRedactor.HashPrefix);
    }

    [Fact]
    public void RedactPayload_Falls_Back_To_Plain_Text_Pass_For_Non_Json_Bodies()
    {
        const string body = "bt_signature=synthetic%2Bsignature&bt_payload=%7B%22email%22%3A%22player%40example.org%22%7D&contact=player@example.org";

        var redacted = WebhookRawDeliveryRedactor.RedactPayload(body, redactPii: true);

        redacted.Should().NotContain(SampleEmail, "the plain-text fallback must still scrub PII");
        redacted.Should().Contain("bt_payload", "non-secret structure is retained");
    }

    [Fact]
    public void RedactPayload_Preserves_Json_Structure_And_Non_Sensitive_Fields()
    {
        const string payload = """
            {"id":"evt_123","amount":4200,"currency":"usd","livemode":false,"metadata":{"tenant_id":"0f1e2d3c-4b5a-6978-8976-a5b4c3d2e1f0"}}
            """;

        var redacted = WebhookRawDeliveryRedactor.RedactPayload(payload, redactPii: true);
        var document = JsonSerializer.Deserialize<JsonElement>(redacted);

        document.GetProperty("id").GetString().Should().Be("evt_123");
        document.GetProperty("amount").GetInt32().Should().Be(4200);
        document.GetProperty("currency").GetString().Should().Be("usd");
        document.GetProperty("livemode").GetBoolean().Should().BeFalse();
        document.GetProperty("metadata").GetProperty("tenant_id").GetString().Should().Be("0f1e2d3c-4b5a-6978-8976-a5b4c3d2e1f0");
    }

    [Fact]
    public void RedactHeaderValue_Hashes_Overlong_Non_Sensitive_Values()
    {
        var oversized = new string('x', WebhookRawDeliveryRedactor.MaxVerbatimHeaderValueLength + 1);

        var redacted = WebhookRawDeliveryRedactor.RedactHeaderValue("X-Long-Debug", oversized, redactPii: true);

        redacted.Should().StartWith(WebhookRawDeliveryRedactor.HashPrefix);
    }

    [Fact]
    public void HashToken_Is_Stable_And_Length_Bounded()
    {
        var first = WebhookRawDeliveryRedactor.HashToken(SampleSignature);
        var second = WebhookRawDeliveryRedactor.HashToken(SampleSignature);

        first.Should().Be(second);
        first.Should().HaveLength(WebhookRawDeliveryRedactor.HashPrefix.Length + 64);
    }

    [Fact]
    public void RedactEmail_Is_Case_Insensitive_And_Deterministic()
    {
        WebhookRawDeliveryRedactor.RedactEmail("Player@Example.ORG")
            .Should().Be(WebhookRawDeliveryRedactor.RedactEmail("player@example.org"));
        WebhookRawDeliveryRedactor.RedactEmail("other@example.org")
            .Should().NotBe(WebhookRawDeliveryRedactor.RedactEmail("player@example.org"));
    }
}
