using FluentAssertions;
using GameGuild.API.Database;
using Microsoft.Extensions.Configuration;

namespace GameGuild.API.UnitTests.Database;

public class DatabaseConnectivityProbeTests
{
    [Fact]
    public async Task IsReachableAsync_ShouldReturnFalse_WhenConnectionStringIsMissing()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        var probe = new DatabaseConnectivityProbe(configuration);

        var result = await probe.IsReachableAsync();

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsReachableAsync_ShouldReturnFalse_WhenConnectionStringIsInvalid()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "not-a-valid-connection-string"
            })
            .Build();
        var probe = new DatabaseConnectivityProbe(configuration);

        var result = await probe.IsReachableAsync();

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsReachableAsync_ShouldReturnFalse_WhenHostIsMissing()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Database=test;Username=test;Password=test;Port=5432"
            })
            .Build();
        var probe = new DatabaseConnectivityProbe(configuration);

        var result = await probe.IsReachableAsync();

        result.Should().BeFalse();
    }
}
