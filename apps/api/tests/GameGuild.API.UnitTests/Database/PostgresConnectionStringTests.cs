using FluentAssertions;
using GameGuild.API.Database;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace GameGuild.API.UnitTests.Database;

public sealed class PostgresConnectionStringTests
{
    [Fact]
    public void Normalize_ShouldLeaveNpgsqlKeywordConnectionStringUnchanged()
    {
        const string connectionString = "Host=localhost;Database=game_guild;Username=game_guild;Password=secret";

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
                ["POSTGRES_SSLMODE"] = "require"
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
        builder.GssEncryptionMode.Should().Be(GssEncryptionMode.Disable);
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

        PostgresConnectionString.Resolve(configuration).Should().Be(connectionString);
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
}
