using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GameGuild.Commerce.Payments.UnitTests.FraudDetection;

public class BlacklistFraudSignalProviderTests : IAsyncDisposable
{
    private readonly PaymentsPersistenceTestDbContext _context = new(
        new DbContextOptionsBuilder<PaymentsPersistenceTestDbContext>()
            .UseInMemoryDatabase($"fraud-blacklist-provider-{Guid.NewGuid():N}")
            .Options);

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _paymentId = Guid.NewGuid();
    private readonly Guid _subscriptionId = Guid.NewGuid();

    private FraudBlacklistService CreateBlacklistService() =>
        new(_context, NullLogger<FraudBlacklistService>.Instance);

    [Fact]
    public async Task EvaluateAsync_Should_Flag_Blacklisted_Payment_Method_Token()
    {
        var method = CreatePaymentMethod(token: "tok_stolen", fingerprint: null);
        _context.Set<PaymentMethod>().Add(method);
        await _context.SaveChangesAsync();
        await AddBlacklistEntryAsync(FraudBlacklistEntryType.PaymentMethodToken, "tok_stolen");

        var signals = await CreateProvider().EvaluateAsync(Context(paymentMethodId: "tok_stolen"), Options(), CancellationToken.None);

        var signal = signals.Should().ContainSingle().Subject;
        signal.SignalType.Should().Be("Blacklist.PaymentMethodToken");
        signal.Weight.Should().Be(50); // BlacklistWeight 60 capped by MaxSignalScore 50
    }

    [Fact]
    public async Task EvaluateAsync_Should_Flag_Blacklisted_Card_Fingerprint_With_Capped_Weight()
    {
        var method = CreatePaymentMethod(token: "tok_ok", fingerprint: "fp_bad");
        _context.Set<PaymentMethod>().Add(method);
        await _context.SaveChangesAsync();
        await AddBlacklistEntryAsync(FraudBlacklistEntryType.CardFingerprint, "fp_bad");

        var signals = await CreateProvider().EvaluateAsync(Context(paymentMethodId: "tok_ok"), Options(), CancellationToken.None);

        var signal = signals.Should().ContainSingle().Subject;
        signal.SignalType.Should().Be("Blacklist.CardFingerprint");
        signal.Weight.Should().Be(50); // BlacklistWeight 60 capped by MaxSignalScore 50
    }

    [Fact]
    public async Task EvaluateAsync_Should_Flag_Blacklisted_External_Customer()
    {
        await AddBlacklistEntryAsync(FraudBlacklistEntryType.ExternalCustomerId, "cus_fraud");

        var signals = await CreateProvider().EvaluateAsync(Context(externalCustomerId: "cus_fraud"), Options(), CancellationToken.None);

        signals.Should().ContainSingle().Which.SignalType.Should().Be("Blacklist.ExternalCustomerId");
    }

    [Fact]
    public async Task EvaluateAsync_Should_Return_No_Signal_When_Whitelist_Override_Matches()
    {
        await AddBlacklistEntryAsync(FraudBlacklistEntryType.ExternalCustomerId, "cus_trusted");
        await AddBlacklistEntryAsync(FraudBlacklistEntryType.ExternalCustomerId, "cus_trusted", isWhitelist: true);

        var signals = await CreateProvider().EvaluateAsync(Context(externalCustomerId: "cus_trusted"), Options(), CancellationToken.None);

        signals.Should().BeEmpty("a manual whitelist override suppresses the blacklist signal");
    }

    [Fact]
    public async Task EvaluateAsync_Should_Return_No_Signal_When_Nothing_Matches()
    {
        var method = CreatePaymentMethod(token: "tok_clean", fingerprint: "fp_clean");
        _context.Set<PaymentMethod>().Add(method);
        await _context.SaveChangesAsync();
        await AddBlacklistEntryAsync(FraudBlacklistEntryType.PaymentMethodToken, "tok_other");

        var signals = await CreateProvider().EvaluateAsync(Context(paymentMethodId: "tok_clean"), Options(), CancellationToken.None);

        signals.Should().BeEmpty();
    }

    [Fact]
    public async Task EvaluateAsync_Should_Not_Leak_The_Raw_Token_Into_The_Evidence()
    {
        var method = CreatePaymentMethod(token: "tok_secret", fingerprint: null);
        _context.Set<PaymentMethod>().Add(method);
        await _context.SaveChangesAsync();
        await AddBlacklistEntryAsync(FraudBlacklistEntryType.PaymentMethodToken, "tok_secret");

        var signals = await CreateProvider().EvaluateAsync(Context(paymentMethodId: "tok_secret"), Options(), CancellationToken.None);

        var details = JsonDocument.Parse(signals.Single().Details).RootElement;
        details.GetProperty("matchedPattern").GetString().Should().Be("tok_secret"); // stored pattern, not derived data
        details.TryGetProperty("token", out _).Should().BeFalse("the evidence must stay bounded");
    }

    [Fact]
    public async Task EvaluateAsync_Should_Flag_Blacklisted_Source_Ip_Address()
    {
        await AddBlacklistEntryAsync(FraudBlacklistEntryType.IpAddress, "203.0.113.*");

        var signals = await CreateProvider().EvaluateAsync(
            Context(sourceIpAddress: "203.0.113.9"),
            Options(),
            CancellationToken.None);

        var signal = signals.Should().ContainSingle().Subject;
        signal.SignalType.Should().Be("Blacklist.IpAddress");
        signal.Weight.Should().Be(50);
    }

    [Fact]
    public async Task EvaluateAsync_Should_Flag_Blacklisted_Device_Fingerprint()
    {
        await AddBlacklistEntryAsync(FraudBlacklistEntryType.DeviceFingerprint, "device_bad");

        var signals = await CreateProvider().EvaluateAsync(
            Context(deviceFingerprint: "device_bad"),
            Options(),
            CancellationToken.None);

        signals.Should().ContainSingle().Which.SignalType.Should().Be("Blacklist.DeviceFingerprint");
    }

    [Fact]
    public async Task EvaluateAsync_Should_Flag_Blacklisted_Exact_Payer_Email()
    {
        await AddBlacklistEntryAsync(FraudBlacklistEntryType.Email, "fraud@example.com");

        var signals = await CreateProvider().EvaluateAsync(
            Context(payerEmail: "fraud@example.com"),
            Options(),
            CancellationToken.None);

        signals.Should().ContainSingle().Which.SignalType.Should().Be("Blacklist.Email");
    }

    [Fact]
    public async Task EvaluateAsync_Should_Flag_Blacklisted_Email_Domain_When_Address_Itself_Is_Not_Listed()
    {
        await AddBlacklistEntryAsync(FraudBlacklistEntryType.EmailDomain, "malicious.com");

        var signals = await CreateProvider().EvaluateAsync(
            Context(payerEmail: "someone@malicious.com"),
            Options(),
            CancellationToken.None);

        signals.Should().ContainSingle().Which.SignalType.Should().Be("Blacklist.EmailDomain");
    }

    [Fact]
    public async Task EvaluateAsync_Should_Not_Double_Flag_When_Both_The_Address_And_Domain_Are_Blacklisted()
    {
        await AddBlacklistEntryAsync(FraudBlacklistEntryType.Email, "fraud@bad.example");
        await AddBlacklistEntryAsync(FraudBlacklistEntryType.EmailDomain, "bad.example");

        var signals = await CreateProvider().EvaluateAsync(
            Context(payerEmail: "fraud@bad.example"),
            Options(),
            CancellationToken.None);

        signals.Should().ContainSingle("the exact address match suppresses the domain check");
    }

    [Fact]
    public void EmailDomainOf_Should_Extract_The_Lowercased_Domain()
    {
        BlacklistFraudSignalProvider.EmailDomainOf("User@Example.COM").Should().Be("example.com");
        BlacklistFraudSignalProvider.EmailDomainOf("no-separator").Should().BeNull();
        BlacklistFraudSignalProvider.EmailDomainOf("@local").Should().BeNull();
        BlacklistFraudSignalProvider.EmailDomainOf("trailing@").Should().BeNull();
    }

    private BlacklistFraudSignalProvider CreateProvider() =>
        new(_context, CreateBlacklistService(), NullLogger<BlacklistFraudSignalProvider>.Instance);

    private FraudSignalContext Context(
        string? externalCustomerId = "cus_regular",
        string? paymentMethodId = null,
        string? sourceIpAddress = null,
        string? deviceFingerprint = null,
        string? payerEmail = null) =>
        new(
            _tenantId,
            _paymentId,
            _subscriptionId,
            externalCustomerId,
            paymentMethodId,
            100m,
            "USD",
            3,
            Country: null,
            SourceIpAddress: sourceIpAddress,
            DeviceFingerprint: deviceFingerprint,
            PayerEmail: payerEmail);

    private static FraudAssessmentOptions Options() => new();

    private PaymentMethod CreatePaymentMethod(string token, string? fingerprint)
    {
        var method = new PaymentMethod(
            _tenantId,
            Guid.NewGuid(),
            "stripe",
            token,
            PaymentMethodType.Card,
            isDefault: false);
        method.UpdateGatewayMetadata(PaymentMethodType.Card, "visa", "4242", 12, 2030, fingerprint);
        return method;
    }

    private async Task AddBlacklistEntryAsync(FraudBlacklistEntryType entryType, string value, bool isWhitelist = false)
    {
        var entry = FraudBlacklistEntry.Create(_tenantId, entryType, value, "unit-test", isWhitelist, FraudBlacklistSource.Manual)!;
        _context.Set<FraudBlacklistEntry>().Add(entry);
        await _context.SaveChangesAsync();
    }

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();
}
