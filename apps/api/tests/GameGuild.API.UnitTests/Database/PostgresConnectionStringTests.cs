using FluentAssertions;
using GameGuild.API.Database;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace GameGuild.API.UnitTests.Database;

public sealed class PostgresConnectionStringTests
{
    [Fact]
    public void Normalize_ShouldDisableGssOnNpgsqlKeywordConnectionString()
    {
        const string connectionString = "Host=localhost;Database=game_guild;Username=game_guild;Password=secret";

        var normalized = PostgresConnectionString.Normalize(connectionString);

        var builder = new NpgsqlConnectionStringBuilder(normalized);
        builder.Host.Should().Be("localhost");
        builder.Database.Should().Be("game_guild");
        builder.Username.Should().Be("game_guild");
        builder.Password.Should().Be("secret");
        builder.GssEncryptionMode.Should().Be(GssEncryptionMode.Disable);
    }

    [Fact]
    public void Normalize_ShouldReturnNullOrWhitespaceUnchanged()
    {
        PostgresConnectionString.Normalize(null).Should().BeNull();
        PostgresConnectionString.Normalize("").Should().Be("");
        PostgresConnectionString.Normalize("   ").Should().Be("   ");
    }

    [Fact]
    public void Normalize_ShouldLeaveNonPostgresUriUnchanged()
    {
        const string connectionString = "mysql://user:secret@db.example.com:3306/game_guild";

        PostgresConnectionString.Normalize(connectionString).Should().Be(connectionString);
    }

    [Fact]
    public void Normalize_ShouldConvertPostgresUrlToNpgsqlKeywordConnectionString()
    {
        var normalized = PostgresConnectionString.Normalize("postgres://game_guild:secret@db.example.com:6543/game_guild");

        var builder = new NpgsqlConnectionStringBuilder(normalized);
        builder.Host.Should().Be("db.example.com");
        builder.Port.Should().Be(6543);
        builder.Database.Should().Be("game_guild");
        builder.Username.Should().Be("game_guild");
        builder.Password.Should().Be("secret");
        builder.GssEncryptionMode.Should().Be(GssEncryptionMode.Disable);
    }

    [Fact]
    public void Resolve_ShouldBuildConnectionStringFromPostgresPartsBeforeConfiguredConnectionString()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "not-a-valid-connection-string",
                ["POSTGRES_HOST"] = "db.internal",
                ["POSTGRES_PORT"] = "6543",
                ["POSTGRES_DB"] = "game_guild",
                ["POSTGRES_USER"] = "game_guild",
                ["POSTGRES_PASSWORD"] = "secret",
                ["POSTGRES_SSLMODE"] = "require",
                ["POSTGRES_MAX_POOL_SIZE"] = "120",
                ["POSTGRES_MIN_POOL_SIZE"] = "4",
                ["POSTGRES_CONNECTION_IDLE_LIFETIME"] = "60"
            })
            .Build();

        var resolved = PostgresConnectionString.Resolve(configuration);

        var builder = new NpgsqlConnectionStringBuilder(resolved);
        builder.Host.Should().Be("db.internal");
        builder.Port.Should().Be(6543);
        builder.Database.Should().Be("game_guild");
        builder.Username.Should().Be("game_guild");
        builder.Password.Should().Be("secret");
        builder.SslMode.Should().Be(SslMode.Require);
        builder.MaxPoolSize.Should().Be(120);
        builder.MinPoolSize.Should().Be(4);
        builder.ConnectionIdleLifetime.Should().Be(60);
        builder.GssEncryptionMode.Should().Be(GssEncryptionMode.Disable);
    }

    [Fact]
    public void Resolve_ShouldBuildConnectionStringFromPostgresPartsWithoutOptionalValues()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["POSTGRES_HOST"] = "db.internal",
                ["POSTGRES_DB"] = "game_guild",
                ["POSTGRES_USER"] = "game_guild",
                ["POSTGRES_PASSWORD"] = "secret"
            })
            .Build();

        var resolved = PostgresConnectionString.Resolve(configuration);

        var builder = new NpgsqlConnectionStringBuilder(resolved);
        builder.Port.Should().Be(5432);
        builder.SslMode.Should().Be(SslMode.Prefer);
    }

    [Fact]
    public void Resolve_ShouldThrowWhenConfigurationIsNull()
    {
        var act = () => PostgresConnectionString.Resolve(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Resolve_ShouldFallbackToConfiguredConnectionStringWhenPostgresPartsAreIncomplete()
    {
        const string connectionString = "Host=localhost;Database=game_guild;Username=game_guild;Password=secret";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connectionString,
                ["POSTGRES_HOST"] = "db.internal"
            })
            .Build();

        var resolved = PostgresConnectionString.Resolve(configuration);

        new NpgsqlConnectionStringBuilder(resolved).GssEncryptionMode.Should().Be(GssEncryptionMode.Disable);
    }

    [Fact]
    public void Normalize_ShouldDecodeEscapedUserInfoAndSslMode()
    {
        var normalized = PostgresConnectionString.Normalize(
            "postgresql://game%20guild:p%40ss@db.example.com/game_guild?sslmode=require");

        var builder = new NpgsqlConnectionStringBuilder(normalized);
        builder.Username.Should().Be("game guild");
        builder.Password.Should().Be("p@ss");
        builder.SslMode.Should().Be(SslMode.Require);
        builder.GssEncryptionMode.Should().Be(GssEncryptionMode.Disable);
    }

    [Fact]
    public void Resolve_ShouldUseDefaultsForInvalidOptionalPostgresParts()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["POSTGRES_HOST"] = " db.internal ",
                ["POSTGRES_PORT"] = "0",
                ["POSTGRES_DB"] = " game_guild ",
                ["POSTGRES_USER"] = " game_guild ",
                ["POSTGRES_PASSWORD"] = "secret",
                ["POSTGRES_MAX_POOL_SIZE"] = "not-an-int",
                ["POSTGRES_MIN_POOL_SIZE"] = "-1",
                ["POSTGRES_CONNECTION_IDLE_LIFETIME"] = "-1",
                ["POSTGRES_INCLUDE_ERROR_DETAIL"] = "true",
                ["POSTGRES_SSLMODE"] = "invalid"
            })
            .Build();

        var resolved = PostgresConnectionString.Resolve(configuration);

        var builder = new NpgsqlConnectionStringBuilder(resolved);
        builder.Host.Should().Be("db.internal");
        builder.Port.Should().Be(5432);
        builder.Database.Should().Be("game_guild");
        builder.Username.Should().Be("game_guild");
        builder.IncludeErrorDetail.Should().BeTrue();
        builder.MaxPoolSize.Should().Be(100);
        builder.MinPoolSize.Should().Be(5);
        builder.ConnectionIdleLifetime.Should().Be(300);
        builder.GssEncryptionMode.Should().Be(GssEncryptionMode.Disable);
    }

    [Fact]
    public void Normalize_ShouldIgnoreQueryParametersWithoutValues()
    {
        var normalized = PostgresConnectionString.Normalize("postgres://user@db.example.com/game_guild?sslmode&ssl_mode=disable&unknown=value");

        var builder = new NpgsqlConnectionStringBuilder(normalized);
        builder.Host.Should().Be("db.example.com");
        builder.Username.Should().Be("user");
        builder.Password.Should().BeNull();
        builder.SslMode.Should().Be(SslMode.Disable);
    }
}
