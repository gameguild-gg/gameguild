using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Microsoft.Extensions.Configuration.Json;

namespace GameGuild.API.UnitTests.Core;

public sealed class WebApplicationBuilderExtensionsTests
{
    [Fact]
    public void AddAppSettings_AddsOnlyTheMissingApplicationSettingsSource()
    {
        var builder = CreateBuilder();
        RemoveJsonSource(builder, "appsettings.json");

        builder.AddAppSettings();

        CountJsonSource(builder, "appsettings.json").Should().Be(1);
        CountJsonSource(builder, "appsettings.Testing.json").Should().Be(1);
    }

    [Fact]
    public void AddAppSettings_AddsOnlyTheMissingEnvironmentSettingsSource()
    {
        var builder = CreateBuilder();
        RemoveJsonSource(builder, "appsettings.Testing.json");

        builder.AddAppSettings();

        CountJsonSource(builder, "appsettings.json").Should().Be(1);
        CountJsonSource(builder, "appsettings.Testing.json").Should().Be(1);
    }

    [Fact]
    public void AddAppSettings_WhenSourcesExist_DoesNotDuplicateThem()
    {
        var builder = CreateBuilder();

        builder.AddAppSettings();

        CountJsonSource(builder, "appsettings.json").Should().Be(1);
        CountJsonSource(builder, "appsettings.Testing.json").Should().Be(1);
    }

    [Fact]
    public void AddEnvironmentVariables_WhenUnprefixedSourceIsMissing_AddsItOnce()
    {
        var builder = CreateBuilder();
        foreach (var source in builder.Configuration.Sources
                     .OfType<EnvironmentVariablesConfigurationSource>()
                     .Where(source => string.IsNullOrEmpty(source.Prefix))
                     .ToArray())
        {
            builder.Configuration.Sources.Remove(source);
        }

        builder.AddEnvironmentVariables();
        builder.AddEnvironmentVariables();

        builder.Configuration.Sources
            .OfType<EnvironmentVariablesConfigurationSource>()
            .Count(source => string.IsNullOrEmpty(source.Prefix))
            .Should().Be(1);
    }

    private static WebApplicationBuilder CreateBuilder() =>
        WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });

    private static void RemoveJsonSource(WebApplicationBuilder builder, string path)
    {
        foreach (var source in builder.Configuration.Sources
                     .OfType<JsonConfigurationSource>()
                     .Where(source => string.Equals(source.Path, path, StringComparison.OrdinalIgnoreCase))
                     .ToArray())
        {
            builder.Configuration.Sources.Remove(source);
        }
    }

    private static int CountJsonSource(WebApplicationBuilder builder, string path) =>
        builder.Configuration.Sources
            .OfType<JsonConfigurationSource>()
            .Count(source => string.Equals(source.Path, path, StringComparison.OrdinalIgnoreCase));
}
