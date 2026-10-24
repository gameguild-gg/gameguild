using FluentAssertions;
using GameGuild.Commerce.Products;
using Xunit;

namespace GameGuild.Tests.Commerce.Products.Unit.Services;

/// <summary>
/// Issue #418 timezone-aware validity windows: wall-clock edges supplied in an IANA
/// timezone are normalized to UTC instants at write time, and evaluation distinguishes
/// the two window edges (not-yet-valid vs expired).
/// </summary>
public class PromoValidityWindowTests
{
    [Fact]
    public void ResolveTimeZone_KnownIanaId_Resolves()
    {
        var (zone, resolved) = PromoValidityWindow.ResolveTimeZone("America/Sao_Paulo");

        resolved.Should().BeTrue();
        zone.Id.Should().Be("America/Sao_Paulo");
    }

    [Fact]
    public void ResolveTimeZone_UnknownId_FallsBackToUtc()
    {
        var (zone, resolved) = PromoValidityWindow.ResolveTimeZone("Mars/Olympus_Mons");

        resolved.Should().BeFalse();
        zone.Id.Should().Be("UTC");
    }

    [Fact]
    public void ResolveTimeZone_NullOrEmpty_FallsBackToUtc()
    {
        PromoValidityWindow.ResolveTimeZone(null).Zone.Id.Should().Be("UTC");
        PromoValidityWindow.ResolveTimeZone("  ").Zone.Id.Should().Be("UTC");
    }

    [Fact]
    public void NormalizeToUtc_UnspecifiedKind_TreatsValueAsWallClockInZone()
    {
        var saoPaulo = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        var wallClock = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Unspecified);

        var utc = PromoValidityWindow.NormalizeToUtc(wallClock, saoPaulo);

        // Brazil daylight time starts 2026-11-01; the local midnight is 03:00 UTC.
        utc.Should().NotBeNull();
        utc.Value.Kind.Should().Be(DateTimeKind.Utc);
        var expected = TimeZoneInfo.ConvertTimeToUtc(wallClock, saoPaulo);
        utc.Value.Should().Be(expected);
        utc.Value.Hour.Should().Be(expected.Hour);
    }

    [Fact]
    public void NormalizeToUtc_UtcKind_PassesThroughAsUtc()
    {
        var value = new DateTime(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc);

        var utc = PromoValidityWindow.NormalizeToUtc(value, TimeZoneInfo.Utc);

        utc.Should().Be(value);
    }

    [Fact]
    public void NormalizeToUtc_Null_PassesThrough()
    {
        PromoValidityWindow.NormalizeToUtc(null, TimeZoneInfo.Utc).Should().BeNull();
    }

    [Fact]
    public void ToWallClock_ConvertsUtcInstantToLocalFace()
    {
        var saoPaulo = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        var utc = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

        var wall = PromoValidityWindow.ToWallClock(utc, saoPaulo);

        wall.Should().Be(new DateTime(2026, 6, 1, 9, 0, 0, DateTimeKind.Unspecified));
    }

    [Fact]
    public void Evaluate_BeforeWindowStart_IsNotYetValid()
    {
        var outcome = PromoValidityWindow.Evaluate(
            validFromUtc: DateTime.UtcNow.AddDays(1),
            validUntilUtc: DateTime.UtcNow.AddDays(10),
            utcNow: DateTime.UtcNow);

        outcome.Should().Be(PromoWindowOutcome.NotYetValid);
    }

    [Fact]
    public void Evaluate_AfterWindowEnd_IsExpired()
    {
        var outcome = PromoValidityWindow.Evaluate(
            validFromUtc: DateTime.UtcNow.AddDays(-10),
            validUntilUtc: DateTime.UtcNow.AddDays(-1),
            utcNow: DateTime.UtcNow);

        outcome.Should().Be(PromoWindowOutcome.Expired);
    }

    [Fact]
    public void Evaluate_InsideWindow_IsWithinWindow()
    {
        var outcome = PromoValidityWindow.Evaluate(
            validFromUtc: DateTime.UtcNow.AddDays(-1),
            validUntilUtc: DateTime.UtcNow.AddDays(1),
            utcNow: DateTime.UtcNow);

        outcome.Should().Be(PromoWindowOutcome.WithinWindow);
    }

    [Fact]
    public void Evaluate_UnboundedWindow_IsWithinWindow()
    {
        PromoValidityWindow.Evaluate(null, null, DateTime.UtcNow)
            .Should().Be(PromoWindowOutcome.WithinWindow);
    }

    [Fact]
    public void Evaluate_ValidUntilBoundary_IsExclusive()
    {
        var until = DateTime.UtcNow;

        PromoValidityWindow.Evaluate(null, until, until)
            .Should().Be(PromoWindowOutcome.Expired);
    }

    [Fact]
    public void PromoCode_EvaluateWindow_MapsBothEdges()
    {
        var notYetValid = new PromoCode
        {
            IsActive = true,
            ValidFrom = DateTime.UtcNow.AddDays(1)
        };
        var expired = new PromoCode
        {
            IsActive = true,
            ValidUntil = DateTime.UtcNow.AddDays(-1)
        };

        notYetValid.EvaluateWindow().Should().Be(PromoWindowOutcome.NotYetValid);
        notYetValid.IsCurrentlyValid().Should().BeFalse();
        expired.EvaluateWindow().Should().Be(PromoWindowOutcome.Expired);
        expired.IsCurrentlyValid().Should().BeFalse();
    }
}
