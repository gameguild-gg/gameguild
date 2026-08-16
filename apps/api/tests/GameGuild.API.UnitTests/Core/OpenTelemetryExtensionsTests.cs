using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using GameGuild.API.Setup;
using OpenTelemetry.Exporter;
using OpenTelemetry.Trace;

namespace GameGuild.API.UnitTests.Core;

public sealed class OpenTelemetryExtensionsTests
{
    [Fact]
    public void AddOpenTelemetryObservability_WhenSectionIsMissing_ReturnsBuilderWithoutProvider()
    {
        var builder = CreateBuilder(new Dictionary<string, string?>());
        builder.Configuration.Sources.Clear();

        var result = builder.AddOpenTelemetryObservability();

        result.Should().BeSameAs(builder);
        builder.Services.Should().NotContain(descriptor => descriptor.ServiceType == typeof(TracerProvider));
    }

    [Fact]
    public void AddOpenTelemetryObservability_WhenDisabled_ReturnsBuilderWithoutProvider()
    {
        var builder = CreateBuilder(new Dictionary<string, string?>
        {
            ["OpenTelemetry:Enabled"] = "false"
        });

        var result = builder.AddOpenTelemetryObservability();

        result.Should().BeSameAs(builder);
        builder.Services.Should().NotContain(descriptor => descriptor.ServiceType == typeof(TracerProvider));
    }

    [Theory]
    [InlineData("", false, null, null)]
    [InlineData("custom-api", true, "http://127.0.0.1:4317", "grpc")]
    public void AddOpenTelemetryObservability_WhenEnabled_RegistersResolvableTracing(
        string serviceName,
        bool consoleExporterEnabled,
        string? otlpEndpoint,
        string? otlpProtocol)
    {
        var builder = CreateBuilder(new Dictionary<string, string?>
        {
            ["OpenTelemetry:Enabled"] = "true",
            ["OpenTelemetry:ServiceName"] = serviceName,
            ["OpenTelemetry:ConsoleExporterEnabled"] = consoleExporterEnabled.ToString(),
            ["OpenTelemetry:OtlpEndpoint"] = otlpEndpoint,
            ["OpenTelemetry:OtlpProtocol"] = otlpProtocol,
            ["OpenTelemetry:IncludeSqlStatements"] = "true"
        });

        builder.AddOpenTelemetryObservability();
        using var provider = builder.Services.BuildServiceProvider();

        provider.GetRequiredService<TracerProvider>().Should().NotBeNull();
    }

    [Theory]
    [InlineData("grpc", OtlpExportProtocol.Grpc)]
    [InlineData("GRPC", OtlpExportProtocol.Grpc)]
    [InlineData("http/protobuf", OtlpExportProtocol.HttpProtobuf)]
    [InlineData(null, OtlpExportProtocol.HttpProtobuf)]
    public void ResolveProtocol_ShouldMapSupportedValues(string? protocol, OtlpExportProtocol expected)
    {
        InvokePrivate<OtlpExportProtocol>("ResolveProtocol", protocol).Should().Be(expected);
    }

    [Theory]
    [InlineData("/health", true)]
    [InlineData("/health/dependencies", true)]
    [InlineData("/live", true)]
    [InlineData("/ready", true)]
    [InlineData("/api/health-report", false)]
    public void IsHealthPath_ShouldRecognizeOperationalEndpoints(string path, bool expected)
    {
        InvokePrivate<bool>("IsHealthPath", new PathString(path)).Should().Be(expected);
    }

    private static WebApplicationBuilder CreateBuilder(IReadOnlyDictionary<string, string?> values)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Testing"
        });
        builder.Configuration.AddInMemoryCollection(values);
        return builder;
    }

    private static T InvokePrivate<T>(string name, object? argument)
    {
        var method = typeof(OpenTelemetryExtensions).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic);
        method.Should().NotBeNull();
        return method!.Invoke(null, [argument]).Should().BeAssignableTo<T>().Subject;
    }
}
