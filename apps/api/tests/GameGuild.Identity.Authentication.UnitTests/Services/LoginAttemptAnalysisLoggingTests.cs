using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GameGuild.Identity.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

public sealed class LoginAttemptAnalysisLoggingTests
{
    private const string Email = "Diagnostic.User@example.test";
    private static readonly string IpAddress = IPAddress.Loopback.ToString();
    private readonly Mock<IAuthenticationAttemptRepository> _repository = new();
    private readonly Mock<ISiemIntegrationService> _siem = new();
    private readonly RecordingLogger _logger = new();

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public async Task RecordingAttempt_PreservesStoredDataWithoutExposingItInAnyLog(int failedAttempts)
    {
        _repository.Setup(repository => repository.GetFailedAttemptsAsync(
                It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Range(0, failedAttempts)
                .Select(_ => new AuthenticationAttempt { Email = Email, IpAddress = IpAddress }).ToList());
        _repository.Setup(repository => repository.CreateAsync(
                It.IsAny<AuthenticationAttempt>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuthenticationAttempt attempt, CancellationToken _) => attempt);

        var result = await CreateService().RecordLoginAttemptAsync(CreateRequest());

        result.IsSuspicious.Should().Be(failedAttempts > 0);
        _repository.Verify(repository => repository.CreateAsync(
            It.Is<AuthenticationAttempt>(attempt => attempt.Email == Email.ToLowerInvariant()
                && attempt.IpAddress == IpAddress), It.IsAny<CancellationToken>()), Times.Once);
        _logger.Entries.Should().Contain(entry => entry.Level == LogLevel.Information);
        _logger.Entries.Any(entry => entry.Message.StartsWith("Suspicious login attempt", StringComparison.Ordinal))
            .Should().Be(failedAttempts > 0);
        AssertOpaqueLogs();
        _logger.Entries.Should().Contain(entry => entry.Message.Contains("email:", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ProviderFailure_DoesNotLogItsSensitiveMessageOrDataAndRethrowsOriginalException()
    {
        const string privateMarker = "private-diagnostic-marker";
        var exception = new InvalidOperationException($"{Email} {IpAddress}\r\n{privateMarker}");
        exception.Data["private-value"] = privateMarker;
        _repository.Setup(repository => repository.GetFailedAttemptsAsync(
                It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        var service = CreateService();
        var failure = async () => await service.RecordLoginAttemptAsync(CreateRequest());

        (await failure.Should().ThrowAsync<InvalidOperationException>()).Which.Should().BeSameAs(exception);
        AssertOpaqueLogs();
        _logger.Entries.Should().Contain(entry => entry.Level == LogLevel.Error
            && entry.Message.Contains(nameof(InvalidOperationException), StringComparison.Ordinal));
        _logger.Entries.Should().OnlyContain(entry => !entry.Message.Contains(privateMarker, StringComparison.Ordinal));
    }

    [Fact]
    public async Task SuspiciousActivity_ProtectsDiagnosticIdentifiersWithoutChangingTheSiemEvent()
    {
        var userId = Guid.NewGuid();
        var activity = new SuspiciousActivity
        {
            ActivityType = "BruteForce\r\nFORGED",
            Identifier = Email,
            UserId = userId,
            RiskLevel = RiskLevel.High
        };

        await CreateService().RecordSuspiciousActivityAsync(activity);

        _siem.Verify(siem => siem.SendSuspiciousActivityEventAsync(
            activity, It.IsAny<CancellationToken>()), Times.Once);
        AssertOpaqueLogs();
        _logger.Entries.Should().OnlyContain(entry => !entry.Message.Contains(userId.ToString(), StringComparison.Ordinal));
        _logger.Entries.Should().OnlyContain(entry => !entry.Message.Contains('\r') && !entry.Message.Contains('\n'));
    }

    [Fact]
    public async Task AdaptiveAnalysis_ProtectsExtensionLabelsWithoutChangingTheAssessment()
    {
        const string privateLabel = "private-adaptive-detail\r\nFORGED EVENT";
        var adaptive = new Mock<IAdaptiveAnomalyDetectionService>();
        adaptive.Setup(service => service.AssessAsync(
                It.IsAny<AuthenticationAttemptContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AdaptiveAnomalyAssessment
            {
                Origin = AdaptiveAnomalyOrigin.LearnedBaseline,
                IsLearnedDeviation = true,
                LearnedRiskScoreContribution = 40,
                DeviationLabels = ["Learned:HourOfDayDeviation", privateLabel]
            });
        var context = new AuthenticationAttemptContext
        {
            IpAddress = IpAddress,
            AttemptedAt = new DateTime(2026, 10, 21, 14, 0, 0, DateTimeKind.Utc)
        };

        var result = await CreateService(adaptive.Object).AnalyzeLoginAttemptAsync(context);

        result.RiskScore.Should().Be(40);
        result.IsAnomalous.Should().BeTrue();
        result.DetectedAnomalies.Should().Contain(privateLabel);
        AssertOpaqueLogs();
        _logger.Entries.Should().Contain(entry => entry.Message.Contains("Learned:HourOfDayDeviation", StringComparison.Ordinal));
        _logger.Entries.Should().OnlyContain(entry => !entry.Message.Contains(privateLabel, StringComparison.Ordinal)
            && !entry.Message.Contains('\r') && !entry.Message.Contains('\n'));
    }

    private LoginAttemptAnalysisService CreateService(IAdaptiveAnomalyDetectionService? adaptive = null)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:Anomaly:SuspiciousThreshold"] = "2"
        }).Build();
        return new LoginAttemptAnalysisService(_repository.Object, Mock.Of<IThreatDetectionService>(),
            _logger, configuration, _siem.Object, null, null, adaptive);
    }

    private static TestCreateAuthenticationAttemptRequest CreateRequest() => new()
    {
        Email = Email,
        IpAddress = IpAddress,
        IsSuccessful = false,
        ProcessingTime = TimeSpan.FromMilliseconds(100),
        UserAgent = "privacy-regression-agent"
    };

    private void AssertOpaqueLogs()
    {
        _logger.Entries.Should().NotBeEmpty();
        _logger.Entries.Should().OnlyContain(entry => entry.Exception == null);
        var diagnostics = string.Join("\n", _logger.Entries.Select(entry =>
            entry.Message + " " + string.Join(" ", entry.StateValues)));
        diagnostics.Should().NotContain(Email).And.NotContain(Email.ToLowerInvariant()).And.NotContain(IpAddress);
    }

    private sealed record LogEntry(LogLevel Level, string Message, Exception? Exception, object?[] StateValues);

    private sealed class RecordingLogger : ILogger<LoginAttemptAnalysisService>
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var values = state is IEnumerable<KeyValuePair<string, object?>> properties
                ? properties.Select(property => property.Value).ToArray()
                : [];
            Entries.Add(new LogEntry(logLevel, formatter(state, exception), exception, values));
        }
    }
}
