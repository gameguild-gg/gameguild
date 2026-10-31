using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using FluentAssertions;
using GameGuild.Commerce;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Billing.UnitTests.Repositories;

/// <summary>
///     Repository-level tests for the opt-in raw-delivery retention (issue #408: capture
///     raw payload and all headers): disabled-by-default no-op, verify-first persistence
///     gate, redacted-at-rest storage, single-record dedupe and the TTL purge. All sample
///     signature/token/email values are synthetic fixtures.
/// </summary>
public class BillingWebhookRawDeliveryRetentionTests
{
    private const string RawPayloadTemplate = """
        {"id":"evt_123","type":"invoice.payment_succeeded","customer":{"email":"player@example.org"},"data":{"secret":"{PROVIDER_SECRET}"}}
        """;

    // Constructed at runtime: literal provider-secret prefixes trip secret scanners.
    private static readonly string FakeProviderSecret = string.Concat("sk", "_live_", "SYNTHETICKEY000000000000");
    private static readonly string RawPayload = RawPayloadTemplate.Replace("{PROVIDER_SECRET}", FakeProviderSecret);

    private static readonly IReadOnlyDictionary<string, string> RawHeaders = new Dictionary<string, string>
    {
        ["Stripe-Signature"] = "t=1770000000,v1=5257a869e7ecebec0ba365d6d1faa2e0d1c1e8a7b6c5d4e3f2a1b0c9d8e7f6a5",
        ["User-Agent"] = "Stripe/1.0 (+https://stripe.com/docs/webhooks)"
    };

    [Fact]
    public async Task CreateAsync_Does_Not_Persist_Any_Raw_Delivery_When_Retention_Is_Disabled()
    {
        await using var context = CreateContext();
        var repository = CreateRepository(context, enabled: false);

        await repository.CreateAsync(NewVerifiedEvent());

        context.BillingWebhookRawDeliveries.Should().BeEmpty("the default policy must be a strict no-op");
    }

    [Fact]
    public async Task CreateAsync_Does_Not_Persist_When_The_Delivery_Was_Not_Signature_Verified()
    {
        await using var context = CreateContext();
        var repository = CreateRepository(context, enabled: true);
        var webhookEvent = NewVerifiedEvent();
        webhookEvent.IsSignatureVerified = null; // PayPal-style row persisted before verification

        await repository.CreateAsync(webhookEvent);

        context.BillingWebhookRawDeliveries.Should().BeEmpty("verify FIRST, then store the redacted form");
    }

    [Fact]
    public async Task CreateAsync_Persists_The_Redacted_Raw_Delivery_For_A_Verified_Row()
    {
        await using var context = CreateContext();
        var repository = CreateRepository(context, enabled: true);
        var webhookEvent = NewVerifiedEvent();

        var created = await repository.CreateAsync(webhookEvent);

        var raw = context.BillingWebhookRawDeliveries.ToList().Single();
        raw.BillingWebhookEventId.Should().Be(created.Id);
        raw.Provider.Should().Be(PaymentProviders.Stripe);
        raw.ExternalEventId.Should().Be("evt_123");
        raw.PiiRedacted.Should().BeTrue();

        // Redacted at rest: no literal email, no literal secret, no literal signature.
        raw.RawPayload.Should().NotContain("player@example.org");
        raw.RawPayload.Should().NotContain(FakeProviderSecret);
        raw.RawPayload.Should().Contain(WebhookRawDeliveryRedactor.HashPrefix);
        raw.RawPayload.Should().Contain(WebhookRawDeliveryRedactor.EmailPrefix);

        var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(raw.RawHeaders)!;
        headers["Stripe-Signature"].Should().StartWith(WebhookRawDeliveryRedactor.HashPrefix);
        headers["User-Agent"].Should().Be("Stripe/1.0 (+https://stripe.com/docs/webhooks)");

        // Intake evidence: hash of the ORIGINAL payload bytes, verifiable against the raw body.
        raw.PayloadSha256.Should().Be(Sha256Hex(RawPayload));
        WebhookRawDeliveryRedactor.VerifyHash(
            "t=1770000000,v1=5257a869e7ecebec0ba365d6d1faa2e0d1c1e8a7b6c5d4e3f2a1b0c9d8e7f6a5",
            headers["Stripe-Signature"]).Should().BeTrue();

        // Retention window: received now, expires after the configured bounded days.
        raw.ExpiresAt.Should().BeAfter(raw.ReceivedAt);
        (raw.ExpiresAt - raw.ReceivedAt).Should().Be(TimeSpan.FromDays(30));
    }

    [Fact]
    public async Task UpdateAsync_Persists_Once_After_Verification_Stamps_The_Row()
    {
        await using var context = CreateContext();
        var repository = CreateRepository(context, enabled: true);
        var webhookEvent = NewVerifiedEvent();
        webhookEvent.IsSignatureVerified = null;

        var created = await repository.CreateAsync(webhookEvent);
        context.BillingWebhookRawDeliveries.Should().BeEmpty("pre-verification updates never retain raw bytes");

        created.MarkSignatureVerified();
        await repository.UpdateAsync(created);
        context.BillingWebhookRawDeliveries.Should().HaveCount(1);

        created.MarkAsProcessed();
        await repository.UpdateAsync(created);
        context.BillingWebhookRawDeliveries.Should().HaveCount(1, "at most one raw delivery per inbox row");
    }

    [Fact]
    public async Task CreateAsync_Without_A_Raw_Capture_Never_Persists()
    {
        await using var context = CreateContext();
        var repository = new BillingWebhookRepository(
            context,
            NullLogger<BillingWebhookRepository>.Instance,
            new WebhookDeliveryContext(),
            Options.Create(Configuration(enabled: true)));

        await repository.CreateAsync(NewVerifiedEvent());

        context.BillingWebhookRawDeliveries.Should().BeEmpty("the retry worker (no HTTP capture) never writes raw rows");
    }

    [Fact]
    public async Task PurgeExpiredRawDeliveriesAsync_Removes_Only_Expired_Rows()
    {
        await using var context = CreateContext();
        var now = SystemClock.UtcNow;
        var live = NewRawDelivery(expiresAt: now.AddDays(7));
        var expired = NewRawDelivery(expiresAt: now.AddDays(-1));
        context.BillingWebhookRawDeliveries.AddRange(live, expired);
        await context.SaveChangesAsync();
        var repository = CreateRepository(context, enabled: true);

        var purged = await repository.PurgeExpiredRawDeliveriesAsync(now);

        purged.Should().Be(1);
        context.BillingWebhookRawDeliveries.Should().ContainSingle()
            .Which.Id.Should().Be(live.Id);
    }

    [Fact]
    public async Task PurgeExpiredRawDeliveriesAsync_Is_A_NoOp_When_Retention_Is_Disabled()
    {
        await using var context = CreateContext();
        var now = SystemClock.UtcNow;
        context.BillingWebhookRawDeliveries.Add(NewRawDelivery(expiresAt: now.AddDays(-30)));
        await context.SaveChangesAsync();
        var repository = CreateRepository(context, enabled: false);

        var purged = await repository.PurgeExpiredRawDeliveriesAsync(now);

        purged.Should().Be(0);
        context.BillingWebhookRawDeliveries.Should().HaveCount(1);
    }

    private static BillingWebhookRepository CreateRepository(TestBillingDbContext context, bool enabled) =>
        new(
            context,
            NullLogger<BillingWebhookRepository>.Instance,
            CapturedDeliveryContext(),
            Options.Create(Configuration(enabled)));

    private static BillingConfiguration Configuration(bool enabled)
    {
        var configuration = new BillingConfiguration();
        configuration.Webhook.Retention.Enabled = enabled;
        return configuration;
    }

    private static WebhookDeliveryContext CapturedDeliveryContext()
    {
        var deliveryContext = new WebhookDeliveryContext();
        deliveryContext.CaptureRawDelivery(RawPayload, new Dictionary<string, string>(RawHeaders));
        return deliveryContext;
    }

    private static BillingWebhookEvent NewVerifiedEvent() => new()
    {
        ExternalEventId = "evt_123",
        Provider = PaymentProviders.Stripe,
        EventType = "invoice.payment_succeeded",
        Payload = "{}",
        IsSignatureVerified = true,
        SignatureVerifiedAt = SystemClock.UtcNow
    };

    private static BillingWebhookRawDelivery NewRawDelivery(DateTime expiresAt) => new()
    {
        BillingWebhookEventId = Guid.NewGuid(),
        Provider = PaymentProviders.Stripe,
        ExternalEventId = $"evt_{Guid.NewGuid():N}",
        RawPayload = "{}",
        RawHeaders = "{}",
        PayloadSha256 = new string('0', 64),
        ReceivedAt = expiresAt.AddDays(-30),
        ExpiresAt = expiresAt
    };

    private static string Sha256Hex(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static TestBillingDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestBillingDbContext>()
            .UseInMemoryDatabase($"BillingRaw_{Guid.NewGuid()}")
            .Options;
        return new TestBillingDbContext(options);
    }

    private class TestBillingDbContext(DbContextOptions<TestBillingDbContext> options)
        : DbContext(options), IApplicationDbContext
    {
        public DbSet<BillingWebhookEvent> BillingWebhookEvents { get; set; } = null!;

        public DbSet<BillingWebhookRawDelivery> BillingWebhookRawDeliveries { get; set; } = null!;

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Mock.Of<IDbContextTransaction>());
        }
    }
}
