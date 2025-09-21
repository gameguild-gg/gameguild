using GameGuild.Source.Modules.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GameGuild.Tests.Modules.Authorization.Unit;

/// <summary>
/// Unit tests for AuthorizationModule configuration and service registration
/// </summary>
public class AuthorizationModuleTests {
    [Fact]
    public void AuthorizationModule_ShouldHaveCorrectMetadata() {
        // Arrange & Act
        var module = new AuthorizationModule();

        // Assert
        Assert.Equal("Authorization", module.ModuleName);
        Assert.Equal("1.0.0", module.ModuleVersion);
    }

    [Fact]
    public void ConfigureServices_ShouldRegisterRequiredServices() {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        var module = new AuthorizationModule();

        // Act
        var result = module.ConfigureServices(services, configuration);

        // Assert
        Assert.Same(services, result); // Should return the same collection for chaining

        // Verify that services were registered (check service descriptors)
        var serviceDescriptors = services.ToList();
        Assert.NotEmpty(serviceDescriptors);

        // Check for specific service registrations
        Assert.Contains(serviceDescriptors, sd => sd.ServiceType.Name.Contains("Permission"));
    }

    [Fact]
    public void AuthorizationModuleExtensions_ShouldProvideStandardizedPattern() {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // Act
        var result = AuthorizationModuleExtensions.AddAuthorizationModule(services, configuration);

        // Assert
        Assert.Same(services, result); // Should return the same collection for chaining
    }

    [Theory]
    [InlineData("1.0.0")]
    [InlineData("2.0.0")]
    public void ModuleVersion_ShouldBeValidSemanticVersion(string expectedVersion) {
        // Arrange
        var module = new AuthorizationModule();

        // Act
        var version = module.ModuleVersion;

        // Assert
        if (expectedVersion == "1.0.0") {
            Assert.Equal(expectedVersion, version);
        }

        // Verify it's a valid semantic version format
        Assert.Matches(@"^\d+\.\d+\.\d+$", version);
    }
}
