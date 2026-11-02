using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GameGuild.Identity.Authorization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authorization.UnitTests.Services;

/// <summary>
///     Issue #331 "expiration analytics and reporting": dashboard bucketing, the audit-log
///     expiration funnel, and the combined report with attention flags.
/// </summary>
public sealed class PermissionExpirationAnalyticsServiceTests
{
    private static readonly Guid TenantA = Guid.NewGuid();

    private readonly Mock<ITenantPermissionRepository> _permissionRepo = new();
    private readonly Mock<IPermissionAuditLogRepository> _auditRepo = new();
    private readonly PermissionExpirationOptions _options = new();
    private PermissionExpirationAnalyticsService CreateSut() => new(
        _permissionRepo.Object,
        _auditRepo.Object,
        Options.Create(_options),
        NullLogger<PermissionExpirationAnalyticsService>.Instance);

    private static TenantPermission Grant(DateTime? expiresAt, bool isActive = true, string[]? permissions = null) => new()
    {
        Id = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        TenantId = TenantA,
        Permissions = permissions ?? new[] { "courses:read" },
        ExpiresAt = expiresAt,
        IsActive = isActive
    };

    private void SetupGrants(params TenantPermission[] grants) =>
        _permissionRepo
            .Setup(x => x.GetByTenantAsync(TenantA, It.IsAny<CancellationToken>()))
            .ReturnsAsync(grants.ToList());

    // ── Summary / dashboard buckets ────────────────────────────

    [Fact]
    public async Task GetSummaryAsync_BucketsGrantsByTimeToExpiry()
    {
        var now = SystemClock.UtcNow;
        SetupGrants(
            Grant(now.AddHours(-2)),                    // expired, pending processing
            Grant(now.AddHours(6)),                     // within 24 hours
            Grant(now.AddHours(48)),                    // 1-to-3 days
            Grant(now.AddDays(5)),                      // 3-to-7 days
            Grant(now.AddDays(14)),                     // 7-to-30 days
            Grant(now.AddDays(45)),                     // beyond 30 days
            Grant(null),                                // permanent
            Grant(now.AddDays(2), isActive: false));    // inactive

        var summary = await CreateSut().GetSummaryAsync(TenantA);

        summary.TotalGrants.Should().Be(8);
        summary.ActiveTemporary.Should().Be(6);
        summary.ActivePermanent.Should().Be(1);
        summary.Inactive.Should().Be(1);

        var counts = summary.Buckets.ToDictionary(b => b.Bucket, b => b.Count);
        counts["expired-pending-processing"].Should().Be(1);
        counts["within-24-hours"].Should().Be(1);
        counts["1-to-3-days"].Should().Be(1);
        counts["3-to-7-days"].Should().Be(1);
        counts["7-to-30-days"].Should().Be(1);
        counts["beyond-30-days"].Should().Be(1);
        counts["permanent"].Should().Be(1);
        counts["inactive"].Should().Be(1);
        counts.Values.Sum().Should().Be(8);
    }

    [Fact]
    public async Task GetSummaryAsync_BreaksDownExpiringGrantsByPermission_MostUrgentFirst()
    {
        var now = SystemClock.UtcNow;
        SetupGrants(
            Grant(now.AddDays(2), permissions: new[] { "billing:refunds" }),
            Grant(now.AddDays(1), permissions: new[] { "billing:refunds" }),
            Grant(now.AddDays(3), permissions: new[] { "courses:read" }),
            Grant(now.AddDays(200), permissions: new[] { "far:away" })); // outside the default 7-day window

        var summary = await CreateSut().GetSummaryAsync(TenantA);

        summary.ExpiringByPermission.Should().HaveCount(2);
        var refunds = summary.ExpiringByPermission.Single(b => b.Permission == "billing:refunds");
        refunds.ExpiringCount.Should().Be(2);
        refunds.EarliestExpiration.Should().Be(now.AddDays(1));
        summary.ExpiringByPermission[0].Permission.Should().Be("billing:refunds"); // earliest expiry first
        summary.ExpiringByPermission.Should().NotContain(b => b.Permission == "far:away");
    }

    // ── Trends / expiration funnel ─────────────────────────────

    [Fact]
    public async Task GetTrendsAsync_CountsExpirationsAndMutationPerDay()
    {
        var day1 = new DateTime(2026, 10, 28, 0, 0, 0, DateTimeKind.Utc);
        var day3 = new DateTime(2026, 10, 30, 0, 0, 0, DateTimeKind.Utc);
        var logs = new List<PermissionAuditLog>
        {
            // Two automatic expirations on day 1.
            new() { OperationType = PermissionOperationType.Expire, ResourceType = "TenantPermission", Timestamp = day1.AddHours(3) },
            new() { OperationType = PermissionOperationType.Expire, ResourceType = "TenantPermission", Timestamp = day1.AddHours(5) },
            // One set + one clear on day 3 (expiration-shaped old/new values).
            new() { OperationType = PermissionOperationType.Update, ResourceType = "TenantPermission", OldValue = "permanent", NewValue = "2026-11-05T00:00:00.0000000Z", Timestamp = day3.AddHours(1) },
            new() { OperationType = PermissionOperationType.Update, ResourceType = "TenantPermission", OldValue = "2026-11-05T00:00:00.0000000Z", NewValue = "permanent", Timestamp = day3.AddHours(2) },
            // A grant/revoke-shaped Update on day 3 that must NOT count as an expiration mutation.
            new() { OperationType = PermissionOperationType.Update, ResourceType = "TenantPermission", OldValue = "[\"Read\"]", NewValue = "[\"Read\",\"Write\"]", Timestamp = day3.AddHours(3) },
            // An expiration-shaped Update on a different resource type: must not count.
            new() { OperationType = PermissionOperationType.Update, ResourceType = "ContentTypeGrant", OldValue = "permanent", NewValue = "2026-11-05T00:00:00.0000000Z", Timestamp = day3.AddHours(4) }
        };

        _auditRepo
            .Setup(x => x.GetByDateRangeAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), TenantA, It.IsAny<CancellationToken>()))
            .ReturnsAsync(logs);

        var trends = await CreateSut().GetTrendsAsync(TenantA, day1, day3);

        trends.Should().HaveCount(3);
        trends[0].Date.Should().Be(day1.Date);
        trends[0].ExpirationsProcessed.Should().Be(2);
        trends[0].ExpirationMutations.Should().Be(0);
        trends[1].Date.Should().Be(day1.Date.AddDays(1));
        trends[1].ExpirationsProcessed.Should().Be(0);
        trends[2].Date.Should().Be(day3.Date);
        trends[2].ExpirationsProcessed.Should().Be(0);
        trends[2].ExpirationMutations.Should().Be(2);
        trends[2].ExpirationsCleared.Should().Be(1);
    }

    [Fact]
    public async Task GetTrendsAsync_RejectsInvertedPeriod()
    {
        var from = new DateTime(2026, 10, 30, 0, 0, 0, DateTimeKind.Utc);
        var act = () => CreateSut().GetTrendsAsync(TenantA, from, from.AddDays(-1));

        await act.Should().ThrowAsync<ArgumentException>();
    }

    // ── Report ─────────────────────────────────────────────────

    [Fact]
    public async Task GetReportAsync_CombinesSummaryTrendsAndFlags()
    {
        var now = SystemClock.UtcNow;
        var periodStart = now.AddDays(-7);
        var periodEnd = now;

        SetupGrants(
            Grant(now.AddHours(-1)),  // expired, pending processing -> attention flag
            Grant(null));             // permanent

        _auditRepo
            .Setup(x => x.GetByDateRangeAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), TenantA, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PermissionAuditLog>
            {
                new() { OperationType = PermissionOperationType.Expire, ResourceType = "TenantPermission", Timestamp = periodStart.AddHours(2) }
            });

        var report = await CreateSut().GetReportAsync(TenantA, periodStart, periodEnd);

        report.TenantId.Should().Be(TenantA);
        report.Summary.TotalGrants.Should().Be(2);
        report.Trends.Should().HaveCount(8);
        report.TotalExpirationsProcessed.Should().Be(1);
        report.AttentionFlags.Should().Contain(f => f.Contains("pending background processing"));
    }

    [Fact]
    public async Task GetReportAsync_FlagsPermanentGrantsUnderRequireExpirationRule()
    {
        _options.GlobalRule = new PermissionExpirationRule { RequireExpiration = true };
        var now = SystemClock.UtcNow;
        SetupGrants(Grant(null), Grant(now.AddDays(5)));

        _auditRepo
            .Setup(x => x.GetByDateRangeAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), TenantA, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PermissionAuditLog>());

        var report = await CreateSut().GetReportAsync(TenantA, now.AddDays(-1), now);

        report.AttentionFlags.Should().Contain(f => f.Contains("permanent grant"));
    }

    [Fact]
    public async Task GetReportAsync_HealthyTenant_HasNoAttentionFlags()
    {
        var now = SystemClock.UtcNow;
        SetupGrants(Grant(now.AddDays(5)));

        _auditRepo
            .Setup(x => x.GetByDateRangeAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), TenantA, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PermissionAuditLog>());

        var report = await CreateSut().GetReportAsync(TenantA, now.AddDays(-1), now);

        report.AttentionFlags.Should().BeEmpty();
    }

    [Fact]
    public async Task GetReportAsync_RejectsInvertedPeriod()
    {
        var now = SystemClock.UtcNow;
        SetupGrants();

        var act = () => CreateSut().GetReportAsync(TenantA, now, now.AddDays(-1));

        await act.Should().ThrowAsync<ArgumentException>();
    }
}
