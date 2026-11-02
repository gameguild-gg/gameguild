using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GameGuild.Commerce.Payments.UnitTests.FraudDetection;

/// <summary>
///     Issue #409 remainder: cross-account device fingerprint signal — the same device
///     presented on multiple customer accounts raises Device.CrossAccountUsage, scaling
///     with the extra accounts and capped per signal.
/// </summary>
public class DeviceFingerprintFraudSignalProviderTests : IAsyncDisposable
{
    private readonly PaymentsPersistenceTestDbContext _context = new(
        new DbContextOptionsBuilder<PaymentsPersistenceTestDbContext>()
            .UseInMemoryDatabase($"fraud-device-{Guid.NewGuid():N}")
            .Options);

    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public async Task EvaluateAsync_ShouldFire_WhenTheDeviceWasSeenOnAnotherAccount()
    {
        await SeedPaymentAsync(deviceFingerprint: "fp-shared", externalCustomerId: "cus_other");

        var signals = await CreateProvider().EvaluateAsync(
            Context(deviceFingerprint: "fp-shared", externalCustomerId: "cus_current"),
            Options(),
            CancellationToken.None);

        var signal = signals.SingleOrDefault(s => s.SignalType == "Device.CrossAccountUsage");
        signal.Should().NotBeNull();
        signal!.Weight.Should().Be(30);
        signal.Details.Should().Contain("\"deviceSharedByAccounts\":2");
    }

    [Fact]
    public async Task EvaluateAsync_ShouldScaleWithExtraAccounts_AndCapAtMaxSignalScore()
    {
        await SeedPaymentAsync(deviceFingerprint: "fp-ring", externalCustomerId: "cus_a");
        await SeedPaymentAsync(deviceFingerprint: "fp-ring", externalCustomerId: "cus_b");
        await SeedPaymentAsync(deviceFingerprint: "fp-ring", externalCustomerId: "cus_c");

        var signals = await CreateProvider().EvaluateAsync(
            Context(deviceFingerprint: "fp-ring", externalCustomerId: "cus_current"),
            Options(),
            CancellationToken.None);

        // 4 shared accounts = base 30 + 2 extra * 30 = 90, capped at MaxSignalScore 50.
        var signal = signals.SingleOrDefault(s => s.SignalType == "Device.CrossAccountUsage");
        signal.Should().NotBeNull();
        signal!.Weight.Should().Be(Options().MaxSignalScore);
    }

    [Fact]
    public async Task EvaluateAsync_ShouldCountSubscriptionFallback_ForUnboundAttempts()
    {
        await SeedPaymentAsync(deviceFingerprint: "fp-sub", externalCustomerId: null, subscriptionId: Guid.NewGuid());

        var signals = await CreateProvider().EvaluateAsync(
            Context(deviceFingerprint: "fp-sub", externalCustomerId: null, subscriptionId: Guid.NewGuid()),
            Options(),
            CancellationToken.None);

        signals.Should().ContainSingle(s => s.SignalType == "Device.CrossAccountUsage");
    }

    [Fact]
    public async Task EvaluateAsync_ShouldNotFire_WhenTheDeviceOnlyServedTheCurrentAccount()
    {
        await SeedPaymentAsync(deviceFingerprint: "fp-solo", externalCustomerId: "cus_current");

        var signals = await CreateProvider().EvaluateAsync(
            Context(deviceFingerprint: "fp-solo", externalCustomerId: "cus_current"),
            Options(),
            CancellationToken.None);

        signals.Should().BeEmpty();
    }

    [Fact]
    public async Task EvaluateAsync_ShouldIgnoreStaleAttempts_AndStaySilentWithoutAFingerprint()
    {
        var options = new FraudAssessmentOptions { CrossAccountDeviceLookbackDays = 1 };
        await SeedPaymentAsync(
            deviceFingerprint: "fp-stale",
            externalCustomerId: "cus_old",
            createdAt: SystemClock.UtcNow.AddDays(-5));

        var provider = CreateProvider();

        var stale = await provider.EvaluateAsync(
            Context(deviceFingerprint: "fp-stale", externalCustomerId: "cus_current"), options, CancellationToken.None);
        var noFingerprint = await provider.EvaluateAsync(
            Context(deviceFingerprint: null, externalCustomerId: "cus_current"), options, CancellationToken.None);

        stale.Should().BeEmpty("the shared attempt is outside the look-back window");
        noFingerprint.Should().BeEmpty("an attempt without a fingerprint cannot correlate accounts");
    }

    private DeviceFingerprintFraudSignalProvider CreateProvider() =>
        new(_context, NullLogger<DeviceFingerprintFraudSignalProvider>.Instance);

    private static FraudAssessmentOptions Options() => new();

    private FraudSignalContext Context(
        string? deviceFingerprint,
        string? externalCustomerId,
        Guid? subscriptionId = null) =>
        new(
            _tenantId,
            PaymentId: Guid.NewGuid(),
            SubscriptionId: subscriptionId ?? Guid.NewGuid(),
            ExternalCustomerId: externalCustomerId,
            PaymentMethodId: "pm_test",
            Amount: 25m,
            Currency: "USD",
            DeviceFingerprint: deviceFingerprint);

    private async Task SeedPaymentAsync(
        string deviceFingerprint,
        string? externalCustomerId,
        Guid? subscriptionId = null,
        DateTime? createdAt = null)
    {
        var payment = Payment.Create(
            _tenantId,
            10m,
            "USD",
            $"key-{Guid.NewGuid():N}",
            subscriptionId: subscriptionId,
            externalCustomerId: externalCustomerId);
        payment.RecordFraudAttemptContext(null, deviceFingerprint);

        if (createdAt.HasValue)
        {
            payment.CreatedAt = createdAt.Value;
        }

        _context.Set<Payment>().Add(payment);
        await _context.SaveChangesAsync();
    }

    public ValueTask DisposeAsync() => _context.DisposeAsync();
}
