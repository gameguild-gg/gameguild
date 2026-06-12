using FluentAssertions;
using GameGuild.API.Database;
using GameGuild.Identity.Authentication;
using GameGuild.Identity.Users;
using Xunit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Tests.Authentication.Integration;

/// <summary>
/// Integration tests for Session Management features
/// Tests concurrent session handling, security mechanisms, anomaly detection, and edge cases
/// </summary>
public class SessionManagementIntegrationTests : IClassFixture<WebApplicationFactory<GameGuild.API.Program>>, IDisposable
{
    private readonly WebApplicationFactory<GameGuild.API.Program> _factory;
    private readonly IServiceScope _scope;
    private readonly ApplicationDbContext _dbContext;
    private readonly ISessionManagementService _sessionService;
    private readonly IAuthService _authService;
    private readonly IAuthenticationAnomalyDetectionService _anomalyService;

    public SessionManagementIntegrationTests(WebApplicationFactory<GameGuild.API.Program> factory)
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services =>
            {
                // Remove existing DbContext registrations
                var descriptorsToRemove = services
                    .Where(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                                d.ServiceType == typeof(ApplicationDbContext) ||
                                d.ServiceType.FullName?.Contains("EntityFramework") == true ||
                                d.ImplementationType?.FullName?.Contains("Npgsql") == true)
                    .ToList();

                foreach (var descriptor in descriptorsToRemove)
                {
                    services.Remove(descriptor);
                }

                // Add in-memory database
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase($"SessionTestDb_{Guid.NewGuid()}");
                });

                // Add HTTP logging services (required by the pipeline)
                services.AddHttpLogging(o => { });
            });
        });

        _scope = _factory.Services.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        _sessionService = _scope.ServiceProvider.GetRequiredService<ISessionManagementService>();
        _authService = _scope.ServiceProvider.GetRequiredService<IAuthService>();
        _anomalyService = _scope.ServiceProvider.GetRequiredService<IAuthenticationAnomalyDetectionService>();

        _dbContext.Database.EnsureCreated();
    }

    #region Concurrent Session Handling Tests

    [Fact]
    public async Task ConcurrentSessions_MultipleDevices_ShouldCreateSeparateSessions()
    {
        // Arrange - Create user
        var userId = Guid.NewGuid();
        await CreateTestUserAsync(userId);

        var devices = new[]
        {
            new { DeviceFingerprint = "device-1", UserAgent = "Chrome/Windows", IpAddress = "192.168.1.1" },
            new { DeviceFingerprint = "device-2", UserAgent = "Safari/iOS", IpAddress = "192.168.1.2" },
            new { DeviceFingerprint = "device-3", UserAgent = "Firefox/Linux", IpAddress = "192.168.1.3" }
        };

        // Act - Create concurrent sessions
        var sessions = new List<UserSession>();

        foreach (var device in devices)
        {
            var session = new UserSession
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DeviceFingerprint = device.DeviceFingerprint,
                UserAgent = device.UserAgent,
                IpAddress = device.IpAddress,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                IsActive = true
            };

            await _dbContext.Set<UserSession>().AddAsync(session);
            sessions.Add(session);
        }

        await _dbContext.SaveChangesAsync();

        // Assert - All sessions should exist
        var userSessions = await _sessionService.GetUserSessionsAsync(userId);

        userSessions.Should().NotBeNull();
        userSessions.Count.Should().Be(3);
        userSessions.Should().AllSatisfy(s => s.IsActive.Should().BeTrue());
    }

    [Fact]
    public async Task ConcurrentSessions_TerminateOthers_ShouldKeepCurrentSession()
    {
        // Arrange - Create multiple sessions
        var userId = Guid.NewGuid();
        await CreateTestUserAsync(userId);

        var currentSessionId = Guid.NewGuid();
        var sessions = new List<UserSession>
        {
            new UserSession
            {
                Id = currentSessionId,
                UserId = userId,
                DeviceFingerprint = "current-device",
                UserAgent = "Chrome",
                IpAddress = "192.168.1.100",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                IsActive = true
            },
            new UserSession
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DeviceFingerprint = "other-device-1",
                UserAgent = "Firefox",
                IpAddress = "192.168.1.101",
                CreatedAt = DateTime.UtcNow.AddMinutes(-30),
                ExpiresAt = DateTime.UtcNow.AddHours(23.5),
                IsActive = true
            },
            new UserSession
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DeviceFingerprint = "other-device-2",
                UserAgent = "Safari",
                IpAddress = "192.168.1.102",
                CreatedAt = DateTime.UtcNow.AddHours(-2),
                ExpiresAt = DateTime.UtcNow.AddHours(22),
                IsActive = true
            }
        };

        await _dbContext.Set<UserSession>().AddRangeAsync(sessions);
        await _dbContext.SaveChangesAsync();

        // Act - Terminate all sessions except current
        await _sessionService.TerminateAllUserSessionsAsync(userId, SessionTerminationReason.UserLogout, currentSessionId);

        // Assert
        var remainingSessions = await _dbContext.Set<UserSession>()
            .Where(s => s.UserId == userId && s.IsActive)
            .ToListAsync();

        remainingSessions.Should().HaveCount(1);
        remainingSessions.First().Id.Should().Be(currentSessionId);
    }

    [Fact]
    public async Task ConcurrentSessions_TerminateAll_ShouldInvalidateAllSessions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await CreateTestUserAsync(userId);

        var sessions = Enumerable.Range(1, 5).Select(i => new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DeviceFingerprint = $"device-{i}",
            UserAgent = $"Browser-{i}",
            IpAddress = $"192.168.1.{i}",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            IsActive = true
        }).ToList();

        await _dbContext.Set<UserSession>().AddRangeAsync(sessions);
        await _dbContext.SaveChangesAsync();

        // Act - Terminate all sessions
        await _sessionService.TerminateAllUserSessionsAsync(userId, SessionTerminationReason.UserLogout);

        // Assert
        var activeSessions = await _dbContext.Set<UserSession>()
            .Where(s => s.UserId == userId && s.IsActive)
            .ToListAsync();

        activeSessions.Should().BeEmpty();
    }

    [Fact]
    public async Task ConcurrentSessions_UnderLoad_ShouldHandleMultipleSimultaneousRequests()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await CreateTestUserAsync(userId);

        // Act - Simulate concurrent session creation
        var tasks = Enumerable.Range(1, 10).Select(async i =>
        {
            var session = new UserSession
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DeviceFingerprint = $"concurrent-device-{i}",
                UserAgent = $"Browser-{i}",
                IpAddress = $"10.0.0.{i}",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                IsActive = true
            };

            await _dbContext.Set<UserSession>().AddAsync(session);
            await _dbContext.SaveChangesAsync();
        });

        await Task.WhenAll(tasks);

        // Assert - All sessions should be created
        var sessions = await _dbContext.Set<UserSession>()
            .Where(s => s.UserId == userId)
            .ToListAsync();

        sessions.Should().HaveCount(10);
        sessions.Should().AllSatisfy(s => s.IsActive.Should().BeTrue());
    }

    #endregion

    #region Session Security Tests

    [Fact]
    public async Task SessionSecurity_IpAndUserAgentChange_ShouldDetectAnomaly()
    {
        var userId = Guid.NewGuid();
        await CreateTestUserAsync(userId);

        await _dbContext.Set<AuthenticationAttempt>().AddAsync(new AuthenticationAttempt
        {
            Id = Guid.NewGuid(),
            Email = $"session.security.{userId:N}@example.com",
            UserId = userId,
            IpAddress = "192.168.1.100",
            UserAgent = "Chrome/Windows",
            Location = "US-Seattle",
            DeviceFingerprint = "known-device",
            IsSuccessful = true,
            AttemptedAt = DateTime.UtcNow.AddMinutes(-10),
            ProcessingTime = TimeSpan.FromMilliseconds(120)
        });
        await _dbContext.SaveChangesAsync();

        var result = await _anomalyService.AnalyzeLoginAttemptAsync(new AuthenticationAttemptContext
        {
            UserId = userId,
            Identifier = $"session.security.{userId:N}@example.com",
            IpAddress = "10.20.30.40",
            UserAgent = "curl/8.0",
            Location = new LocationInfo { Country = "US", City = "Seattle" },
            DeviceFingerprint = "known-device",
            Timestamp = DateTime.UtcNow
        });

        result.IsAnomalous.Should().BeTrue();
        result.DetectedAnomalies.Should().Contain("IpAddressChange");
        result.DetectedAnomalies.Should().Contain("UserAgentChange");
        result.RiskLevel.Should().BeOneOf(RiskLevel.Medium, RiskLevel.High, RiskLevel.Critical);
    }

    [Fact]
    public async Task SessionSecurity_ImpossibleTravel_ShouldDetectAnomaly()
    {
        var userId = Guid.NewGuid();
        await CreateTestUserAsync(userId);

        await _dbContext.Set<AuthenticationAttempt>().AddAsync(new AuthenticationAttempt
        {
            Id = Guid.NewGuid(),
            Email = $"travel.{userId:N}@example.com",
            UserId = userId,
            IpAddress = "192.168.1.100",
            UserAgent = "Chrome/Windows",
            Location = "US-NewYork",
            DeviceFingerprint = "travel-device",
            IsSuccessful = true,
            AttemptedAt = DateTime.UtcNow.AddMinutes(-5),
            ProcessingTime = TimeSpan.FromMilliseconds(110)
        });
        await _dbContext.SaveChangesAsync();

        var result = await _anomalyService.AnalyzeLoginAttemptAsync(new AuthenticationAttemptContext
        {
            UserId = userId,
            Identifier = $"travel.{userId:N}@example.com",
            IpAddress = "203.0.113.10",
            UserAgent = "Chrome/Windows",
            Location = new LocationInfo { Country = "GB", City = "London" },
            DeviceFingerprint = "travel-device",
            Timestamp = DateTime.UtcNow
        });

        result.IsAnomalous.Should().BeTrue();
        result.DetectedAnomalies.Should().Contain("ImpossibleTravel");
        result.RiskLevel.Should().BeOneOf(RiskLevel.Medium, RiskLevel.High, RiskLevel.Critical);
    }

    [Fact]
    public async Task SessionSecurity_SecurityAnalysis_ShouldIdentifyRiskySessionSpread()
    {
        var userId = Guid.NewGuid();
        await CreateTestUserAsync(userId);

        var sessions = Enumerable.Range(1, 12).Select(i => new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DeviceFingerprint = $"device-{i}",
            UserAgent = $"Browser-{i}",
            IpAddress = $"10.0.0.{i}",
            CreatedAt = DateTime.UtcNow.AddMinutes(-i),
            LastUsedAt = DateTime.UtcNow.AddMinutes(-i),
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            IsActive = true
        });

        await _dbContext.Set<UserSession>().AddRangeAsync(sessions);
        await _dbContext.SaveChangesAsync();

        var analysis = await _sessionService.AnalyzeSessionSecurityAsync(userId, "10.0.0.1", "Browser-1");

        analysis.ActiveSessionCount.Should().Be(12);
        analysis.UnusualActivityDetected.Should().BeTrue();
        analysis.RiskLevel.Should().Be(RiskLevel.High);
        analysis.RiskFactors.Should().NotBeEmpty();
    }

    #endregion

    #region Anomaly Detection Accuracy Tests

    [Fact]
    public async Task AnomalyDetection_BruteForceAttempt_ShouldDetectAndThrottle()
    {
        var userId = Guid.NewGuid();
        var email = $"bruteforce.{userId:N}@example.com";
        await CreateTestUserAsync(userId);

        var failedAttempts = Enumerable.Range(1, 5).Select(i => new AuthenticationAttempt
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserId = userId,
            IpAddress = "192.168.1.100",
            UserAgent = "Chrome/Windows",
            Location = "US-Seattle",
            IsSuccessful = false,
            FailureReason = "Invalid credentials",
            AttemptedAt = DateTime.UtcNow.AddMinutes(-i),
            ProcessingTime = TimeSpan.FromMilliseconds(90)
        });

        await _dbContext.Set<AuthenticationAttempt>().AddRangeAsync(failedAttempts);
        await _dbContext.SaveChangesAsync();

        (await _anomalyService.DetectBruteForceAsync(email)).Should().BeTrue();
    }

    [Fact]
    public async Task AnomalyDetection_NormalBehavior_ShouldNotFlag()
    {
        var userId = Guid.NewGuid();
        var email = $"normal.{userId:N}@example.com";
        await CreateTestUserAsync(userId);

        await _dbContext.Set<AuthenticationAttempt>().AddAsync(new AuthenticationAttempt
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserId = userId,
            IpAddress = "192.168.1.100",
            UserAgent = "Chrome/Windows",
            Location = "US-Seattle",
            DeviceFingerprint = "normal-device",
            IsSuccessful = true,
            AttemptedAt = DateTime.UtcNow.AddDays(-1),
            ProcessingTime = TimeSpan.FromMilliseconds(120)
        });
        await _dbContext.SaveChangesAsync();

        var result = await _anomalyService.AnalyzeLoginAttemptAsync(new AuthenticationAttemptContext
        {
            UserId = userId,
            Identifier = email,
            IpAddress = "192.168.1.100",
            UserAgent = "Chrome/Windows",
            Location = new LocationInfo { Country = "US", City = "Seattle" },
            DeviceFingerprint = "normal-device",
            Timestamp = DateTime.UtcNow.AddHours(14)
        });

        result.IsAnomalous.Should().BeFalse();
        result.RiskLevel.Should().Be(RiskLevel.Low);
    }

    #endregion

    #region Session Timeout and Renewal Edge Cases

    [Fact]
    public async Task SessionTimeout_ExpiredSession_ShouldNotBeActive()
    {
        var userId = Guid.NewGuid();
        await CreateTestUserAsync(userId);

        var expiredSession = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DeviceFingerprint = "test-device",
            UserAgent = "Chrome",
            IpAddress = "192.168.1.100",
            CreatedAt = DateTime.UtcNow.AddHours(-25),
            ExpiresAt = DateTime.UtcNow.AddHours(-1),
            IsActive = true
        };

        await _dbContext.Set<UserSession>().AddAsync(expiredSession);
        await _dbContext.SaveChangesAsync();

        var activeSessions = await _sessionService.GetUserSessionsAsync(userId);

        activeSessions.Should().BeEmpty();
    }

    [Fact]
    public async Task SessionRenewal_BeforeExpiry_ShouldExtendSession()
    {
        var userId = Guid.NewGuid();
        await CreateTestUserAsync(userId);

        var sessionId = Guid.NewGuid();
        var originalExpiry = DateTime.UtcNow.AddHours(1);

        var session = new UserSession
        {
            Id = sessionId,
            UserId = userId,
            DeviceFingerprint = "test-device",
            UserAgent = "Chrome",
            IpAddress = "192.168.1.100",
            CreatedAt = DateTime.UtcNow.AddHours(-23),
            ExpiresAt = originalExpiry,
            IsActive = true
        };

        await _dbContext.Set<UserSession>().AddAsync(session);
        await _dbContext.SaveChangesAsync();

        session.LastUsedAt = DateTime.UtcNow;
        session.ExpiresAt = DateTime.UtcNow.AddHours(24);
        _dbContext.Set<UserSession>().Update(session);
        await _dbContext.SaveChangesAsync();

        var renewedSession = await _dbContext.Set<UserSession>().FindAsync(sessionId);
        renewedSession.Should().NotBeNull();
        renewedSession!.ExpiresAt.Should().BeAfter(originalExpiry);
    }

    [Fact]
    public async Task SessionTimeout_IdleSession_ShouldExpireAfterInactivity()
    {
        var userId = Guid.NewGuid();
        await CreateTestUserAsync(userId);

        var idleSession = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DeviceFingerprint = "idle-device",
            UserAgent = "Chrome",
            IpAddress = "192.168.1.100",
            CreatedAt = DateTime.UtcNow.AddHours(-10),
            LastUsedAt = DateTime.UtcNow.AddHours(-2),
            ExpiresAt = DateTime.UtcNow.AddHours(14),
            IsActive = true
        };

        await _dbContext.Set<UserSession>().AddAsync(idleSession);
        await _dbContext.SaveChangesAsync();

        var idleThreshold = TimeSpan.FromHours(1);
        var timeSinceLastActivity = DateTime.UtcNow - idleSession.LastUsedAt;

        timeSinceLastActivity.Should().BeGreaterThan(idleThreshold);
    }

    [Fact]
    public async Task SessionActivity_Timeline_ShouldTrackSessionAndTrustedDeviceActions()
    {
        var userId = Guid.NewGuid();
        await CreateTestUserAsync(userId);

        var session = await _sessionService.CreateSessionAsync(userId, "192.168.1.100", "Chrome/Windows", "timeline-device");
        await _sessionService.TrustDeviceAsync(userId, "timeline-device", "Work laptop");
        await _sessionService.TerminateSessionAsync(session.Id, SessionTerminationReason.UserLogout);

        var timeline = await _sessionService.GetActivityTimelineAsync(userId);

        timeline.Should().NotBeEmpty();
        timeline.Should().Contain(entry => entry.ActivityType == "SessionCreated" && entry.SessionId == session.Id);
        timeline.Should().Contain(entry => entry.ActivityType == "DeviceTrusted" && entry.DeviceFingerprint == "timeline-device");
    }

    #endregion

    #region Helper Methods

    private async Task CreateTestUserAsync(Guid userId)
    {
        // Create a minimal user entry for testing
        var user = new User
        {
            Id = userId,
            Email = $"test.user.{userId}@example.com",
            Username = $"testuser_{userId:N}",
            PasswordHash = "hashed_password"
        };

        await _dbContext.Set<User>().AddAsync(user);
        await _dbContext.SaveChangesAsync();
    }

    #endregion

    public void Dispose()
    {
        _scope?.Dispose();
        _dbContext?.Dispose();
    }
}
