using FluentAssertions;
using GameGuild.API.Database;
using Microsoft.AspNetCore.Builder;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace GameGuild.API.UnitTests.Database;

public sealed class DatabaseStartupMigrationFailureGuardTests
{
    [Fact]
    public void ResolveFailStartupOnMigrationFailure_IgnoresExplicitOptOutInProduction()
    {
        var configuration = CreateConfiguration("Database:FailStartupOnMigrationFailure", "false");

        DatabaseStartupConfiguration.ResolveFailStartupOnMigrationFailure(configuration, Environments.Production)
            .Should().BeTrue();
    }

    [Fact]
    public void ResolveFailStartupOnMigrationFailure_FailsClosedByDefaultInProduction()
    {
        var configuration = CreateConfiguration(null, null);

        DatabaseStartupConfiguration.ResolveFailStartupOnMigrationFailure(configuration, Environments.Production)
            .Should().BeTrue();
    }

    [Fact]
    public void ResolveFailStartupOnMigrationFailure_HonorsOptInEverywhere()
    {
        var configuration = CreateConfiguration("Database:FailStartupOnMigrationFailure", "true");

        DatabaseStartupConfiguration.ResolveFailStartupOnMigrationFailure(configuration, Environments.Development)
            .Should().BeTrue();
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Test")]
    [InlineData("Testing")]
    public void ResolveFailStartupOnMigrationFailure_AllowsFallbackOptOutOutsideProduction(string environmentName)
    {
        var configuration = CreateConfiguration("Database:FailStartupOnMigrationFailure", "false");

        DatabaseStartupConfiguration.ResolveFailStartupOnMigrationFailure(configuration, environmentName)
            .Should().BeFalse();
    }

    [Fact]
    public void ResolveFailStartupOnMigrationFailure_FallsBackToContinueInDevelopmentWhenOmitted()
    {
        var configuration = CreateConfiguration(null, null);

        DatabaseStartupConfiguration.ResolveFailStartupOnMigrationFailure(configuration, Environments.Development)
            .Should().BeFalse();
    }

    [Fact]
    public async Task ApplyMigrationsAsync_WhenMigrationFailsInProductionWithExplicitOptOut_ShouldStillThrow()
    {
        var directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"startup-{Guid.NewGuid():N}"));
        try
        {
            await using var app = CreateApp(Environments.Production, new Dictionary<string, string?>
            {
                ["ConnectionStrings:MigrationConnection"] = MigrationConnection,
                ["Database:GrantRuntimeRoleAfterMigrations"] = "false",
                ["Database:MigrationMaxAttempts"] = "1",
                ["Database:FailStartupOnMigrationFailure"] = "false"
            });

            Func<Task<bool>> act = () => DatabaseStartupInitializer.ApplyMigrationsAsync(app, CreateFailingContext(directory.FullName));

            await act.Should().ThrowAsync<SqliteException>();
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task ApplyMigrationsAsync_WhenMigrationFailsInDevelopmentWithExplicitOptOut_ShouldReturnFalse()
    {
        var directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"startup-{Guid.NewGuid():N}"));
        try
        {
            await using var app = CreateApp(Environments.Development, new Dictionary<string, string?>
            {
                ["ConnectionStrings:MigrationConnection"] = MigrationConnection,
                ["Database:GrantRuntimeRoleAfterMigrations"] = "false",
                ["Database:MigrationMaxAttempts"] = "1",
                ["Database:FailStartupOnMigrationFailure"] = "false"
            });

            var result = await DatabaseStartupInitializer.ApplyMigrationsAsync(app, CreateFailingContext(directory.FullName));

            result.Should().BeFalse();
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    private static IConfiguration CreateConfiguration(string? key, string? value)
    {
        var settings = new Dictionary<string, string?>();
        if (key is not null)
            settings[key] = value;

        return new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
    }

    private static WebApplication CreateApp(
        string environmentName,
        IReadOnlyDictionary<string, string?> settings)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = environmentName,
            ApplicationName = typeof(DatabaseStartupInitializer).Assembly.FullName
        });
        builder.Configuration.AddInMemoryCollection(settings);
        builder.Services.AddSingleton<DatabaseConnectivityProbe>();
        return builder.Build();
    }

    private static Func<string, ApplicationDbContext> CreateFailingContext(string unusableDataSource) =>
        _ => new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite($"Data Source={unusableDataSource}")
                .ReplaceService<IMigrationsAssembly, CoverageMigrationsAssembly>()
                .Options);

    private const string MigrationConnection =
        "Host=database;Database=app;Username=migration_user;Password=migration-secret";
}
