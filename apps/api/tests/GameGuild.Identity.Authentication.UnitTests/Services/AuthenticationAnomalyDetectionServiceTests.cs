using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

public class AuthenticationAnomalyDetectionServiceTests
{
    private readonly Mock<IAuthenticationAttemptRepository> _attemptRepositoryMock;
    private readonly Mock<ILogger<AuthenticationAnomalyDetectionService>> _loggerMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ISiemIntegrationService> _siemServiceMock;
    private readonly AuthenticationAnomalyDetectionService _service;

    public AuthenticationAnomalyDetectionServiceTests()
    {
        _attemptRepositoryMock = new Mock<IAuthenticationAttemptRepository>();
        _loggerMock = new Mock<ILogger<AuthenticationAnomalyDetectionService>>();
        _configurationMock = new Mock<IConfiguration>();
        _siemServiceMock = new Mock<ISiemIntegrationService>();
        
        _service = new AuthenticationAnomalyDetectionService(
            _attemptRepositoryMock.Object,
            _loggerMock.Object,
            _configurationMock.Object,
            _siemServiceMock.Object
        );
    }

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
            new AuthenticationAttempt { UserId = userId, IpAddress = ipAddress, UserAgent = userAgent, IsSuccessful = true, DeviceFingerprint = null, AttemptedAt = now.AddMinutes(-1) },
            new AuthenticationAttempt { UserId = userId, IpAddress = ipAddress, UserAgent = userAgent, IsSuccessful = true, DeviceFingerprint = null, AttemptedAt = now.AddMinutes(-2) },
            new AuthenticationAttempt { UserId = userId, IpAddress = ipAddress, UserAgent = userAgent, IsSuccessful = true, DeviceFingerprint = null, AttemptedAt = now.AddMinutes(-3) }
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
            new AuthenticationAttempt { UserId = userId, IpAddress = ipAddress, UserAgent = userAgent, IsSuccessful = true, DeviceFingerprint = knownFingerprint, AttemptedAt = DateTime.UtcNow.AddDays(-1) }
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
        
        // Create conditions for high risk score
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

    [Fact]
    public async Task DetectBruteForceAsync_WithManyFailedAttempts_ReturnsTrue()
    {
        // Arrange
        var identifier = "test@example.com";
        var failedAttempts = Enumerable.Range(0, 10)
            .Select(i => new AuthenticationAttempt
            {
                UserId = Guid.NewGuid(),
                IpAddress = "192.168.1.1",
                UserAgent = "Mozilla/5.0",
                IsSuccessful = false,
                DeviceFingerprint = null,
                AttemptedAt = DateTime.UtcNow.AddMinutes(-i)
            })
            .ToList();
        
        _attemptRepositoryMock
            .Setup(x => x.GetFailedAttemptsAsync(identifier, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(failedAttempts);

        // Act
        var result = await _service.DetectBruteForceAsync(identifier);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task DetectBruteForceAsync_WithFewFailedAttempts_ReturnsFalse()
    {
        // Arrange
        var identifier = "test@example.com";
        var failedAttempts = Enumerable.Range(0, 2)
            .Select(i => new AuthenticationAttempt
            {
                UserId = Guid.NewGuid(),
                IpAddress = "192.168.1.1",
                UserAgent = "Mozilla/5.0",
                IsSuccessful = false,
                DeviceFingerprint = null,
                AttemptedAt = DateTime.UtcNow.AddMinutes(-i)
            })
            .ToList();
        
        _attemptRepositoryMock
            .Setup(x => x.GetFailedAttemptsAsync(identifier, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(failedAttempts);

        // Act
        var result = await _service.DetectBruteForceAsync(identifier);

        // Assert
        result.Should().BeFalse();
    }
}
