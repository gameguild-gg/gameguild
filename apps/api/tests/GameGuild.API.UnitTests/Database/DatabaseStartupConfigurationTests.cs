using FluentAssertions;
using GameGuild.API.Database;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace GameGuild.API.UnitTests.Database;

public sealed class DatabaseStartupConfigurationTests
{
    [Fact]
    public void Validate_RejectsMissingMigrationConnectionInProduction()
    {
        var configuration = CreateConfiguration(RuntimeConnection);

        var failures = DatabaseStartupConfiguration.Validate(configuration, Environments.Production);

        failures.Should().Contain(message => message.Contains("migration connection", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_RejectsSharedRuntimeAndMigrationRoleInProduction()
    {
        var configuration = CreateConfiguration(RuntimeConnection, RuntimeConnection);

        var failures = DatabaseStartupConfiguration.Validate(configuration, Environments.Production);

        failures.Should().Contain(message => message.Contains("distinct", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_AcceptsDistinctRuntimeAndMigrationRolesInProduction()
    {
        var configuration = CreateConfiguration(
            RuntimeConnection,
            "Host=database;Database=game_guild;Username=game_guild_migrator;Password=migration-secret");

        var failures = DatabaseStartupConfiguration.Validate(configuration, Environments.Production);

        failures.Should().BeEmpty();
    }

    [Fact]
    public void Validate_AcceptsPostgresMigrationConnectionEnvironmentAliasInProduction()
    {
        var configuration = CreateConfiguration(RuntimeConnection, values: new Dictionary<string, string?>
        {
            ["POSTGRES_MIGRATION_CONNECTION"] =
                "Host=database;Database=game_guild;Username=game_guild_migrator;Password=migration-secret"
        });

        var failures = DatabaseStartupConfiguration.Validate(configuration, Environments.Production);

        failures.Should().BeEmpty();
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Test")]
    [InlineData("Testing")]
    public void Validate_AllowsDevelopmentAndTestToUseRuntimeMigrationFallback(string environmentName)
    {
        var configuration = CreateConfiguration(RuntimeConnection);

        var failures = DatabaseStartupConfiguration.Validate(configuration, environmentName);

        failures.Should().BeEmpty();
    }

    [Fact]
    public void Validate_AllowsExternalMigrationWhenStartupInitializationIsDisabled()
    {
        var configuration = CreateConfiguration(RuntimeConnection, values: new Dictionary<string, string?>
        {
            ["Database:RunStartupInitialization"] = "false"
        });

        var failures = DatabaseStartupConfiguration.Validate(configuration, Environments.Production);

        failures.Should().BeEmpty();
    }

    [Fact]
    public void DesignTimeFactory_PrefersMigrationConnection()
    {
        const string migrationConnection =
            "Host=database;Database=game_guild;Username=game_guild_migrator;Password=migration-secret";
        var configuration = CreateConfiguration(RuntimeConnection, migrationConnection);

        var resolved = DesignTimeDbContextFactory.ResolveConnectionString(configuration);

        resolved.Should().Contain("game_guild_migrator");
        resolved.Should().NotContain("game_guild_runtime");
    }

    private const string RuntimeConnection =
        "Host=database;Database=game_guild;Username=game_guild_runtime;Password=runtime-secret";

    private static IConfiguration CreateConfiguration(
        string runtimeConnection,
        string? migrationConnection = null,
        IReadOnlyDictionary<string, string?>? values = null)
    {
        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = runtimeConnection
        };
        if (migrationConnection is not null)
            settings["ConnectionStrings:MigrationConnection"] = migrationConnection;
        if (values is not null)
        {
            foreach (var (key, value) in values)
                settings[key] = value;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
    }
}
