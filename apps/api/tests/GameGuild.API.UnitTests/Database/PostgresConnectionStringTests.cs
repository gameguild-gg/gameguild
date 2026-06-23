using FluentAssertions;
using GameGuild.API.Database;
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
    }
}
