using FluentAssertions;
using GameGuild.Identity.Context.Actors;
using Microsoft.Extensions.Logging;
using Xunit;

namespace GameGuild.Identity.Context.UnitTests.Actors;

public class SecurityAuditLoggerTests
{
    [Theory]
    [InlineData(SecurityEventType.ActorContextCreated, true, LogLevel.Debug)]
    [InlineData(SecurityEventType.UnauthorizedAccessAttempt, false, LogLevel.Warning)]
    [InlineData(SecurityEventType.PrivilegeEscalationAttempt, true, LogLevel.Information)]
    [InlineData(SecurityEventType.PrivilegeEscalationAttempt, false, LogLevel.Warning)]
    [InlineData(SecurityEventType.SensitiveResourceAccess, true, LogLevel.Information)]
    [InlineData(SecurityEventType.ContextElevated, true, LogLevel.Information)]
    [InlineData(SecurityEventType.ContextElevationExpired, true, LogLevel.Information)]
    [InlineData(SecurityEventType.ImpersonationStarted, true, LogLevel.Warning)]
    [InlineData(SecurityEventType.ImpersonationEnded, true, LogLevel.Information)]
    [InlineData(SecurityEventType.SessionTerminated, true, LogLevel.Information)]
    [InlineData(SecurityEventType.CrossTenantAccess, true, LogLevel.Information)]
    [InlineData(SecurityEventType.CrossTenantAccess, false, LogLevel.Error)]
    [InlineData((SecurityEventType)999, true, LogLevel.Information)]
    public async Task LogAsync_Should_Map_Event_Types_To_Expected_LogLevels(SecurityEventType eventType, bool success, LogLevel expectedLevel)
    {
        var logger = new TestLogger<SecurityAuditLogger>();
        var auditLogger = new SecurityAuditLogger(logger);
        var auditEvent = new SecurityAuditEvent
        {
            EventId = Guid.NewGuid(),
            EventType = eventType,
            Timestamp = DateTime.UtcNow,
            SubjectId = "user",
            TenantId = Guid.NewGuid(),
            ActorKind = ActorKind.User,
            Success = success
        };

        await auditLogger.LogAsync(auditEvent);

        logger.LastLevel.Should().Be(expectedLevel);
    }

    [Fact]
    public async Task LogUnauthorizedAccessAsync_Should_Log_Warning()
    {
        var logger = new TestLogger<SecurityAuditLogger>();
        var auditLogger = new SecurityAuditLogger(logger);
        var context = ActorContextBuilder.ForUser(Guid.NewGuid()).Build();

        await auditLogger.LogUnauthorizedAccessAsync(context, "resource", "1", "resource:read");

        logger.LastLevel.Should().Be(LogLevel.Warning);
    }

    [Fact]
    public async Task LogSensitiveAccessAsync_Should_Log_Information()
    {
        var logger = new TestLogger<SecurityAuditLogger>();
        var auditLogger = new SecurityAuditLogger(logger);
        var context = ActorContextBuilder.ForUser(Guid.NewGuid()).Build();

        await auditLogger.LogSensitiveAccessAsync(context, "resource", "1", "view");

        logger.LastLevel.Should().Be(LogLevel.Information);
    }

    [Fact]
    public async Task LogPrivilegeEscalationAsync_Should_Log_Based_On_Success_Flag()
    {
        var logger = new TestLogger<SecurityAuditLogger>();
        var auditLogger = new SecurityAuditLogger(logger);
        var context = ActorContextBuilder.ForUser(Guid.NewGuid()).Build();

        await auditLogger.LogPrivilegeEscalationAsync(context, ["Member"], ["Admin"], success: false, reason: "denied");
        logger.LastLevel.Should().Be(LogLevel.Warning);

        await auditLogger.LogPrivilegeEscalationAsync(context, ["Member"], ["Admin"], success: true, reason: "approved");
        logger.LastLevel.Should().Be(LogLevel.Information);
    }

    [Fact]
    public async Task LogCrossTenantAccessAsync_Should_Log_Error_On_Failure()
    {
        var logger = new TestLogger<SecurityAuditLogger>();
        var auditLogger = new SecurityAuditLogger(logger);
        var context = ActorContextBuilder.ForUser(Guid.NewGuid()).Build();

        await auditLogger.LogCrossTenantAccessAsync(context, Guid.NewGuid(), Guid.NewGuid(), "resource", success: false);

        logger.LastLevel.Should().Be(LogLevel.Error);
    }

    [Fact]
    public async Task LogCrossTenantAccessAsync_Should_Log_Information_On_Success()
    {
        var logger = new TestLogger<SecurityAuditLogger>();
        var auditLogger = new SecurityAuditLogger(logger);
        var context = ActorContextBuilder.ForUser(Guid.NewGuid()).Build();

        await auditLogger.LogCrossTenantAccessAsync(context, Guid.NewGuid(), Guid.NewGuid(), "resource", success: true);

        logger.LastLevel.Should().Be(LogLevel.Information);
    }

    [Fact]
    public void Constructor_Should_Throw_When_Logger_Null()
    {
        var act = () => new SecurityAuditLogger(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task LogAsync_Should_Rethrow_When_Logger_Fails_During_Primary_Log()
    {
        var logger = new ThrowingLogger<SecurityAuditLogger>();
        var auditLogger = new SecurityAuditLogger(logger);
        var auditEvent = SecurityAuditEvent.Create(SecurityEventType.ActorContextCreated, success: true);

        var act = () => auditLogger.LogAsync(auditEvent);

        await act.Should().ThrowAsync<InvalidOperationException>();
        logger.DebugLogAttempted.Should().BeTrue();
    }

    private sealed class TestLogger<T> : ILogger<T>
    {
        public LogLevel? LastLevel { get; private set; }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => new NoopScope();

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            LastLevel = logLevel;
        }

        private sealed class NoopScope : IDisposable
        {
            public void Dispose() { }
        }
    }

    private sealed class ThrowingLogger<T> : ILogger<T>
    {
        private bool _hasThrown;

        public bool DebugLogAttempted { get; private set; }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => new NoopScope();

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!_hasThrown)
            {
                _hasThrown = true;
                throw new InvalidOperationException("boom");
            }

            if (logLevel == LogLevel.Debug)
            {
                DebugLogAttempted = true;
            }
        }

        private sealed class NoopScope : IDisposable
        {
            public void Dispose() { }
        }
    }
}
