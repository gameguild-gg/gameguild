using FluentAssertions;
using GameGuild.Modules.Authentication;
using Xunit;

namespace GameGuild.Tests.Authentication.Unit.Entities;

/// <summary>
/// Unit tests for the AuthenticationAttemptAnalysis model
/// Tests the properties and behavior of authentication attempt analysis
/// </summary>
public class AuthenticationAttemptAnalysisTests
{
    [Fact]
    public void AuthenticationAttemptAnalysis_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var analysis = new AuthenticationAttemptAnalysis();

        // Assert
        analysis.RiskScore.Should().Be(0);
        analysis.IsSuspicious.Should().BeFalse();
        analysis.RiskFactors.Should().BeEmpty();
        analysis.AnalyzedAt.Should().Be(default(DateTime));
    }

    [Fact]
    public void AuthenticationAttemptAnalysis_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var analyzedAt = DateTime.UtcNow;
        var riskFactors = new List<string> { "NewDevice", "UnusualLocation", "HighFrequency" };

        // Act
        var analysis = new AuthenticationAttemptAnalysis
        {
            RiskScore = 85,
            IsSuspicious = true,
            RiskFactors = riskFactors,
            AnalyzedAt = analyzedAt
        };

        // Assert
        analysis.RiskScore.Should().Be(85);
        analysis.IsSuspicious.Should().BeTrue();
        analysis.RiskFactors.Should().BeEquivalentTo(riskFactors);
        analysis.AnalyzedAt.Should().Be(analyzedAt);
    }

    [Fact]
    public void AuthenticationAttemptAnalysis_ShouldHandleLowRiskScore()
    {
        // Arrange & Act
        var analysis = new AuthenticationAttemptAnalysis
        {
            RiskScore = 15,
            IsSuspicious = false,
            RiskFactors = new List<string>(),
            AnalyzedAt = DateTime.UtcNow
        };

        // Assert
        analysis.RiskScore.Should().Be(15);
        analysis.IsSuspicious.Should().BeFalse();
        analysis.RiskFactors.Should().BeEmpty();
    }
}
