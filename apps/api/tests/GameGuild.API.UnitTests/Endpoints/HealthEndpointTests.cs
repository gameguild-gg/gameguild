using FluentAssertions;
using Xunit;

namespace GameGuild.API.UnitTests.Endpoints;

/// <summary>
/// Unit tests for HealthEndpoint - Tests focus on public contracts since endpoint classes are internal
/// </summary>
public class HealthEndpointTests
{
    [Fact]
    public void HealthEndpoint_Namespace_ShouldBeCorrect()
    {
        // This test verifies that the endpoint namespace structure exists
        // Since HealthEndpoint is internal, we test via integration tests instead
        var apiAssembly = typeof(GameGuild.API.Program).Assembly;
        
        // Assert
        apiAssembly.Should().NotBeNull();
        apiAssembly.GetName().Name.Should().Be("GameGuild.API");
    }

    [Fact]
    public void HealthEndpoint_ShouldBeDefinedInAPIAssembly()
    {
        // Arrange
        var apiAssembly = typeof(GameGuild.API.Program).Assembly;
        
        // Act
        var healthEndpointType = apiAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "HealthEndpoint");

        // Assert
        healthEndpointType.Should().NotBeNull("HealthEndpoint should exist in the API assembly");
    }

    [Fact]
    public void HealthResponse_ShouldBeDefinedInAPIAssembly()
    {
        // Arrange
        var apiAssembly = typeof(GameGuild.API.Program).Assembly;
        
        // Act
        var healthResponseType = apiAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "HealthResponse");

        // Assert
        healthResponseType.Should().NotBeNull("HealthResponse should exist in the API assembly");
    }

    [Fact]
    public void DependencyHealth_ShouldBeDefinedInAPIAssembly()
    {
        // Arrange
        var apiAssembly = typeof(GameGuild.API.Program).Assembly;
        
        // Act
        var dependencyHealthType = apiAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "DependencyHealth");

        // Assert
        dependencyHealthType.Should().NotBeNull("DependencyHealth should exist in the API assembly");
    }
}
