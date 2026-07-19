using FluentAssertions;
using GameGuild.API.Setup;

namespace GameGuild.API.UnitTests.Core;

public class ModuleConfigurationTests
{
    [Fact]
    public void DefaultEnabledModules_ShouldContainExpectedModules()
    {
        ModuleConfiguration.DefaultEnabledModules.Should().Contain("Authentication");
        ModuleConfiguration.DefaultEnabledModules.Should().Contain("Authorization");
        ModuleConfiguration.DefaultEnabledModules.Should().Contain("Users");
        ModuleConfiguration.DefaultEnabledModules.Should().Contain("Tenants");
        ModuleConfiguration.DefaultEnabledModules.Should().Contain("Payments");
        ModuleConfiguration.DefaultEnabledModules.Should().Contain("Projects");
        ModuleConfiguration.DefaultEnabledModules.Should().Contain("TestingLab");
        ModuleConfiguration.DefaultEnabledModules.Should().Contain("LaunchPad");
    }

    [Fact]
    public void DefaultEnabledModules_ShouldNotBeEmpty()
    {
        ModuleConfiguration.DefaultEnabledModules.Should().NotBeEmpty();
        ModuleConfiguration.DefaultEnabledModules.Should().HaveCountGreaterThan(5);
    }

    [Fact]
    public void FinancialCapabilityModules_ShouldBeKnownButDisabledByDefault()
    {
        ModuleConfiguration.DefaultDisabledModules.Should().BeEquivalentTo(
            "Economy.AdRewards",
            "Economy.Bounties",
            "Economy.Payouts",
            "Economy.Treasury",
            "Compliance.FinancialCrime",
            "TrustSafety");
        ModuleConfiguration.DefaultEnabledModules.Should()
            .NotIntersectWith(ModuleConfiguration.DefaultDisabledModules);
    }

    [Fact]
    public void HandlerTypeNames_ShouldContainExpectedTypes()
    {
        ModuleConfiguration.HandlerTypeNames.Should().Contain("ICommandHandler");
        ModuleConfiguration.HandlerTypeNames.Should().Contain("IQueryHandler");
        ModuleConfiguration.HandlerTypeNames.Should().Contain("IRequestHandler");
    }

    [Fact]
    public void NewInstance_ShouldHaveDefaults()
    {
        var config = new ModuleConfiguration();

        config.EnabledModules.Should().BeSameAs(ModuleConfiguration.DefaultEnabledModules);
        config.AssemblyPrefix.Should().Be("GameGuild.");
        config.ExcludeTestAssemblies.Should().BeTrue();
    }

    [Fact]
    public void EnabledModules_ShouldBeSettable()
    {
        var config = new ModuleConfiguration
        {
            EnabledModules = ["Authentication", "Users"]
        };

        config.EnabledModules.Should().HaveCount(2);
    }

    [Fact]
    public void AssemblyPrefix_ShouldBeSettable()
    {
        var config = new ModuleConfiguration { AssemblyPrefix = "Custom." };

        config.AssemblyPrefix.Should().Be("Custom.");
    }

    [Fact]
    public void ExcludeTestAssemblies_ShouldBeSettable()
    {
        var config = new ModuleConfiguration { ExcludeTestAssemblies = false };

        config.ExcludeTestAssemblies.Should().BeFalse();
    }

    [Theory]
    [InlineData("GameGuild.Content.Pages", true)]
    [InlineData("GameGuild.TestingLab", true)]
    [InlineData("GameGuild.Learning.Experience.LearningPaths", true)]
    [InlineData("GameGuild.Unknown", false)]
    [InlineData(null, false)]
    public void IsEnabledAssembly_ShouldMatchDottedAndCompactModuleNames(string? assemblyName, bool expected)
    {
        var config = new ModuleConfiguration();

        config.IsEnabledAssembly(assemblyName).Should().Be(expected);
    }
}
