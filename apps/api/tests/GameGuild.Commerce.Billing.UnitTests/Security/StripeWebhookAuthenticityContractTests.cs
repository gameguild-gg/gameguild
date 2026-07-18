using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace GameGuild.Commerce.Billing.UnitTests.Security;

public sealed class StripeWebhookAuthenticityContractTests
{
    private const string Secret = "whsec_contract_test_secret";
    private const string ApiVersion = "2023-10-16";

    [Fact]
    public void Verify_AcceptsAuthenticCurrentEventAndReturnsProviderScope()
    {
        var tenantId = Guid.NewGuid();
        var payload = CreatePayload("evt_valid", tenantId);
        var verifier = CreateVerifier();

        var result = verifier.Verify(payload, Sign(payload));

        result.EventId.Should().Be("evt_valid");
        result.ProviderEnvironment.Should().Be("test");
        result.ProviderAccountId.Should().Be("platform");
        result.WebhookEndpointId.Should().Be("we_contract_test");
        result.ProviderObjectId.Should().Be("in_contract");
        result.TenantId.Should().Be(tenantId);
        result.Amount.Should().Be(10m);
        result.Currency.Should().Be("USD");
    }

    [Fact]
    public void Verify_RejectsForgedAndWrongSecretSignatures()
    {
        var payload = CreatePayload("evt_forged", Guid.NewGuid());
        var verifier = CreateVerifier();

        var forged = () => verifier.Verify(payload, "t=1,v1=forged");
        var wrongSecret = () => verifier.Verify(payload, Sign(payload, secret: "whsec_wrong"));

        forged.Should().Throw<InvalidWebhookSignatureException>();
        wrongSecret.Should().Throw<InvalidWebhookSignatureException>();
    }

    [Fact]
    public void Verify_RejectsAuthenticButStaleSignature()
    {
        var payload = CreatePayload("evt_stale", Guid.NewGuid());
        var verifier = CreateVerifier(toleranceSeconds: 60);
        var staleTimestamp = DateTimeOffset.UtcNow.AddMinutes(-5).ToUnixTimeSeconds();

        var act = () => verifier.Verify(payload, Sign(payload, staleTimestamp));

        act.Should().Throw<InvalidWebhookSignatureException>();
    }

    [Fact]
    public void Verify_ClassifiesSignedMalformedJsonAsInvalidPayload()
    {
        const string payload = "{not-json";
        var verifier = CreateVerifier();

        var act = () => verifier.Verify(payload, Sign(payload));

        act.Should().Throw<InvalidWebhookPayloadException>();
    }

    [Fact]
    public void Verify_RejectsWrongLivemodeAccountAndSchemaVersion()
    {
        var tenantId = Guid.NewGuid();
        var livePayload = CreatePayload("evt_live", tenantId, liveMode: true);
        var accountPayload = CreatePayload("evt_account", tenantId, account: "acct_wrong");
        var schemaPayload = CreatePayload("evt_schema", tenantId, apiVersion: "2022-11-15");

        var wrongMode = () => CreateVerifier().Verify(livePayload, Sign(livePayload));
        var wrongAccount = () => CreateVerifier(connectedAccountId: "acct_expected")
            .Verify(accountPayload, Sign(accountPayload));
        var wrongSchema = () => CreateVerifier().Verify(schemaPayload, Sign(schemaPayload));

        wrongMode.Should().Throw<InvalidWebhookPayloadException>();
        wrongAccount.Should().Throw<InvalidWebhookPayloadException>();
        wrongSchema.Should().Throw<InvalidWebhookPayloadException>();
    }

    [Fact]
    public void Verify_RetainsOnlyClassifiedFinancialFields()
    {
        var payload = CreatePayload("evt_minimized", Guid.NewGuid());
        var verifier = CreateVerifier();

        var result = verifier.Verify(payload, Sign(payload));

        result.RetainedPayload.Should().Contain("amount_paid");
        result.RetainedPayload.Should().NotContain("customer_email");
        result.RetainedPayload.Should().NotContain("private_note");
        result.PayloadSha256.Should().MatchRegex("^[0-9a-f]{64}$");
        result.VerifiedPayload.Should().Be(payload);
    }

    private static StripeWebhookVerifier CreateVerifier(
        long toleranceSeconds = 300,
        string connectedAccountId = "")
    {
        return new StripeWebhookVerifier(Options.Create(new BillingConfiguration
        {
            Stripe = new StripeSettings
            {
                WebhookSecret = Secret,
                WebhookEndpointId = "we_contract_test",
                ConnectedAccountId = connectedAccountId,
                ApiVersion = ApiVersion,
                LiveMode = false,
                WebhookToleranceSeconds = toleranceSeconds
            }
        }));
    }

    private static string CreatePayload(
        string eventId,
        Guid tenantId,
        bool liveMode = false,
        string? account = null,
        string apiVersion = ApiVersion)
    {
        return JsonSerializer.Serialize(new
        {
            id = eventId,
            @object = "event",
            api_version = apiVersion,
            created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            data = new
            {
                @object = new
                {
                    id = "in_contract",
                    @object = "invoice",
                    subscription = "sub_contract",
                    amount_paid = 1000,
                    currency = "usd",
                    customer_email = "private@example.com",
                    metadata = new
                    {
                        tenant_id = tenantId,
                        private_note = "must-not-retain"
                    }
                }
            },
            livemode = liveMode,
            pending_webhooks = 1,
            request = new { id = (string?)null, idempotency_key = (string?)null },
            type = "invoice.payment_succeeded",
            account
        });
    }

    private static string Sign(
        string payload,
        long? timestamp = null,
        string secret = Secret)
    {
        var resolvedTimestamp = timestamp ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signedPayload = $"{resolvedTimestamp}.{payload}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var signature = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload))).ToLowerInvariant();
        return $"t={resolvedTimestamp},v1={signature}";
    }
}
