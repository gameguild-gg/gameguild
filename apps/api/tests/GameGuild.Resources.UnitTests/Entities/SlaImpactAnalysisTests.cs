using FluentAssertions;
using Xunit;

namespace GameGuild.Resources.UnitTests.Entities;

public class SlaImpactAnalysisTests
{
    [Fact]
    public void CalculateDuration_WithEndTime_SetsDurationSeconds()
    {
        var analysis = new SlaImpactAnalysis
        {
            ViolationStartTime = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc),
            ViolationEndTime = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc)
        };

        analysis.CalculateDuration();

        analysis.DurationSeconds.Should().Be(7200);
    }

    [Fact]
    public void CalculateDuration_WithoutEndTime_DoesNotChangeDuration()
    {
        var analysis = new SlaImpactAnalysis
        {
            ViolationStartTime = DateTime.UtcNow.AddHours(-2),
            ViolationEndTime = null,
            DurationSeconds = 15
        };

        analysis.CalculateDuration();

        analysis.DurationSeconds.Should().Be(15);
    }

    [Fact]
    public void CalculateDeviation_WithExpectedValue_ComputesRoundedPercentage()
    {
        var analysis = new SlaImpactAnalysis { ExpectedValue = 100, ActualValue = 120 };

        analysis.CalculateDeviation();

        analysis.DeviationPercentage.Should().Be(20.00m);
    }

    [Fact]
    public void CalculateDeviation_WithZeroExpectedValue_DoesNotChangeDeviation()
    {
        var analysis = new SlaImpactAnalysis { ExpectedValue = 0, ActualValue = 50, DeviationPercentage = 3.5m };

        analysis.CalculateDeviation();

        analysis.DeviationPercentage.Should().Be(3.5m);
    }

    [Fact]
    public void Resolve_SetsResolutionFieldsAndMitigation()
    {
        var before = DateTime.UtcNow;
        var userId = Guid.NewGuid();
        var analysis = new SlaImpactAnalysis
        {
            ViolationStartTime = DateTime.UtcNow.AddHours(-1),
            ViolationEndTime = null
        };

        analysis.Resolve(userId, "fixed the issue");

        analysis.IsResolved.Should().BeTrue();
        analysis.ResolvedByUserId.Should().Be(userId);
        analysis.ResolvedAt.Should().NotBeNull();
        analysis.ResolvedAt.Should().BeOnOrAfter(before);
        analysis.ViolationEndTime.Should().NotBeNull();
        analysis.MitigationActions.Should().Be("fixed the issue");
        analysis.DurationSeconds.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Resolve_WithExistingEndTime_DoesNotOverwriteViolationEndTime()
    {
        var endTime = new DateTime(2026, 1, 1, 11, 30, 0, DateTimeKind.Utc);
        var analysis = new SlaImpactAnalysis
        {
            ViolationStartTime = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc),
            ViolationEndTime = endTime
        };

        analysis.Resolve(Guid.NewGuid());

        analysis.ViolationEndTime.Should().Be(endTime);
    }

    [Fact]
    public void IsCriticalAndOngoing_WhenCriticalAndUnresolved_ReturnsTrue()
    {
        var analysis = new SlaImpactAnalysis
        {
            Severity = SlaViolationSeverity.Critical,
            IsResolved = false,
            ViolationEndTime = null
        };

        analysis.IsCriticalAndOngoing().Should().BeTrue();
    }

    [Fact]
    public void IsCriticalAndOngoing_WhenNotCritical_ReturnsFalse()
    {
        var analysis = new SlaImpactAnalysis
        {
            Severity = SlaViolationSeverity.High,
            IsResolved = false,
            ViolationEndTime = null
        };

        analysis.IsCriticalAndOngoing().Should().BeFalse();
    }

    [Fact]
    public void IsCriticalAndOngoing_WhenResolved_ReturnsFalse()
    {
        var analysis = new SlaImpactAnalysis
        {
            Severity = SlaViolationSeverity.Critical,
            IsResolved = true,
            ViolationEndTime = null
        };

        analysis.IsCriticalAndOngoing().Should().BeFalse();
    }

    [Fact]
    public void ExceedsDuration_WhenAboveThreshold_ReturnsTrue()
    {
        var analysis = new SlaImpactAnalysis
        {
            ViolationStartTime = DateTime.UtcNow.AddMinutes(-30),
            ViolationEndTime = null
        };

        analysis.ExceedsDuration(20).Should().BeTrue();
    }

    [Fact]
    public void ExceedsDuration_WhenBelowThreshold_ReturnsFalse()
    {
        var analysis = new SlaImpactAnalysis
        {
            ViolationStartTime = DateTime.UtcNow.AddMinutes(-5),
            ViolationEndTime = null
        };

        analysis.ExceedsDuration(60).Should().BeFalse();
    }
}
