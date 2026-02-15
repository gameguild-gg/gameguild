using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace GameGuild.API.UnitTests.Core;

public class DependencyInjectionTests
{
    [Fact]
    public void GetApplicationAssemblies_WithNullEntry_ShouldThrow()
    {
        var act = () => DependencyInjection.GetApplicationAssemblies(null!, Array.Empty<Assembly>());
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetApplicationAssemblies_WithNullAdditional_ShouldThrow()
    {
        var act = () => DependencyInjection.GetApplicationAssemblies(
            Assembly.GetExecutingAssembly(), null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetApplicationAssemblies_WithEntryOnly_ShouldReturnDistinctAssemblies()
    {
        var entry = Assembly.GetExecutingAssembly();
        var result = DependencyInjection.GetApplicationAssemblies(entry);

        result.Should().NotBeEmpty();
        result.Should().OnlyHaveUniqueItems();
        result.Should().Contain(entry);
    }

    [Fact]
    public void GetApplicationAssemblies_WithAdditional_ShouldIncludeThem()
    {
        var entry = Assembly.GetExecutingAssembly();
        var extra = typeof(object).Assembly;
        var result = DependencyInjection.GetApplicationAssemblies(entry, extra);

        result.Should().Contain(extra);
        result.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void GetApplicationAssemblies_WithDuplicates_ShouldDedup()
    {
        var entry = Assembly.GetExecutingAssembly();
        var result = DependencyInjection.GetApplicationAssemblies(entry, entry, entry);

        result.Where(a => a == entry).Should().HaveCount(1);
    }

    [Fact]
    public void GetAssembliesByPattern_DefaultPattern_ShouldReturnOnlyMatchingAssemblies()
    {
        var result = DependencyInjection.GetAssembliesByPattern();

        // In unit test context, the result may be empty or contain GameGuild assemblies.
        // Verify that any returned assemblies actually match the pattern.
        foreach (var assembly in result)
        {
            assembly.FullName.Should().StartWith("GameGuild");
        }
    }

    [Fact]
    public void GetAssembliesByPattern_NonexistentPattern_ShouldReturnEmpty()
    {
        var result = DependencyInjection.GetAssembliesByPattern("ZzzDoesNotExist.*");

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetRegistrationMetrics_WhenNotRegistered_ShouldReturnDefaults()
    {
        var services = new ServiceCollection();
        var sp = services.BuildServiceProvider();

        var metrics = DependencyInjection.GetRegistrationMetrics(sp);

        metrics.Should().NotBeNull();
        metrics.TotalHandlersRegistered.Should().Be(0);
        metrics.TotalValidatorsRegistered.Should().Be(0);
        metrics.RegistrationDuration.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void GetRegistrationMetrics_WhenRegistered_ShouldReturnStoredMetrics()
    {
        var expected = new RegistrationMetrics
        {
            TotalHandlersRegistered = 42,
            TotalValidatorsRegistered = 15,
            RegistrationDuration = TimeSpan.FromMilliseconds(123)
        };
        var services = new ServiceCollection();
        services.AddSingleton(expected);
        var sp = services.BuildServiceProvider();

        var metrics = DependencyInjection.GetRegistrationMetrics(sp);

        metrics.Should().BeSameAs(expected);
        metrics.TotalHandlersRegistered.Should().Be(42);
        metrics.TotalValidatorsRegistered.Should().Be(15);
    }
}
