using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

public class AuthenticationAnomalyDetectionServiceTests
{
    private readonly Mock<IAuthenticationAttemptRepository> _attemptRepositoryMock;
    private readonly Mock<IThreatDetectionService> _threatDetectionMock;
    private readonly Mock<IBehavioralAnalysisService> _behavioralAnalysisMock;
    private readonly Mock<ILoginAttemptAnalysisService> _loginAttemptAnalysisMock;
    private readonly Mock<ILogger<AuthenticationAnomalyDetectionService>> _loggerMock;
    private readonly AuthenticationAnomalyDetectionService _service;

    public AuthenticationAnomalyDetectionServiceTests()
    {
        _attemptRepositoryMock = new Mock<IAuthenticationAttemptRepository>();
        _threatDetectionMock = new Mock<IThreatDetectionService>();
        _behavioralAnalysisMock = new Mock<IBehavioralAnalysisService>();
        _loginAttemptAnalysisMock = new Mock<ILoginAttemptAnalysisService>();
        _loggerMock = new Mock<ILogger<AuthenticationAnomalyDetectionService>>();

        _service = new AuthenticationAnomalyDetectionService(
            _attemptRepositoryMock.Object,
            _threatDetectionMock.Object,
            _behavioralAnalysisMock.Object,
            _loginAttemptAnalysisMock.Object,
            _loggerMock.Object
        );
    }

    // ── AnalyzeAttemptAsync (inline logic in facade) ─────────────────

    [Fact]
    public async Task AnalyzeAttemptAsync_WithNoRecentAttempts_ReturnsLowRisk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var ipAddress = "192.168.1.1";
        var userAgent = "Mozilla/5.0";

        _attemptRepositoryMock
            .Setup(x => x.GetRecentAttemptsAsync(userId, It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AuthenticationAttempt>());

        _attemptRepositoryMock
            .Setup(x => x.GetRecentAttemptsByIpAsync(ipAddress, It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AuthenticationAttempt>());

        _attemptRepositoryMock
            .Setup(x => x.GetLastSuccessfulAttemptAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuthenticationAttempt?)null);

        // Act
        var result = await _service.AnalyzeAttemptAsync(userId, ipAddress, userAgent);

        // Assert
        result.Should().NotBeNull();
        result.RiskLevel.Should().Be(RiskLevel.Low);
        result.RiskScore.Should().BeLessThan(30);
    }

    [Fact]
    public async Task AnalyzeAttemptAsync_WithMultipleUserAgentsFromSameIp_IncreasesRiskScore()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var ipAddress = "192.168.1.1";
        var userAgent = "Mozilla/5.0";

        var multipleUserAgentAttempts = Enumerable.Range(0, 15)
            .Select(i => new AuthenticationAttempt
            {
                UserId = userId,
                IpAddress = ipAddress,
                UserAgent = $"UserAgent-{i}",
                IsSuccessful = true,
                DeviceFingerprint = null,
                AttemptedAt = DateTime.UtcNow.AddMinutes(-i)
            })
            .ToList();

        _attemptRepositoryMock
            .Setup(x => x.GetRecentAttemptsAsync(userId, It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AuthenticationAttempt> { multipleUserAgentAttempts[0] });

        _attemptRepositoryMock
            .Setup(x => x.GetRecentAttemptsByIpAsync(ipAddress, It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(multipleUserAgentAttempts);

        _attemptRepositoryMock
            .Setup(x => x.GetLastSuccessfulAttemptAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuthenticationAttempt?)null);

        // Act
        var result = await _service.AnalyzeAttemptAsync(userId, ipAddress, userAgent);

        // Assert
        result.RiskScore.Should().BeGreaterOrEqualTo(30);
        result.RiskFactors.Should().Contain(f => f.Contains("Multiple user agents"));
    }

    [Fact]
    public async Task AnalyzeAttemptAsync_WithRapidAttempts_IncreasesRiskScore()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var ipAddress = "192.168.1.1";
        var userAgent = "Mozilla/5.0";

        var now = DateTime.UtcNow;
        var rapidAttempts = new List<AuthenticationAttempt>
        {
            new() { UserId = userId, IpAddress = ipAddress, UserAgent = userAgent, IsSuccessful = true, DeviceFingerprint = null, AttemptedAt = now.AddMinutes(-1) },
            new() { UserId = userId, IpAddress = ipAddress, UserAgent = userAgent, IsSuccessful = true, DeviceFingerprint = null, AttemptedAt = now.AddMinutes(-2) },
            new() { UserId = userId, IpAddress = ipAddress, UserAgent = userAgent, IsSuccessful = true, DeviceFingerprint = null, AttemptedAt = now.AddMinutes(-3) }
        };

        _attemptRepositoryMock
            .Setup(x => x.GetRecentAttemptsAsync(userId, It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rapidAttempts);

        _attemptRepositoryMock
            .Setup(x => x.GetRecentAttemptsByIpAsync(ipAddress, It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rapidAttempts);

        _attemptRepositoryMock
            .Setup(x => x.GetLastSuccessfulAttemptAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuthenticationAttempt?)null);

        // Act
        var result = await _service.AnalyzeAttemptAsync(userId, ipAddress, userAgent);

        // Assert
        result.RiskScore.Should().BeGreaterOrEqualTo(25);
        result.RiskFactors.Should().Contain(f => f.Contains("Rapid authentication attempts"));
    }

    [Fact]
    public async Task AnalyzeAttemptAsync_WithNewDeviceFingerprint_IncreasesRiskScore()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var ipAddress = "192.168.1.1";
        var userAgent = "Mozilla/5.0";
        var knownFingerprint = "known-fingerprint";
        var newFingerprint = "new-fingerprint";

        var attemptsWithKnownFingerprint = new List<AuthenticationAttempt>
        {
            new() { UserId = userId, IpAddress = ipAddress, UserAgent = userAgent, IsSuccessful = true, DeviceFingerprint = knownFingerprint, AttemptedAt = DateTime.UtcNow.AddDays(-1) }
        };

        _attemptRepositoryMock
            .Setup(x => x.GetRecentAttemptsAsync(userId, It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(attemptsWithKnownFingerprint);

        _attemptRepositoryMock
            .Setup(x => x.GetRecentAttemptsByIpAsync(ipAddress, It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(attemptsWithKnownFingerprint);

        _attemptRepositoryMock
            .Setup(x => x.GetLastSuccessfulAttemptAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuthenticationAttempt?)null);

        // Act
        var result = await _service.AnalyzeAttemptAsync(userId, ipAddress, userAgent, newFingerprint);

        // Assert
        result.RiskScore.Should().BeGreaterOrEqualTo(15);
        result.RiskFactors.Should().Contain(f => f.Contains("New device fingerprint"));
    }

    [Fact]
    public async Task AnalyzeAttemptAsync_WithHighRiskScore_SetsIsAnomalousToTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var ipAddress = "192.168.1.1";
        var userAgent = "Mozilla/5.0";

        var multipleUserAgentAttempts = Enumerable.Range(0, 15)
            .Select(i => new AuthenticationAttempt
            {
                UserId = userId,
                IpAddress = ipAddress,
                UserAgent = $"UserAgent-{i}",
                IsSuccessful = true,
                DeviceFingerprint = null,
                AttemptedAt = DateTime.UtcNow.AddMinutes(-i)
            })
            .ToList();

        _attemptRepositoryMock
            .Setup(x => x.GetRecentAttemptsAsync(userId, It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AuthenticationAttempt>());

        _attemptRepositoryMock
            .Setup(x => x.GetRecentAttemptsByIpAsync(ipAddress, It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(multipleUserAgentAttempts);

        _attemptRepositoryMock
            .Setup(x => x.GetLastSuccessfulAttemptAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuthenticationAttempt?)null);

        // Act
        var result = await _service.AnalyzeAttemptAsync(userId, ipAddress, userAgent);

        // Assert
        result.IsAnomalous.Should().BeTrue();
        result.RiskLevel.Should().BeOneOf(RiskLevel.Medium, RiskLevel.High, RiskLevel.Critical);
    }

    // ── Delegation to IThreatDetectionService ────────────────────────

    [Fact]
    public async Task DetectBruteForceAsync_DelegatesToThreatDetectionService()
    {
        // Arrange
        var identifier = "test@example.com";
        _threatDetectionMock
            .Setup(x => x.DetectBruteForceAsync(identifier, 15))
            .ReturnsAsync(true);

        // Act
        var result = await _service.DetectBruteForceAsync(identifier);

        // Assert
        result.Should().BeTrue();
        _threatDetectionMock.Verify(x => x.DetectBruteForceAsync(identifier, 15), Times.Once);
    }

    [Fact]
    public async Task DetectBruteForceAsync_WithCustomTimeWindow_DelegatesToThreatDetectionService()
    {
        // Arrange
        var identifier = "test@example.com";
        _threatDetectionMock
            .Setup(x => x.DetectBruteForceAsync(identifier, 30))
            .ReturnsAsync(false);

        // Act
        var result = await _service.DetectBruteForceAsync(identifier, 30);

        // Assert
        result.Should().BeFalse();
        _threatDetectionMock.Verify(x => x.DetectBruteForceAsync(identifier, 30), Times.Once);
    }

    [Fact]
    public async Task DetectImpossibleTravelAsync_DelegatesToThreatDetectionService()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var currentLocation = new LocationInfo { Latitude = 40.7128, Longitude = -74.0060 };
        var previousLocation = new LocationInfo { Latitude = 51.5074, Longitude = -0.1278 };
        var timeBetween = TimeSpan.FromMinutes(30);

        _threatDetectionMock
            .Setup(x => x.DetectImpossibleTravelAsync(userId, currentLocation, previousLocation, timeBetween))
            .ReturnsAsync(true);

        // Act
        var result = await _service.DetectImpossibleTravelAsync(userId, currentLocation, previousLocation, timeBetween);

        // Assert
        result.Should().BeTrue();
        _threatDetectionMock.Verify(
            x => x.DetectImpossibleTravelAsync(userId, currentLocation, previousLocation, timeBetween),
            Times.Once);
    }

    [Fact]
    public async Task ShouldThrottleAsync_DelegatesToThreatDetectionService()
    {
        // Arrange
        var ipAddress = "192.168.1.1";
        var email = "test@example.com";
        var expected = new ThrottleDecision { ShouldThrottle = true, DelayMs = 60000 };

        _threatDetectionMock
            .Setup(x => x.ShouldThrottleAsync(ipAddress, email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _service.ShouldThrottleAsync(ipAddress, email);

        // Assert
        result.Should().Be(expected);
        _threatDetectionMock.Verify(
            x => x.ShouldThrottleAsync(ipAddress, email, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void GenerateDeviceFingerprint_DelegatesToThreatDetectionService()
    {
        // Arrange
        var userAgent = "Mozilla/5.0";
        var expected = "fingerprint-hash";

        _threatDetectionMock
            .Setup(x => x.GenerateDeviceFingerprint(userAgent, null, null))
            .Returns(expected);

        // Act
        var result = _service.GenerateDeviceFingerprint(userAgent);

        // Assert
        result.Should().Be(expected);
        _threatDetectionMock.Verify(x => x.GenerateDeviceFingerprint(userAgent, null, null), Times.Once);
    }

    // ── Delegation to IBehavioralAnalysisService ─────────────────────

    [Fact]
    public async Task AnalyzeBehavioralPatternsAsync_DelegatesToBehavioralAnalysisService()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var context = new AuthenticationAttemptContext
        {
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla/5.0"
        };
        var expected = new BehavioralAnalysisResult { RiskScore = 42 };

        _behavioralAnalysisMock
            .Setup(x => x.AnalyzeBehavioralPatternsAsync(userId, context))
            .ReturnsAsync(expected);

        // Act
        var result = await _service.AnalyzeBehavioralPatternsAsync(userId, context);

        // Assert
        result.Should().Be(expected);
        _behavioralAnalysisMock.Verify(x => x.AnalyzeBehavioralPatternsAsync(userId, context), Times.Once);
    }

    // ── Delegation to ILoginAttemptAnalysisService ───────────────────

    [Fact]
    public async Task RecordSuspiciousActivityAsync_DelegatesToLoginAttemptAnalysisService()
    {
        // Arrange
        var activity = new SuspiciousActivity
        {
            UserId = Guid.NewGuid(),
            ActivityType = "BruteForce"
        };

        _loginAttemptAnalysisMock
            .Setup(x => x.RecordSuspiciousActivityAsync(activity))
            .Returns(Task.CompletedTask);

        // Act
        await _service.RecordSuspiciousActivityAsync(activity);

        // Assert
        _loginAttemptAnalysisMock.Verify(x => x.RecordSuspiciousActivityAsync(activity), Times.Once);
    }

    [Fact]
    public async Task AnalyzeLoginAttemptAsync_DelegatesToLoginAttemptAnalysisService()
    {
        // Arrange
        var context = new AuthenticationAttemptContext
        {
            IpAddress = "10.0.0.1",
            UserAgent = "TestAgent"
        };
        var expected = new AuthenticationAnomalyResult
        {
            IsAnomalous = true,
            RiskScore = 75,
            RiskLevel = RiskLevel.High,
            RiskFactors = new List<string> { "Suspicious IP" }
        };

        _loginAttemptAnalysisMock
            .Setup(x => x.AnalyzeLoginAttemptAsync(context))
            .ReturnsAsync(expected);

        // Act
        var result = await _service.AnalyzeLoginAttemptAsync(context);

        // Assert
        result.Should().Be(expected);
        _loginAttemptAnalysisMock.Verify(x => x.AnalyzeLoginAttemptAsync(context), Times.Once);
    }

    [Fact]
    public async Task RecordLoginAttemptAsync_DelegatesToLoginAttemptAnalysisService()
    {
        // Arrange
        var request = new TestCreateAuthenticationAttemptRequest
        {
            UserId = Guid.NewGuid(),
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla/5.0",
            IsSuccessful = true
        };
        var expected = new AuthenticationAttemptAnalysis { IsSuspicious = false };

        _loginAttemptAnalysisMock
            .Setup(x => x.RecordLoginAttemptAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _service.RecordLoginAttemptAsync(request);

        // Assert
        result.Should().Be(expected);
        _loginAttemptAnalysisMock.Verify(
            x => x.RecordLoginAttemptAsync(request, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private class TestCreateAuthenticationAttemptRequest : CreateAuthenticationAttemptRequest
    {
    }
}
