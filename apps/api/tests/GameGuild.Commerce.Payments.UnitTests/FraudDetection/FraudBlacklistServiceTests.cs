using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GameGuild.Commerce.Payments.UnitTests.FraudDetection;

public class FraudBlacklistServiceTests : IAsyncDisposable
{
    private readonly PaymentsPersistenceTestDbContext _context = new(
        new DbContextOptionsBuilder<PaymentsPersistenceTestDbContext>()
            .UseInMemoryDatabase($"fraud-blacklist-{Guid.NewGuid():N}")
            .Options);

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _otherTenantId = Guid.NewGuid();

    private FraudBlacklistService CreateService() =>
        new(_context, NullLogger<FraudBlacklistService>.Instance);

    [Fact]
    public async Task EvaluateAsync_Should_Match_Exact_Entry_And_Return_Evidence()
    {
        await AddEntryAsync(FraudBlacklistEntryType.EmailDomain, "mailinator.com", reason: "disposable provider");

        var result = await CreateService().EvaluateAsync(_tenantId, FraudBlacklistEntryType.EmailDomain, "MAILINATOR.com");

        result.Evaluation.Should().Be(FraudBlacklistEvaluation.Blacklisted);
        result.MatchedPattern.Should().Be("mailinator.com");
        result.Reason.Should().Be("disposable provider");
        result.Source.Should().Be(FraudBlacklistSource.Manual);
    }

    [Fact]
    public async Task EvaluateAsync_Should_Match_Wildcard_Pattern()
    {
        await AddEntryAsync(FraudBlacklistEntryType.EmailDomain, "*.malicious.com");
        await AddEntryAsync(FraudBlacklistEntryType.IpAddress, "203.0.113.*");

        var service = CreateService();
        (await service.EvaluateAsync(_tenantId, FraudBlacklistEntryType.EmailDomain, "mail.malicious.com"))
            .Evaluation.Should().Be(FraudBlacklistEvaluation.Blacklisted);
        (await service.EvaluateAsync(_tenantId, FraudBlacklistEntryType.EmailDomain, "malicious.com"))
            .Evaluation.Should().Be(FraudBlacklistEvaluation.NoMatch);
        (await service.EvaluateAsync(_tenantId, FraudBlacklistEntryType.IpAddress, "203.0.113.77"))
            .Evaluation.Should().Be(FraudBlacklistEvaluation.Blacklisted);
    }

    [Fact]
    public async Task EvaluateAsync_Should_Ignore_Expired_Entries()
    {
        await AddEntryAsync(FraudBlacklistEntryType.CardBin, "411111", expiresAt: SystemClock.UtcNow.AddHours(-1));

        var result = await CreateService().EvaluateAsync(_tenantId, FraudBlacklistEntryType.CardBin, "411111");

        result.Evaluation.Should().Be(FraudBlacklistEvaluation.NoMatch);
    }

    [Fact]
    public async Task EvaluateAsync_Should_Let_Whitelist_Override_Suppress_Blacklist()
    {
        await AddEntryAsync(FraudBlacklistEntryType.EmailDomain, "partner.example", reason: "listed");
        await AddEntryAsync(FraudBlacklistEntryType.EmailDomain, "partner.example", reason: "trusted partner", isWhitelist: true);

        var result = await CreateService().EvaluateAsync(_tenantId, FraudBlacklistEntryType.EmailDomain, "partner.example");

        result.Evaluation.Should().Be(FraudBlacklistEvaluation.Whitelisted);
    }

    [Fact]
    public async Task EvaluateAsync_Should_Be_Tenant_Scoped()
    {
        await AddEntryAsync(FraudBlacklistEntryType.PaymentMethodToken, "tok_bad");

        var result = await CreateService().EvaluateAsync(_otherTenantId, FraudBlacklistEntryType.PaymentMethodToken, "tok_bad");

        result.Evaluation.Should().Be(FraudBlacklistEvaluation.NoMatch);
    }

    [Fact]
    public async Task AddAsync_Should_Be_Idempotent_For_Identical_Live_Entries()
    {
        var first = FraudBlacklistEntry.Create(_tenantId, FraudBlacklistEntryType.DeviceFingerprint, "fp_1", "reason", isWhitelist: false, FraudBlacklistSource.Manual)!;
        var second = FraudBlacklistEntry.Create(_tenantId, FraudBlacklistEntryType.DeviceFingerprint, "FP_1", "other reason", isWhitelist: false, FraudBlacklistSource.Manual)!;

        var service = CreateService();
        var storedFirst = await service.AddAsync(first);
        var storedSecond = await service.AddAsync(second);

        storedSecond.Id.Should().Be(storedFirst.Id, "a normalized duplicate returns the existing row");
        (await _context.Set<FraudBlacklistEntry>().CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task RemoveAsync_Should_SoftDelete_And_Be_Tenant_Scoped()
    {
        var entry = FraudBlacklistEntry.Create(_tenantId, FraudBlacklistEntryType.Email, "fraud@example.com", "reason", isWhitelist: false, FraudBlacklistSource.Manual)!;
        await CreateService().AddAsync(entry);

        var service = CreateService();
        (await service.RemoveAsync(_otherTenantId, entry.Id)).Should().BeFalse("the entry belongs to another tenant");
        (await service.RemoveAsync(_tenantId, entry.Id)).Should().BeTrue();

        (await service.EvaluateAsync(_tenantId, FraudBlacklistEntryType.Email, "fraud@example.com"))
            .Evaluation.Should().Be(FraudBlacklistEvaluation.NoMatch);
    }

    [Fact]
    public async Task ListAsync_Should_Filter_By_Type_And_Exclude_Expired_By_Default()
    {
        await AddEntryAsync(FraudBlacklistEntryType.EmailDomain, "a.example");
        await AddEntryAsync(FraudBlacklistEntryType.EmailDomain, "expired.example", expiresAt: SystemClock.UtcNow.AddHours(-1));
        await AddEntryAsync(FraudBlacklistEntryType.IpAddress, "10.0.0.1");

        var service = CreateService();
        var (items, totalCount) = await service.ListAsync(_tenantId, FraudBlacklistEntryType.EmailDomain);

        totalCount.Should().Be(1);
        items.Should().ContainSingle().Which.Value.Should().Be("a.example");

        var (allTypes, allCount) = await service.ListAsync(_tenantId, includeExpired: true);
        allCount.Should().Be(3);
        allTypes.Should().HaveCount(3);
    }

    [Theory]
    [InlineData("*.malicious.com", "mail.malicious.com", true)]
    [InlineData("*.malicious.com", "evil.malicious.com", true)]
    [InlineData("*.malicious.com", "malicious.com", false)]
    [InlineData("192.168.*", "192.168.1.50", true)]
    [InlineData("192.168.*", "192.169.1.50", false)]
    [InlineData("exact.example", "exact.example", true)]
    [InlineData("exact.example", "other.example", false)]
    [InlineData("user-*@example.com", "user-42@example.com", true)]
    [InlineData("user-*@example.com", "admin@example.com", false)]
    public void FraudBlacklistPatterns_Matches_Should_Handle_Wildcards(string pattern, string value, bool expected)
    {
        FraudBlacklistPatterns.Matches(pattern, value).Should().Be(expected);
    }

    private async Task AddEntryAsync(
        FraudBlacklistEntryType entryType,
        string value,
        string? reason = null,
        bool isWhitelist = false,
        DateTime? expiresAt = null)
    {
        var entry = FraudBlacklistEntry.Create(_tenantId, entryType, value, reason, isWhitelist, FraudBlacklistSource.Manual, expiresAt)!;
        _context.Set<FraudBlacklistEntry>().Add(entry);
        await _context.SaveChangesAsync();
    }

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();
}
