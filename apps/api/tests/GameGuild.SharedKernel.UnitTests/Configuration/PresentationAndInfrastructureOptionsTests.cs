using FluentAssertions;
using GameGuild.Configuration.InfrastructureLayer.MemoryCaching;
using GameGuild.Configuration.InfrastructureLayer.RedisCaching;
using GameGuild.Configuration.PresentationLayer;
using GameGuild.Configuration.PresentationLayer.ApiVersioning;
using GameGuild.Configuration.PresentationLayer.Authentication;
using GameGuild.Configuration.PresentationLayer.Authorization;
using GameGuild.Configuration.PresentationLayer.Controllers;
using GameGuild.Configuration.PresentationLayer.CORS;
using GameGuild.Configuration.PresentationLayer.Endpoints;
using GameGuild.Configuration.PresentationLayer.GraphQL;
using GameGuild.Configuration.PresentationLayer.HealthChecks;
using GameGuild.Configuration.PresentationLayer.OpenAPI;
using GameGuild.Configuration.PresentationLayer.RateLimiting;
using GameGuild.Configuration.PresentationLayer.SignalR;


namespace GameGuild.Tests.SharedKernel.Unit.Configuration;

public class PresentationLayerOptionsTests
{
    [Fact]
    public void SectionName_ShouldBePresentationLayer()
    {
        PresentationLayerOptions.SectionName.Should().Be("PresentationLayer");
    }

    [Fact]
    public void Defaults_ShouldEnableCommonFeatures()
    {
        var options = new PresentationLayerOptions();

        options.EnableOpenApi.Should().BeTrue();
        options.EnableApiVersioning.Should().BeTrue();
        options.EnableApiExplorer.Should().BeTrue();
        options.EnableCors.Should().BeTrue();
        options.EnableAuthentication.Should().BeTrue();
        options.EnableAuthorization.Should().BeTrue();
        options.EnableResponseCompression.Should().BeTrue();
        options.EnableProblemDetails.Should().BeTrue();
        options.EnableModelValidation.Should().BeTrue();
        options.EnableHealthChecks.Should().BeTrue();
        options.EnableRequestContext.Should().BeTrue();
        options.EnableResponseCaching.Should().BeTrue();
        options.EnableMemoryCaching.Should().BeTrue();
        options.EnableControllers.Should().BeTrue();
        options.EnableEndpoints.Should().BeTrue();
    }

    [Fact]
    public void Defaults_ShouldDisableOptionalFeatures()
    {
        var options = new PresentationLayerOptions();

        options.EnableRateLimiting.Should().BeFalse();
        options.EnableHttpLogging.Should().BeFalse();
        options.EnableLocalization.Should().BeFalse();
        options.EnableSignalR.Should().BeFalse();
        options.EnableGraphQL.Should().BeFalse();
        options.EnableFeatureFlags.Should().BeFalse();
    }

    [Fact]
    public void Defaults_ShouldHaveNullNestedOptions()
    {
        var options = new PresentationLayerOptions();

        options.Cors.Should().BeNull();
        options.HttpLogging.Should().BeNull();
        options.ProblemDetails.Should().BeNull();
        options.Localization.Should().BeNull();
        options.Authentication.Should().BeNull();
        options.Authorization.Should().BeNull();
        options.GraphQL.Should().BeNull();
        options.SignalR.Should().BeNull();
    }

    [Fact]
    public void CreateDefault_ShouldPopulateConfiguredNestedOptions()
    {
        var options = PresentationLayerOptions.CreateDefault();

        options.Should().NotBeNull();
        options.Cors.Should().NotBeNull();
        options.HttpLogging.Should().NotBeNull();
        options.ProblemDetails.Should().NotBeNull();
        options.Localization.Should().NotBeNull();
        options.MemoryCaching.Should().NotBeNull();
        options.ResponseCaching.Should().NotBeNull();
        options.ResponseCompression.Should().NotBeNull();
        options.Authorization.Should().NotBeNull();
        options.RequestContext.Should().NotBeNull();
        options.RateLimiting.Should().NotBeNull();
        options.ModelValidation.Should().NotBeNull();
        options.FeatureFlags.Should().NotBeNull();
        options.ApiVersioning.Should().NotBeNull();
        options.HealthChecks.Should().NotBeNull();
        options.SignalR.Should().NotBeNull();
        options.GraphQL.Should().NotBeNull();
        options.OpenApi.Should().NotBeNull();
        options.ApiExplorer.Should().NotBeNull();
        options.Controllers.Should().NotBeNull();
        options.Endpoints.Should().NotBeNull();
        options.Authentication.Should().NotBeNull();
    }

    [Fact]
    public void Validate_WithNullNested_ShouldNotThrow()
    {
        var options = new PresentationLayerOptions();
        var act = () => options.Validate();

        act.Should().NotThrow();
    }
}

public class CorsOptionsTests
{
    [Fact]
    public void SectionName_ShouldBeCors()
    {
        CorsOptions.SectionName.Should().Be("Cors");
    }

    [Fact]
    public void Defaults_ShouldHaveEmptyArrays()
    {
        var options = new CorsOptions();

        options.AllowedOrigins.Should().BeEmpty();
        options.AllowedMethods.Should().BeEmpty();
        options.AllowedHeaders.Should().BeEmpty();
    }

    [Fact]
    public void CreateDefault_ShouldReturnInstance()
    {
        CorsOptions.CreateDefault().Should().NotBeNull();
    }

    [Fact]
    public void Validate_WithWildcardOnly_ShouldNotThrow()
    {
        var options = new CorsOptions { AllowedOrigins = ["*"] };
        var act = () => options.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithWildcardAndOthers_ShouldThrow()
    {
        var options = new CorsOptions { AllowedOrigins = ["*", "http://example.com"] };
        var act = () => options.Validate();

        act.Should().Throw<InvalidOperationException>().WithMessage("*wildcard*");
    }

    [Fact]
    public void Validate_WithSpecificOrigins_ShouldNotThrow()
    {
        var options = new CorsOptions { AllowedOrigins = ["http://localhost:3000", "https://example.com"] };
        var act = () => options.Validate();

        act.Should().NotThrow();
    }
}

public class AuthenticationOptionsTests
{
    [Fact]
    public void SectionName_ShouldBeAuthentication()
    {
        AuthenticationOptions.SectionName.Should().Be("Authentication");
    }

    [Fact]
    public void Defaults_ShouldEnableAuthentication()
    {
        var options = new AuthenticationOptions();

        options.EnableAuthentication.Should().BeTrue();
        options.EnableAuthorization.Should().BeTrue();
        options.EnableDacAuthorization.Should().BeTrue();
        options.JwtSecretKey.Should().BeEmpty();
        options.JwtIssuer.Should().BeEmpty();
        options.JwtAudience.Should().BeEmpty();
        options.JwtExpiration.Should().Be(TimeSpan.FromHours(24));
    }

    [Fact]
    public void CreateDefault_ShouldReturnInstance()
    {
        AuthenticationOptions.CreateDefault().Should().NotBeNull();
    }

    [Fact]
    public void Validate_WhenEnabled_WithEmptySecretKey_ShouldThrow()
    {
        var options = new AuthenticationOptions { EnableAuthentication = true };
        var act = () => options.Validate();

        act.Should().Throw<InvalidOperationException>().WithMessage("*secret key*");
    }

    [Fact]
    public void Validate_WhenDisabled_ShouldNotThrow()
    {
        var options = new AuthenticationOptions { EnableAuthentication = false };
        var act = () => options.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WhenEnabled_WithAllConfig_ShouldNotThrow()
    {
        var options = new AuthenticationOptions
        {
            EnableAuthentication = true,
            JwtSecretKey = "supersecretkey",
            JwtIssuer = "TestIssuer",
            JwtAudience = "TestAudience",
            JwtExpiration = TimeSpan.FromHours(1)
        };
        var act = () => options.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ZeroExpiration_ShouldThrow()
    {
        var options = new AuthenticationOptions
        {
            EnableAuthentication = true,
            JwtSecretKey = "key",
            JwtIssuer = "issuer",
            JwtAudience = "audience",
            JwtExpiration = TimeSpan.Zero
        };
        var act = () => options.Validate();

        act.Should().Throw<InvalidOperationException>().WithMessage("*expiration*");
    }
}

public class AuthorizationOptionsTests
{
    [Fact]
    public void SectionName_ShouldBeAuthorization()
    {
        AuthorizationOptions.SectionName.Should().Be("Authorization");
    }

    [Fact]
    public void Defaults_ShouldBeReasonable()
    {
        var options = new AuthorizationOptions();

        options.DefaultPolicy.Should().Be("Default");
        options.RequireAuthenticatedUser.Should().BeTrue();
        options.SystemAccountId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void CreateDefault_ShouldReturnInstance()
    {
        AuthorizationOptions.CreateDefault().Should().NotBeNull();
    }

    [Fact]
    public void Validate_WithEmptyPolicy_ShouldThrow()
    {
        var options = new AuthorizationOptions { DefaultPolicy = "" };
        var act = () => options.Validate();

        act.Should().Throw<InvalidOperationException>().WithMessage("*policy*");
    }

    [Fact]
    public void Validate_WithEmptyGuidSystemAccount_ShouldThrow()
    {
        var options = new AuthorizationOptions { SystemAccountId = Guid.Empty };
        var act = () => options.Validate();

        act.Should().Throw<InvalidOperationException>().WithMessage("*SystemAccountId*");
    }

    [Fact]
    public void Validate_WithDefaults_ShouldNotThrow()
    {
        var options = new AuthorizationOptions();
        var act = () => options.Validate();

        act.Should().NotThrow();
    }
}

public class AuthorizationCacheOptionsTests
{
    [Fact]
    public void CreateDefault_ShouldReturnInstance()
    {
        AuthorizationCacheOptions.CreateDefault().Should().NotBeNull();
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

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Validate_NonPositiveMaxCacheSize_ShouldThrow()
    {
        var options = AuthorizationCacheOptions.CreateDefault();
        options.MaxPolicyCacheSize = 0;
        var act = () => options.Validate();

        act.Should().Throw<InvalidOperationException>();
    }
}

public class TenancyOptionsTests
{
    [Fact]
    public void CreateDefault_ShouldReturnInstance()
    {
        TenancyOptions.CreateDefault().Should().NotBeNull();
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
        options.TenantClaimType.Should().Be("tenant_id");
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
        ApiVersioningOptions.CreateDefault().Should().NotBeNull();
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

public class HealthChecksOptionsTests
{
    [Fact]
    public void Defaults_ShouldHaveEndpoint()
    {
        var options = new HealthChecksOptions();

        options.Endpoints.Should().Contain("/health");
        options.EnableLiveness.Should().BeTrue();
        options.EnableReadiness.Should().BeTrue();
        options.HealthCheckPath.Should().Be("/health");
        options.TimeoutSeconds.Should().Be(30);
    }

    [Fact]
    public void CreateDefault_ShouldReturnInstance()
    {
        HealthChecksOptions.CreateDefault().Should().NotBeNull();
    }

    [Fact]
    public void Validate_WithEndpoints_ShouldNotThrow()
    {
        var options = new HealthChecksOptions();
        var act = () => options.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithEmptyEndpoints_ShouldThrow()
    {
        var options = new HealthChecksOptions { Endpoints = [] };
        var act = () => options.Validate();

        act.Should().Throw<InvalidOperationException>().WithMessage("*endpoint*");
    }

    [Fact]
    public void Validate_WithNullEndpoints_ShouldThrow()
    {
        var options = new HealthChecksOptions { Endpoints = null! };
        var act = () => options.Validate();

        act.Should().Throw<Exception>();
    }
}

public class GraphQLOptionsTests
{
    [Fact]
    public void SectionName_ShouldBeGraphQL()
    {
        GraphQLOptions.SectionName.Should().Be("GraphQL");
    }

    [Fact]
    public void Defaults_ShouldBeDisabled()
    {
        var options = new GraphQLOptions();

        options.EnableGraphQL.Should().BeFalse();
        options.Endpoint.Should().Be("/graphql");
    }

    [Fact]
    public void CreateDefault_ShouldReturnInstance()
    {
        GraphQLOptions.CreateDefault().Should().NotBeNull();
    }

    [Fact]
    public void Validate_WithEndpoint_ShouldNotThrow()
    {
        var options = new GraphQLOptions();
        var act = () => options.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithEmptyEndpoint_ShouldThrow()
    {
        var options = new GraphQLOptions { Endpoint = "" };
        var act = () => options.Validate();

        act.Should().Throw<InvalidOperationException>();
    }
}

public class OpenApiOptionsTests
{
    [Fact]
    public void SectionName_ShouldBeOpenApi()
    {
        OpenApiOptions.SectionName.Should().Be("OpenApi");
    }

    [Fact]
    public void Defaults_ShouldBeReasonable()
    {
        var options = new OpenApiOptions();

        options.EnableOpenApi.Should().BeTrue();
        options.Title.Should().Be("GameGuild API");
        options.Version.Should().Be("v1");
        options.Description.Should().BeEmpty();
        options.ContactName.Should().BeEmpty();
        options.ContactEmail.Should().BeEmpty();
        options.ContactUrl.Should().BeEmpty();
        options.TermsOfServiceUrl.Should().BeEmpty();
        options.LicenseName.Should().BeEmpty();
        options.LicenseUrl.Should().BeEmpty();
    }

    [Fact]
    public void CreateDefault_ShouldReturnInstance()
    {
        OpenApiOptions.CreateDefault().Should().NotBeNull();
    }

    [Fact]
    public void AllPropertiesShouldBeSettable()
    {
        var options = new OpenApiOptions
        {
            Title = "Test",
            Version = "v2",
            Description = "desc",
            ContactName = "Name",
            ContactEmail = "email@test.com",
            ContactUrl = "https://test.com",
            TermsOfServiceUrl = "https://tos.com",
            LicenseName = "MIT",
            LicenseUrl = "https://license.com"
        };

        options.Title.Should().Be("Test");
        options.LicenseName.Should().Be("MIT");
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

        act.Should().Throw<ArgumentException>();
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

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Validate_WithDefault_ShouldNotThrow()
    {
        var options = new OpenApiServerVariableOptions { Default = "v1" };
        var act = () => options.Validate();

        act.Should().NotThrow();
    }
}

public class RateLimitingOptionsTests
{
    [Fact]
    public void SectionName_ShouldBeRateLimiting()
    {
        RateLimitingOptions.SectionName.Should().Be("RateLimiting");
    }

    [Fact]
    public void Defaults_ShouldBeDisabled()
    {
        var options = new RateLimitingOptions();

        options.EnableRateLimiting.Should().BeFalse();
    }

    [Fact]
    public void Defaults_ShouldHaveReasonableValues()
    {
        var options = new RateLimitingOptions();

        options.Limit.Should().Be(100);
        options.Period.Should().Be(TimeSpan.FromMinutes(1));
        options.RequestsPerMinute.Should().Be(60);
        options.BurstSize.Should().Be(10);
        options.ExemptPaths.Should().BeEmpty();
        options.AuthenticationRequestsPerMinute.Should().Be(10);
        options.AuthorizationRequestsPerMinute.Should().Be(100);
        options.ApiRequestsPerMinute.Should().Be(60);
        options.QueueLimit.Should().Be(2);
        options.TenantRequestsPerMinute.Should().Be(1000);
        options.UserRequestsPerMinute.Should().Be(300);
        options.StandardApiKeyRequestsPerMinute.Should().Be(100);
        options.PremiumApiKeyRequestsPerMinute.Should().Be(1000);
        options.TokenBucketLimit.Should().Be(100);
        options.TokenReplenishmentPeriod.Should().Be(TimeSpan.FromSeconds(10));
        options.TokensPerPeriod.Should().Be(20);
        options.MaxConcurrentRequests.Should().Be(10);
    }

    [Fact]
    public void CreateDefault_ShouldReturnInstance()
    {
        RateLimitingOptions.CreateDefault().Should().NotBeNull();
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

        act.Should().Throw<InvalidOperationException>();
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

        act.Should().Throw<ArgumentException>();
    }
}
