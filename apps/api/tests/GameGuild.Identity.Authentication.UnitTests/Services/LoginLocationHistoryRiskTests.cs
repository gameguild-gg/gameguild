using GameGuild.Identity.Authentication;
using GameGuild.Identity.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

public sealed class LoginLocationHistoryRiskTests
{
    [Theory]
    [InlineData("structured", "United States", "US", "New York", false)]
    [InlineData("structured", "United States", null, "New York", false)]
    [InlineData("structured", null, "US", "New York", false)]
    [InlineData("structured", "Brazil", "BR", "Sao Paulo", true)]
    [InlineData("legacy", "US", null, "Winston-Salem", false)]
    [InlineData("unknown", "Brazil", "BR", "Sao Paulo", false)]
    [InlineData("country-only", "Brazil", "BR", "Sao Paulo", true)]
    public async Task LoginAnalysisUsesComparableActualOrLegacyHistory(string format, string? country, string? code, string city, bool suspicious)
    {
        var user = Guid.NewGuid();
        var now = new DateTime(2026, 10, 19, 12, 0, 0, DateTimeKind.Utc);
        var previous = new AuthenticationAttempt
        {
            UserId = user, Email = "synthetic-login-risk@example.test", IsSuccessful = true,
            IpAddress = "198.51.100.72", UserAgent = "Synthetic login risk", AttemptedAt = now.AddMinutes(-10)
        };
        (previous.Country, previous.City, previous.Location) = format switch
        {
            "structured" => ("US", "New York", "New York, United States"),
            "legacy" => (null, null, "US-Winston-Salem"),
            "country-only" => ("US", null, "United States"),
            "unknown" => (null, null, "New York, United States"),
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };
        var repository = new Mock<IAuthenticationAttemptRepository>(MockBehavior.Strict);
        repository.Setup(port => port.GetRecentAttemptsAsync(user, It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AuthenticationAttempt> { previous });
        repository.Setup(port => port.GetLastSuccessfulAttemptAsync(user, It.IsAny<CancellationToken>())).ReturnsAsync(previous);
        var siem = Siem();
        var configuration = new ConfigurationBuilder().Build();
        var threat = new ThreatDetectionService(repository.Object, NullLogger<ThreatDetectionService>.Instance, configuration, siem.Object);
        var service = new LoginAttemptAnalysisService(repository.Object, threat, NullLogger<LoginAttemptAnalysisService>.Instance, configuration, siem.Object);
        var result = await service.AnalyzeLoginAttemptAsync(new AuthenticationAttemptContext
        {
            UserId = user, IpAddress = previous.IpAddress, UserAgent = previous.UserAgent!, AttemptedAt = now,
            Location = new LocationInfo { Country = country, CountryCode = code, City = city }
        });
        Assert.Equal(suspicious ? 50 : 0, result.RiskScore);
        Assert.Equal(suspicious, result.DetectedAnomalies.Contains(SecurityAlertKinds.ImpossibleTravel));
        siem.Verify(port => port.SendImpossibleTravelEventAsync(user, It.IsAny<LocationInfo>(), It.IsAny<LocationInfo>(),
            TimeSpan.FromMinutes(10), CancellationToken.None), suspicious ? Times.Once() : Times.Never());
    }

    [Theory]
    [InlineData("United States", "US", "US", null, false)]
    [InlineData("Brazil", "BR", "United States", "US", true)]
    [InlineData(null, "BR", null, "US", true)]
    [InlineData("United States", null, "US", null, false)]
    public async Task DirectThreatAnalysisComparesCompatibleCountryRepresentations(string? currentCountry, string? currentCode,
        string? previousCountry, string? previousCode, bool suspicious)
    {
        var siem = Siem();
        var service = new ThreatDetectionService(Mock.Of<IAuthenticationAttemptRepository>(), NullLogger<ThreatDetectionService>.Instance,
            new ConfigurationBuilder().Build(), siem.Object);
        var result = await service.DetectImpossibleTravelAsync(Guid.NewGuid(),
            new LocationInfo { Country = currentCountry, CountryCode = currentCode, City = "Current city" },
            new LocationInfo { Country = previousCountry, CountryCode = previousCode, City = "Previous city" }, TimeSpan.FromMinutes(10));
        Assert.Equal(suspicious, result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task NonpositiveElapsedTimeDoesNotInventTravel(int minutes)
    {
        var siem = Siem();
        var service = new ThreatDetectionService(Mock.Of<IAuthenticationAttemptRepository>(), NullLogger<ThreatDetectionService>.Instance,
            new ConfigurationBuilder().Build(), siem.Object);
        Assert.False(await service.DetectImpossibleTravelAsync(Guid.NewGuid(), new LocationInfo { Country = "Brazil", CountryCode = "BR" },
            new LocationInfo { Country = "United States", CountryCode = "US" }, TimeSpan.FromMinutes(minutes)));
        siem.VerifyNoOtherCalls();
    }

    // ── Issue #1115: user-declared travel windows suppress the GeoVelocity alert ──

    [Fact]
    public async Task DeclaredTravelWindowCoveringSignInSuppressesImpossibleTravel()
    {
        var now = new DateTime(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc);
        using var clock = FakeClock.At(now);
        var user = Guid.NewGuid();
        var siem = Siem();
        var preferences = PreferencesWithTravelWindows($$"""[{"start":"2026-10-31","end":"2026-11-05"}]""");
        var service = CreateService(siem.Object, preferences);

        var suspicious = await service.DetectImpossibleTravelAsync(user,
            new LocationInfo { Country = "United States", CountryCode = "US" },
            new LocationInfo { Country = "Brazil", CountryCode = "BR" }, TimeSpan.FromMinutes(30));

        Assert.False(suspicious);
        siem.Verify(port => port.SendImpossibleTravelEventAsync(It.IsAny<Guid>(), It.IsAny<LocationInfo>(), It.IsAny<LocationInfo>(),
            It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task DeclaredTravelWindowOutsideSignInStillAlerts()
    {
        var now = new DateTime(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc);
        using var clock = FakeClock.At(now);
        var user = Guid.NewGuid();
        var siem = Siem();
        // Window ended two weeks before the sign-in.
        var preferences = PreferencesWithTravelWindows($$"""[{"start":"2026-10-01","end":"2026-10-15"}]""");
        var service = CreateService(siem.Object, preferences);

        var suspicious = await service.DetectImpossibleTravelAsync(user,
            new LocationInfo { Country = "United States", CountryCode = "US" },
            new LocationInfo { Country = "Brazil", CountryCode = "BR" }, TimeSpan.FromMinutes(30));

        Assert.True(suspicious);
        siem.Verify(port => port.SendImpossibleTravelEventAsync(user, It.IsAny<LocationInfo>(), It.IsAny<LocationInfo>(),
            It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task OverlongDeclaredWindowIsIgnoredAndStillAlerts()
    {
        var now = new DateTime(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc);
        using var clock = FakeClock.At(now);
        var user = Guid.NewGuid();
        var siem = Siem();
        // 60-day window exceeds the 31-day cap and must not suppress the alert.
        var preferences = PreferencesWithTravelWindows($$"""[{"start":"2026-10-01","end":"2026-11-30"}]""");
        var service = CreateService(siem.Object, preferences);

        var suspicious = await service.DetectImpossibleTravelAsync(user,
            new LocationInfo { Country = "United States", CountryCode = "US" },
            new LocationInfo { Country = "Brazil", CountryCode = "BR" }, TimeSpan.FromMinutes(30));

        Assert.True(suspicious);
    }

    [Theory]
    [InlineData("[{\"start\":\"garbage\"}]")]
    [InlineData("[]")]
    [InlineData("[123]")]
    [InlineData("{\"start\":\"2026-01-01\",\"end\":\"2026-01-02\"}")]
    public async Task MalformedDeclaredWindowsFailClosedAndStillAlert(string preferenceJson)
    {
        var now = new DateTime(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc);
        using var clock = FakeClock.At(now);
        var user = Guid.NewGuid();
        var siem = Siem();
        var preferences = PreferencesWithTravelWindows(preferenceJson);
        var service = CreateService(siem.Object, preferences);

        var suspicious = await service.DetectImpossibleTravelAsync(user,
            new LocationInfo { Country = "United States", CountryCode = "US" },
            new LocationInfo { Country = "Brazil", CountryCode = "BR" }, TimeSpan.FromMinutes(30));

        Assert.True(suspicious);
    }

    [Fact]
    public async Task PreferencesStoreFailureFailsClosedAndStillAlerts()
    {
        var user = Guid.NewGuid();
        var siem = Siem();
        var preferencesRepository = new Mock<IUserPreferencesRepository>(MockBehavior.Strict);
        preferencesRepository
            .Setup(port => port.GetByUserIdAsync(user, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("store unavailable"));
        var service = new ThreatDetectionService(Mock.Of<IAuthenticationAttemptRepository>(), NullLogger<ThreatDetectionService>.Instance,
            new ConfigurationBuilder().Build(), siem.Object, preferencesRepository.Object);

        var suspicious = await service.DetectImpossibleTravelAsync(user,
            new LocationInfo { Country = "United States", CountryCode = "US" },
            new LocationInfo { Country = "Brazil", CountryCode = "BR" }, TimeSpan.FromMinutes(30));

        Assert.True(suspicious);
    }

    [Fact]
    public async Task NoPreferencesRepositoryKeepsLegacyAlertingBehavior()
    {
        var siem = Siem();
        var service = new ThreatDetectionService(Mock.Of<IAuthenticationAttemptRepository>(), NullLogger<ThreatDetectionService>.Instance,
            new ConfigurationBuilder().Build(), siem.Object);

        var suspicious = await service.DetectImpossibleTravelAsync(Guid.NewGuid(),
            new LocationInfo { Country = "United States", CountryCode = "US" },
            new LocationInfo { Country = "Brazil", CountryCode = "BR" }, TimeSpan.FromMinutes(30));

        Assert.True(suspicious);
    }

    private static ThreatDetectionService CreateService(ISiemIntegrationService siem, UserPreferences? preferences)
    {
        var preferencesRepository = new Mock<IUserPreferencesRepository>();
        preferencesRepository
            .Setup(port => port.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(preferences);
        return new ThreatDetectionService(Mock.Of<IAuthenticationAttemptRepository>(), NullLogger<ThreatDetectionService>.Instance,
            new ConfigurationBuilder().Build(), siem, preferencesRepository.Object);
    }

    private static UserPreferences? PreferencesWithTravelWindows(string windowsJson)
    {
        if (string.IsNullOrEmpty(windowsJson))
        {
            return null;
        }

        var preferences = UserPreferences.Create(Guid.NewGuid());
        preferences.SetGeneralPreferences(new Dictionary<string, object?>
        {
            [DeclaredTravelWindow.PreferencesKey] = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(windowsJson)
        });
        return preferences;
    }

    private sealed class FakeClock : IDisposable
    {
        public FakeClock(DateTime instant)
        {
            SystemClock.SetProvider(new FixedTimeProvider(instant));
        }

        public void Dispose() => SystemClock.Reset();
    }

    private sealed class FixedTimeProvider(DateTime instant) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new DateTimeOffset(instant, TimeSpan.Zero);
    }

    private static Mock<ISiemIntegrationService> Siem()
    {
        var siem = new Mock<ISiemIntegrationService>(MockBehavior.Strict);
        siem.Setup(port => port.SendImpossibleTravelEventAsync(It.IsAny<Guid>(), It.IsAny<LocationInfo>(), It.IsAny<LocationInfo>(),
            It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        return siem;
    }
}
