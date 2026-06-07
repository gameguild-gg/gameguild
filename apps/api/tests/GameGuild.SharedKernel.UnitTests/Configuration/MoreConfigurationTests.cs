using FluentAssertions;
using GameGuild.Configuration.PresentationLayer.Authorization;
using GameGuild.Configuration.PresentationLayer.ApiVersioning;
using GameGuild.Configuration.PresentationLayer.SignalR;
using GameGuild.Configuration.PresentationLayer.Controllers;
using GameGuild.Configuration.PresentationLayer.Endpoints;
using GameGuild.Configuration.PresentationLayer.HealthChecks;
using GameGuild.Configuration.PresentationLayer.OpenAPI;
using GameGuild.Configuration.InfrastructureLayer.MemoryCaching;
using GameGuild.Configuration.InfrastructureLayer.RedisCaching;

namespace GameGuild.SharedKernel.UnitTests.Configuration;

public class AuthorizationCacheOptionsTests
{
    [Fact]
    public void CreateDefault_ShouldReturnInstance()
    {
        var options = AuthorizationCacheOptions.CreateDefault();
        options.Should().NotBeNull();
    }

    [Fact]
    public void Validate_WithDefaults_ShouldNotThrow()
    {
        var options = AuthorizationCacheOptions.CreateDefault();
        var act = () => options.Validate();
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_NegativePolicyTtl_ShouldThrow()
    {
        var options = AuthorizationCacheOptions.CreateDefault();
        options.PolicyTtlSeconds = -1;
        var act = () => options.Validate();
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Validate_NonPositiveMaxCacheSize_ShouldThrow()
    {
        var options = AuthorizationCacheOptions.CreateDefault();
        options.MaxPolicyCacheSize = 0;
        var act = () => options.Validate();
        act.Should().Throw<Exception>();
    }
}

public class TenancyOptionsTests
{
    [Fact]
    public void CreateDefault_ShouldReturnInstance()
    {
        var options = TenancyOptions.CreateDefault();
        options.Should().NotBeNull();
    }

    [Fact]
    public void Validate_WithDefaults_ShouldNotThrow()
    {
        var options = TenancyOptions.CreateDefault();
        var act = () => options.Validate();
        act.Should().NotThrow();
    }

    [Fact]
    public void Defaults_ShouldHaveResolutionAndFallback()
    {
        var options = TenancyOptions.CreateDefault();
        options.Resolution.Should().NotBeNull();
        options.Fallback.Should().NotBeNull();
    }
}

public class AuthorizationTokenOptionsTests
{
    [Fact]
    public void CreateDefault_ShouldReturnInstance()
    {
        var options = AuthorizationTokenOptions.CreateDefault();
        options.Should().NotBeNull();
    }

    [Fact]
    public void Validate_WithDefaults_ShouldNotThrow()
    {
        var options = AuthorizationTokenOptions.CreateDefault();
        var act = () => options.Validate();
        act.Should().NotThrow();
    }
}

public class ApiVersioningOptionsTests
{
    [Fact]
    public void Defaults_ShouldBeReasonable()
    {
        var options = new ApiVersioningOptions();
        options.DefaultVersion.Should().NotBeNullOrEmpty();
        options.AssumeDefaultVersionWhenUnspecified.Should().BeTrue();
        options.QueryParameterName.Should().NotBeEmpty();
    }

    [Fact]
    public void CreateDefault_ShouldReturnInstance()
    {
        var options = ApiVersioningOptions.CreateDefault();
        options.Should().NotBeNull();
    }

    [Fact]
    public void Validate_WithDefaults_ShouldNotThrow()
    {
        var options = ApiVersioningOptions.CreateDefault();
        var act = () => options.Validate();
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_NullDefaultVersion_ShouldThrow()
    {
        var options = new ApiVersioningOptions { DefaultVersion = null! };
        var act = () => options.Validate();
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Validate_EmptyQueryParameterName_ShouldThrow()
    {
        var options = new ApiVersioningOptions { QueryParameterName = "" };
        var act = () => options.Validate();
        act.Should().Throw<ArgumentException>();
    }
}

public class SignalROptionsTests
{
    [Fact]
    public void Defaults_ShouldBeReasonable()
    {
        var options = new SignalROptions();
        options.HubPath.Should().NotBeEmpty();
        options.KeepAliveInterval.Should().BeGreaterThan(TimeSpan.Zero);
        options.ClientTimeoutInterval.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public void CreateDefault_ShouldReturnInstance()
    {
        SignalROptions.CreateDefault().Should().NotBeNull();
    }
}

public class ControllersOptionsTests
{
    [Fact]
    public void Defaults_ShouldEnableKebabCase()
    {
        var options = new ControllersOptions();
        options.UseKebabCaseRoutes.Should().BeTrue();
    }

    [Fact]
    public void CreateDefault_ShouldReturnInstance()
    {
        ControllersOptions.CreateDefault().Should().NotBeNull();
    }
}

public class EndpointsOptionsTests
{
    [Fact]
    public void Defaults_ShouldBeReasonable()
    {
        var options = new EndpointsOptions();
        options.RegisterFromMainAssembly.Should().BeTrue();
    }

    [Fact]
    public void CreateDefault_ShouldReturnInstance()
    {
        EndpointsOptions.CreateDefault().Should().NotBeNull();
    }
}

public class HealthCheckOptionsTests
{
    [Fact]
    public void Constructor_ShouldPopulateTags()
    {
        var options = new HealthCheckOptions();
        options.Tags.Should().NotBeEmpty();
    }

    [Fact]
    public void Defaults_ShouldEnable()
    {
        var options = new HealthCheckOptions();
        options.EnableDatabaseCheck.Should().BeTrue();
        options.EnableApiHealthCheck.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithValidTimeout_ShouldNotThrow()
    {
        var options = new HealthCheckOptions();
        var act = () => options.Validate();
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithZeroTimeout_ShouldThrow()
    {
        var options = new HealthCheckOptions { Timeout = TimeSpan.Zero };
        var act = () => options.Validate();
        act.Should().Throw<Exception>();
    }
}

public class OpenApiServerOptionsTests
{
    [Fact]
    public void Defaults_ShouldHaveEmptyValues()
    {
        var options = new OpenApiServerOptions();
        options.Url.Should().BeEmpty();
        options.Description.Should().BeEmpty();
        options.Variables.Should().BeEmpty();
    }

    [Fact]
    public void Validate_EmptyUrl_ShouldThrow()
    {
        var options = new OpenApiServerOptions { Url = "" };
        var act = () => options.Validate();
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Validate_WithUrl_ShouldNotThrow()
    {
        var options = new OpenApiServerOptions { Url = "https://api.example.com" };
        var act = () => options.Validate();
        act.Should().NotThrow();
    }
}

public class OpenApiServerVariableOptionsTests
{
    [Fact]
    public void Defaults_ShouldHaveEmptyDefault()
    {
        var options = new OpenApiServerVariableOptions();
        options.Default.Should().BeEmpty();
    }

    [Fact]
    public void Validate_EmptyDefault_ShouldThrow()
    {
        var options = new OpenApiServerVariableOptions { Default = "" };
        var act = () => options.Validate();
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Validate_WithDefault_ShouldNotThrow()
    {
        var options = new OpenApiServerVariableOptions { Default = "v1" };
        var act = () => options.Validate();
        act.Should().NotThrow();
    }
}

public class MemoryCachingOptionsTests
{
    [Fact]
    public void Defaults_ShouldBeReasonable()
    {
        var options = new MemoryCachingOptions();
        options.SizeLimit.Should().BeGreaterThan(0);
    }

    [Fact]
    public void CreateDefault_ShouldReturnInstance()
    {
        MemoryCachingOptions.CreateDefault().Should().NotBeNull();
    }

    [Fact]
    public void Validate_WithDefaults_ShouldNotThrow()
    {
        var options = MemoryCachingOptions.CreateDefault();
        var act = () => options.Validate();
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_NegativeSize_ShouldThrow()
    {
        var options = new MemoryCachingOptions { SizeLimit = -1 };
        var act = () => options.Validate();
        act.Should().Throw<Exception>();
    }
}

public class RedisCachingOptionsTests
{
    [Fact]
    public void Defaults_ShouldBeReasonable()
    {
        var options = new RedisCachingOptions();
        options.DefaultExpirationMinutes.Should().BeGreaterThan(0);
        options.InstanceName.Should().NotBeEmpty();
    }

    [Fact]
    public void CreateDefault_ShouldReturnInstance()
    {
        RedisCachingOptions.CreateDefault().Should().NotBeNull();
    }

    [Fact]
    public void Validate_WithDefaults_ShouldNotThrow()
    {
        var options = RedisCachingOptions.CreateDefault();
        var act = () => options.Validate();
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_EmptyConnectionString_ShouldThrow()
    {
        var options = new RedisCachingOptions { Enabled = true, ConnectionString = "" };
        var act = () => options.Validate();
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Validate_NegativeExpiration_ShouldThrow()
    {
        var options = RedisCachingOptions.CreateDefault();
        options.DefaultExpirationMinutes = -1;
        var act = () => options.Validate();
        act.Should().Throw<Exception>();
    }
}
