using System.Reflection;
using FluentAssertions;
using GameGuild.API.Setup;
using GameGuild.Configuration.PresentationLayer.OpenAPI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Moq;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace GameGuild.API.UnitTests.Core;

public sealed class OpenApiSecurityUiTests
{
    private sealed class SecuredController
    {
        [Authorize(AuthenticationSchemes = "ApiKey")]
        public void WithApiKey() { }

        [Authorize(AuthenticationSchemes = "ApiKey,Bearer")]
        public void WithApiKeyOrBearer() { }

        [Authorize(AuthenticationSchemes = "ApiKey")]
        [AllowAnonymous]
        public void Anonymous() { }
    }

    [Fact]
    public void SetupOpenApi_RegistersConfiguredSecurityDefinitions()
    {
        var options = new OpenApiOptions
        {
            SecuritySchemes = new Dictionary<string, OpenApiSecuritySchemeOptions>
            {
                ["ApiKey"] = new() { Kind = OpenApiSecuritySchemeKind.ApiKeyHeader, HeaderName = "X-API-Key", AuthenticationScheme = "ApiKey" },
                ["OAuth2"] = new()
                {
                    Kind = OpenApiSecuritySchemeKind.OAuth2AuthorizationCode,
                    AuthorizationUrl = "https://identity.example.com/authorize",
                    TokenUrl = "https://identity.example.com/token",
                    Scopes = new Dictionary<string, string> { ["api.read"] = "Read API data" }
                },
                ["Basic"] = new() { Kind = OpenApiSecuritySchemeKind.HttpBasic },
                ["HttpBearer"] = new() { Kind = OpenApiSecuritySchemeKind.HttpBearer }
            }
        };
        var services = new ServiceCollection();
        services.SetupOpenApi(new ConfigurationBuilder().Build(), options);

        using var provider = services.BuildServiceProvider();
        var schemes = provider.GetRequiredService<IOptions<SwaggerGenOptions>>()
            .Value.SwaggerGeneratorOptions.SecuritySchemes;

        schemes.Should().ContainKey("Bearer");
        schemes["ApiKey"].Name.Should().Be("X-API-Key");
        schemes["ApiKey"].Type.Should().Be(SecuritySchemeType.ApiKey);
        schemes["OAuth2"].Flows.AuthorizationCode.Scopes.Should().ContainKey("api.read");
        schemes["Basic"].Scheme.Should().Be("basic");
        schemes["HttpBearer"].Scheme.Should().Be("bearer");
    }

    [Fact]
    public void ConfiguredSecurityFilter_UsesExplicitAuthenticationScheme()
    {
        var options = new OpenApiOptions
        {
            SecuritySchemes = new Dictionary<string, OpenApiSecuritySchemeOptions>
            {
                ["ApiKey"] = new() { AuthenticationScheme = "ApiKey" }
            }
        };
        var method = typeof(SecuredController).GetMethod(nameof(SecuredController.WithApiKey))!;
        var context = new OperationFilterContext(new ApiDescription
        {
            ActionDescriptor = new ControllerActionDescriptor
            {
                MethodInfo = method,
                ControllerTypeInfo = typeof(SecuredController).GetTypeInfo()
            }
        }, Mock.Of<ISchemaGenerator>(), new SchemaRepository(), method);
        var operation = new OpenApiOperation();

        new ConfiguredSecurityOperationFilter(options).Apply(operation, context);

        operation.Security.Should().ContainSingle();
        operation.Security[0].Keys.Single().Reference.Id.Should().Be("ApiKey");
    }

    [Fact]
    public void AnonymousFilter_ClearsConfiguredSecurityRequirement()
    {
        var options = new OpenApiOptions
        {
            SecuritySchemes = new Dictionary<string, OpenApiSecuritySchemeOptions>
            {
                ["ApiKey"] = new() { AuthenticationScheme = "ApiKey" }
            }
        };
        var method = typeof(SecuredController).GetMethod(nameof(SecuredController.Anonymous))!;
        var context = new OperationFilterContext(new ApiDescription
        {
            ActionDescriptor = new ControllerActionDescriptor
            {
                MethodInfo = method,
                ControllerTypeInfo = typeof(SecuredController).GetTypeInfo()
            }
        }, Mock.Of<ISchemaGenerator>(), new SchemaRepository(), method);
        var operation = new OpenApiOperation();

        new ConfiguredSecurityOperationFilter(options).Apply(operation, context);
        new AllowAnonymousOperationFilter().Apply(operation, context);

        operation.Security.Should().BeEmpty();
    }

    [Fact]
    public void ConfiguredSecurityFilter_PreservesBearerAsAnAlternative()
    {
        var options = new OpenApiOptions
        {
            SecuritySchemes = new Dictionary<string, OpenApiSecuritySchemeOptions>
            {
                ["ApiKey"] = new() { AuthenticationScheme = "ApiKey" }
            }
        };
        var method = typeof(SecuredController).GetMethod(nameof(SecuredController.WithApiKeyOrBearer))!;
        var context = new OperationFilterContext(new ApiDescription(), Mock.Of<ISchemaGenerator>(), new SchemaRepository(), method);
        var operation = new OpenApiOperation();

        new ConfiguredSecurityOperationFilter(options).Apply(operation, context);

        operation.Security.Select(requirement => requirement.Keys.Single().Reference.Id)
            .Should().BeEquivalentTo("ApiKey", "Bearer");
    }

    [Fact]
    public void UiOptions_ConfigureAppearanceAndBehavior()
    {
        var configured = new OpenApiUiOptions
        {
            RoutePrefix = "developer/docs",
            DocumentTitle = "GameGuild developer API",
            EnableDeepLinking = true,
            EnableFilter = true,
            DisplayRequestDuration = true,
            PersistAuthorization = false
        };
        var target = new SwaggerUIOptions();

        PipelineExtensions.ConfigureOpenApiUi(target, configured);

        target.RoutePrefix.Should().Be("developer/docs");
        target.DocumentTitle.Should().Be("GameGuild developer API");
        target.ConfigObject.DeepLinking.Should().BeTrue();
        target.ConfigObject.Filter.Should().BeEmpty();
        target.ConfigObject.DisplayRequestDuration.Should().BeTrue();
        target.ConfigObject.PersistAuthorization.Should().BeFalse();
    }
}
