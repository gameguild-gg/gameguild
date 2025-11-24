using FluentAssertions;
using GameGuild.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GameGuild.API.UnitTests.Core;

/// <summary>
/// Unit tests for WebApplicationExtensions
/// </summary>
public class WebApplicationExtensionsTests
{
    [Fact]
    public void ConfigureCommonPipeline_WithNullApp_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        var act = () => WebApplicationExtensions.ConfigureCommonPipeline(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ConfigurePipeline_WithNullApp_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        var act = () => WebApplicationExtensions.ConfigurePipeline(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WebApplicationExtensions_ShouldExistInCoreNamespace()
    {
        // Arrange
        var apiAssembly = typeof(GameGuild.API.Program).Assembly;
        
        // Act
        var extensionsType = apiAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "WebApplicationExtensions");

        // Assert
        extensionsType.Should().NotBeNull("WebApplicationExtensions should exist in the API assembly");
    }

    [Fact]
    public void WebApplicationExtensions_ShouldHaveConfigurePipelineMethod()
    {
        // Arrange
        var apiAssembly = typeof(GameGuild.API.Program).Assembly;
        var extensionsType = apiAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "WebApplicationExtensions");

        // Act
        var configurePipelineMethod = extensionsType?.GetMethod("ConfigurePipeline");

        // Assert
        configurePipelineMethod.Should().NotBeNull("ConfigurePipeline method should exist");
    }

    [Fact]
    public void WebApplicationExtensions_ShouldHaveConfigureCommonPipelineMethod()
    {
        // Arrange
        var apiAssembly = typeof(GameGuild.API.Program).Assembly;
        var extensionsType = apiAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "WebApplicationExtensions");

        // Act
        var method = extensionsType?.GetMethod("ConfigureCommonPipeline");

        // Assert
        method.Should().NotBeNull("ConfigureCommonPipeline method should exist");
    }

    [Fact]
    public void WebApplicationExtensions_ShouldHaveConfigureDevelopmentPipelineMethod()
    {
        // Arrange
        var apiAssembly = typeof(GameGuild.API.Program).Assembly;
        var extensionsType = apiAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "WebApplicationExtensions");

        // Act
        var method = extensionsType?.GetMethod("ConfigureDevelopmentPipeline");

        // Assert
        method.Should().NotBeNull("ConfigureDevelopmentPipeline method should exist");
    }

    [Fact]
    public void WebApplicationExtensions_ShouldHaveConfigureProductionPipelineMethod()
    {
        // Arrange
        var apiAssembly = typeof(GameGuild.API.Program).Assembly;
        var extensionsType = apiAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "WebApplicationExtensions");

        // Act
        var method = extensionsType?.GetMethod("ConfigureProductionPipeline");

        // Assert
        method.Should().NotBeNull("ConfigureProductionPipeline method should exist");
    }
}
