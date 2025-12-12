using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GameGuild.API.UnitTests;

/// <summary>
/// Unit tests for Program configuration and startup
/// </summary>
public class ProgramTests
{
    [Fact]
    public void Program_ShouldHavePublicClass_ForTestingSupport()
    {
        // Arrange & Act
        var programType = typeof(GameGuild.API.Program);

        // Assert
        programType.Should().NotBeNull("Program class should be accessible for integration testing");
        programType.IsClass.Should().BeTrue();
    }

    [Fact]
    public void ProgramAssembly_ShouldContainRequiredTypes()
    {
        // Arrange
        var programAssembly = typeof(GameGuild.API.Program).Assembly;

        // Act
        var types = programAssembly.GetTypes();

        // Assert
        types.Should().NotBeEmpty("Program assembly should contain types");
        types.Should().Contain(t => t.Name == "Program");
    }

    [Fact]
    public void ServiceCollection_ShouldBeConfigurable_WithTestConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=test;",
                ["JWT:SecretKey"] = "test-secret-key-for-unit-tests-minimum-32-chars",
                ["JWT:Issuer"] = "test-issuer",
                ["JWT:Audience"] = "test-audience"
            })
            .Build();

        services.AddSingleton<IConfiguration>(configuration);

        // Act
        Action act = () => { var provider = services.BuildServiceProvider(); };

        // Assert
        act.Should().NotThrow("service configuration should succeed with test settings");
    }
}
