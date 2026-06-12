using FluentAssertions;
using GameGuild.API.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GameGuild.API.UnitTests.Database;

public sealed class DatabaseSeederTests
{
    [Fact]
    public async Task SeedAsync_Should_Not_Log_Warnings_When_LegacyIdentityManagers_Are_Not_Registered()
    {
        var services = new ServiceCollection();
        var logger = new CapturingLogger<ApplicationDbContext>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Seed:AdminPassword"] = "UnitTestAdmin123!"
            })
            .Build();

        _ = typeof(GameGuild.Identity.Users.User);

        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<ILogger<ApplicationDbContext>>(logger);
        services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        await using var provider = services.BuildServiceProvider();

        await DatabaseSeeder.SeedAsync(provider).ConfigureAwait(false);

        logger.Messages.Where(message => message.Level >= LogLevel.Warning).Should().BeEmpty();
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Text)> Messages { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add((logLevel, formatter(state, exception)));
        }

        private sealed class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new();

            public void Dispose() { }
        }
    }
}
