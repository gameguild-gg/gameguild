using System.Net;
using GameGuild.CQRS;
using GameGuild.Identity.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

public sealed class OAuthProviderOutageAbuseRegressionTests
{
    [Theory]
    [InlineData("provider-503", true)]
    [InlineData("transport", true)]
    [InlineData("timeout", true)]
    [InlineData("httpclient-timeout", true)]
    [InlineData("typed-outage", true)]
    [InlineData("provider-401", false)]
    [InlineData("invalid-credential", false)]
    [InlineData("unknown-error", false)]
    public async Task ProviderFailures_KeepAuditHistory_WithoutChargingOutagesToAbuseCounters(string scenario, bool outage)
    {
        await using var fixture = new Fixture();
        var failure = Failure(scenario);
        fixture.Provider.Setup(provider => provider.GetUserProfileAsync("github", It.IsAny<string>())).ThrowsAsync(failure);

        for (var attempt = 0; attempt < 10; attempt++)
        {
            var error = await Record.ExceptionAsync(() => fixture.Service.GitHubSignInAsync(Request()));
            if (outage)
            {
                var unavailable = Assert.IsType<OAuthProviderUnavailableException>(error);
                Assert.Same(failure, scenario == "typed-outage" ? unavailable : unavailable.InnerException);
            }
            else
            {
                Assert.Same(failure, error);
            }
        }

        var history = await fixture.Repository.GetByIpAddressAsync(Fixture.IpAddress);
        Assert.Equal(10, history.Count);
        Assert.All(history, attempt =>
        {
            Assert.False(attempt.IsSuccessful);
            Assert.Equal("oauth-github@audit.invalid", attempt.Email);
            Assert.Equal(outage ? nameof(OAuthProviderUnavailableException) : failure.GetType().Name, attempt.FailureReason);
        });
        Assert.Equal(10, fixture.AuditEvents.Count(value => value.ActionType == "Authentication.Failed" && !value.Success));
        Assert.Equal(10, fixture.AuditEvents.Count(value => value.ActionType == "Authentication.ExternalLoginFailed" && !value.Success));
        var diagnosticType = failure is OAuthProviderUnavailableException { InnerException: { } providerFailure }
            ? providerFailure.GetType().Name
            : failure.GetType().Name;
        Assert.All(fixture.AuditEvents.Where(value => value.ActionType == "Authentication.ExternalLoginFailed"), audit =>
        {
            Assert.Equal(outage ? nameof(OAuthProviderUnavailableException) : failure.GetType().Name, audit.ErrorMessage);
            Assert.Equal(diagnosticType, audit.Metadata?.GetType().GetProperty("FailureType")?.GetValue(audit.Metadata) as string);
        });
        Assert.Equal(10, (await fixture.Repository.GetByEmailAsync("oauth-github@audit.invalid")).Count);
        Assert.Empty(fixture.Users.Invocations);
        Assert.Empty(fixture.Tokens.Invocations);

        var throttle = await fixture.Enumeration.ShouldThrottleAsync(Fixture.IpAddress);
        Assert.Equal(!outage, throttle.ShouldThrottle);
        Assert.Equal(outage ? 0 : 10, throttle.AttemptCount);
        Assert.Equal(!outage, await fixture.Threats.DetectBruteForceAsync(Fixture.IpAddress));
        Assert.Equal(outage ? 0 : 10, await fixture.Repository.CountRecentFailedAttemptsAsync(
            "oauth-github@audit.invalid", SystemClock.UtcNow.AddHours(-1)));
    }

    [Fact]
    public async Task CallerCancellation_IsNotRecordedAsProviderFailure()
    {
        await using var fixture = new Fixture();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var failure = new OperationCanceledException(cancellation.Token);
        fixture.Provider.Setup(provider => provider.GetUserProfileAsync("github", It.IsAny<string>())).ThrowsAsync(failure);

        var error = await Record.ExceptionAsync(() => fixture.Service.GitHubSignInAsync(Request(), cancellation.Token));

        Assert.Same(failure, error);
        Assert.Empty(await fixture.Repository.GetByIpAddressAsync(Fixture.IpAddress));
        Assert.Empty(fixture.AuditEvents);
        Assert.False((await fixture.Enumeration.ShouldThrottleAsync(Fixture.IpAddress)).ShouldThrottle);
        Assert.Empty(fixture.Users.Invocations);
        Assert.Empty(fixture.Tokens.Invocations);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(nameof(HttpRequestException))]
    public async Task UnclassifiedHistoricalFailures_RemainAbuseEvidence(string? failureReason)
    {
        await using var fixture = new Fixture();
        for (var attempt = 0; attempt < 5; attempt++)
        {
            await fixture.Repository.CreateAsync(new AuthenticationAttempt
            {
                Email = "oauth-github@audit.invalid", IpAddress = Fixture.IpAddress,
                IsSuccessful = false, FailureReason = failureReason, AttemptedAt = SystemClock.UtcNow
            });
        }

        Assert.True(await fixture.Threats.DetectBruteForceAsync(Fixture.IpAddress));
        Assert.Equal(5, await fixture.Repository.CountRecentFailedAttemptsAsync(
            "oauth-github@audit.invalid", SystemClock.UtcNow.AddHours(-1)));
        Assert.Equal(5, (await fixture.Repository.GetByIpAddressAsync(Fixture.IpAddress)).Count);
    }

    private static OAuthSignInRequest Request() => new() { AccessToken = "fixture-credential" };

    private static Exception Failure(string scenario) => scenario switch
    {
        "provider-503" => new HttpRequestException("Provider unavailable", null, HttpStatusCode.ServiceUnavailable),
        "transport" => new HttpRequestException("Connection reset"),
        "timeout" => new TimeoutException("Provider timeout"),
        "httpclient-timeout" => new TaskCanceledException("Timeout", new TimeoutException("Provider timeout")),
        "typed-outage" => new OAuthProviderUnavailableException("GitHub", new TimeoutException()),
        "provider-401" => new HttpRequestException("Credential rejected", null, HttpStatusCode.Unauthorized),
        "invalid-credential" => new UnauthorizedAccessException("Invalid credential"),
        "unknown-error" => new InvalidOperationException("Unexpected provider shape"),
        _ => throw new ArgumentOutOfRangeException(nameof(scenario))
    };

    private sealed class Fixture : IAsyncDisposable
    {
        internal const string IpAddress = "203.0.113.71";
        private readonly AttemptContext _database = new(new DbContextOptionsBuilder<AttemptContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        private readonly MemoryCache _cache = new(new MemoryCacheOptions());
        internal Mock<IOAuthService> Provider { get; } = new();
        internal Mock<IUserRepository> Users { get; } = new(MockBehavior.Strict);
        internal Mock<IJwtTokenService> Tokens { get; } = new(MockBehavior.Strict);
        internal List<AuthenticationAuditEvent> AuditEvents { get; } = [];
        internal AuthenticationAttemptRepository Repository { get; }
        internal UserEnumerationProtectionService Enumeration { get; }
        internal ThreatDetectionService Threats { get; }
        internal OAuthAuthService Service { get; }

        internal Fixture()
        {
            Repository = new AuthenticationAttemptRepository(_database);
            Enumeration = new UserEnumerationProtectionService(NullLogger<UserEnumerationProtectionService>.Instance, _cache);
            var audit = new Mock<IAuthenticationAuditEventSink>();
            audit.Setup(sink => sink.RecordAsync(It.IsAny<AuthenticationAuditEvent>(), It.IsAny<CancellationToken>()))
                .Callback<AuthenticationAuditEvent, CancellationToken>((value, _) => AuditEvents.Add(value)).Returns(Task.CompletedTask);
            var recording = new AuthAttemptService(Repository, Enumeration, NullLogger<AuthAttemptService>.Instance,
                auditEventSink: audit.Object);
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = IPAddress.Parse(IpAddress);
            context.Request.Headers.UserAgent = "OutageRegression/1.0";
            var configuration = new ConfigurationBuilder().Build();
            Threats = new ThreatDetectionService(Repository, NullLogger<ThreatDetectionService>.Instance,
                configuration, Mock.Of<ISiemIntegrationService>());
            Service = new OAuthAuthService(Users.Object, Tokens.Object, Mock.Of<IRefreshTokenHasher>(),
                Provider.Object, Mock.Of<IGoogleIdTokenVerifier>(), Mock.Of<IOidcFederationService>(),
                Mock.Of<IMfaService>(), Mock.Of<IExternalLoginRepository>(), configuration, recording,
                new HttpContextAccessor { HttpContext = context }, Mock.Of<ISender>(),
                Mock.Of<ISessionManagementService>(), NullLogger<OAuthAuthService>.Instance, auditEventSink: audit.Object);
        }

        public async ValueTask DisposeAsync()
        {
            _cache.Dispose();
            await _database.DisposeAsync();
        }
    }

    private sealed class AttemptContext(DbContextOptions<AttemptContext> options) : DbContext(options), IApplicationDbContext
    {
        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new NotSupportedException();
        }
        protected override void OnModelCreating(ModelBuilder builder) => builder.Entity<AuthenticationAttempt>().HasKey(attempt => attempt.Id);
    }
}
