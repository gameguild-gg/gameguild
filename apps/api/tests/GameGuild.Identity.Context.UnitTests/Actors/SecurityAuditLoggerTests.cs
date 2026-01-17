using FluentAssertions;
using GameGuild.Identity.Context.Actors;
using Microsoft.Extensions.Logging;
using Xunit;

namespace GameGuild.Identity.Context.UnitTests.Actors;

public class SecurityAuditLoggerTests
{
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
    public async Task LogCrossTenantAccessAsync_Should_Log_Error_On_Failure()
    {
        var logger = new TestLogger<SecurityAuditLogger>();
        var auditLogger = new SecurityAuditLogger(logger);
        var context = ActorContextBuilder.ForUser(Guid.NewGuid()).Build();

        await auditLogger.LogCrossTenantAccessAsync(context, Guid.NewGuid(), Guid.NewGuid(), "resource", success: false);

        logger.LastLevel.Should().Be(LogLevel.Error);
    }

    [Fact]
    public void Constructor_Should_Throw_When_Logger_Null()
    {
        var act = () => new SecurityAuditLogger(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
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
}
