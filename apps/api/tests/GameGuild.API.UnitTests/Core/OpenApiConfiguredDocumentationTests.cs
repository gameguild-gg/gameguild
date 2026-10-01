using System.Globalization;
using System.Text.Json;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using FluentAssertions;
using GameGuild.API.Core.OpenApi;
using GameGuild.API.Setup;
using GameGuild.Configuration.PresentationLayer.OpenAPI;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Writers;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerUI;
using Moq;

namespace GameGuild.API.UnitTests.Core;

public sealed class OpenApiConfiguredDocumentationTests
{
    [Fact]
    public async Task ConfiguredLocale_GeneratesTranslatedDocumentAndPreservesBaseDocument()
    {
        var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(OpenApiExtensions).Assembly.GetName().Name,
            EnvironmentName = "Testing",
            ContentRootPath = AppContext.BaseDirectory,
            Args = []
        });
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["OpenApi:Locales:pt-BR:Title"] = "API GameGuild",
            ["OpenApi:Locales:pt-BR:Description"] = "Documentação para desenvolvedores.",
            ["OpenApi:Locales:pt-BR:Tags:ai"] = "Inteligência artificial",
            ["OpenApi:Locales:pt-BR:Operations:GET /v1/ai/status:Summary"] = "Verificar o status da IA",
            ["OpenApi:Locales:pt-BR:Operations:GET /v1/ai/status:Description"] = "Mostra os provedores configurados.",
            ["OpenApi:Locales:pt-BR:Schemas:Identity_Users_UserDto:Description"] = "Perfil público da conta.",
            ["OpenApi:Locales:pt-BR:Schemas:Identity_Users_UserDto:Properties:id"] = "Identificador da conta."
        });
        builder.Services.SetupControllers(builder.Configuration, null);
        builder.Services.SetupApiVersioning(builder.Configuration, null);
        builder.Services.SetupApiExplorer(builder.Configuration, null);
        builder.Services.SetupOpenApi(builder.Configuration, OpenApiOptions.CreateDefault());

        await using var app = builder.Build();
        var swaggerProvider = app.Services.GetRequiredService<ISwaggerProvider>();
        var baseDocument = swaggerProvider.GetSwagger("v1");
        var localizedDocument = swaggerProvider.GetSwagger("v1.pt-BR");

        localizedDocument.Info.Title.Should().Be("API GameGuild");
        localizedDocument.Info.Description.Should().Be("Documentação para desenvolvedores.");
        localizedDocument.Paths.Keys.Should().Equal(baseDocument.Paths.Keys);
        localizedDocument.Paths["/v1/ai/status"].Operations[OperationType.Get].Summary
            .Should().Be("Verificar o status da IA");
        localizedDocument.Paths["/v1/ai/status"].Operations[OperationType.Get].Description
            .Should().Be("Mostra os provedores configurados.");
        localizedDocument.Tags.Should().Contain(tag =>
            tag.Name == "ai" && tag.Description == "Inteligência artificial");

        var localizedSchema = localizedDocument.Components.Schemas["Identity_Users_UserDto"];
        localizedSchema.Description.Should().Be("Perfil público da conta.");
        localizedSchema.Properties["id"].Description.Should().Be("Identificador da conta.");
        localizedSchema.Properties["email"].Description.Should()
            .Be(baseDocument.Components.Schemas["Identity_Users_UserDto"].Properties["email"].Description);
        baseDocument.Info.Title.Should().Be("GameGuild API");
        baseDocument.Info.Description.Should().NotBe("Documentação para desenvolvedores.");
        baseDocument.Paths["/v1/ai/status"].Operations[OperationType.Get].Summary
            .Should().NotBe("Verificar o status da IA");
        baseDocument.Components.Schemas["Identity_Users_UserDto"].Description
            .Should().NotBe("Perfil público da conta.");
    }

    [Fact]
    public void SetupOpenApi_RejectsUnknownLocaleNames()
    {
        var options = new OpenApiLocalizationOptions
        {
            Locales = new Dictionary<string, OpenApiLocalizedDocumentOptions>
            {
                ["@@@"] = new()
            }
        };

        var act = () => options.Validate();

        act.Should().Throw<ArgumentException>().WithMessage("*@@@*");
    }

    [Fact]
    public void SetupOpenApi_RejectsLocaleAliasesThatNormalizeToTheSameCulture()
    {
        var options = new OpenApiLocalizationOptions
        {
            Locales = new Dictionary<string, OpenApiLocalizedDocumentOptions>(StringComparer.Ordinal)
            {
                ["pt-br"] = new(),
                ["pt-BR"] = new()
            }
        };

        var act = () => options.Validate();

        act.Should().Throw<ArgumentException>().WithMessage("*configured more than once*");
    }

    [Fact]
    public void ConfigureOpenApiDocuments_AddsBaseAndLocalizedVersionEndpoints()
    {
        var provider = new Mock<IApiVersionDescriptionProvider>();
        provider.SetupGet(value => value.ApiVersionDescriptions).Returns(
        [
            new ApiVersionDescription(new ApiVersion(1, 0), "v1", false)
        ]);
        var localizationOptions = new OpenApiLocalizationOptions
        {
            Locales = new Dictionary<string, OpenApiLocalizedDocumentOptions>
            {
                ["pt-BR"] = new()
            }
        };
        var uiOptions = new SwaggerUIOptions();

        PipelineExtensions.ConfigureOpenApiDocuments(uiOptions, provider.Object, "v1", localizationOptions);

        uiOptions.ConfigObject.Urls.Should().Contain(url =>
            url.Url == "/swagger/v1/swagger.json" && url.Name == "GameGuild API V1");
        uiOptions.ConfigObject.Urls.Should().Contain(url =>
            url.Url == "/swagger/v1.pt-BR/swagger.json" && url.Name == "GameGuild API V1 (pt-BR)");

        var fallbackUiOptions = new SwaggerUIOptions();
        PipelineExtensions.ConfigureOpenApiDocuments(fallbackUiOptions, null, "v2", localizationOptions);
        fallbackUiOptions.ConfigObject.Urls.Should().Contain(url =>
            url.Url == "/swagger/v2.pt-BR/swagger.json" && url.Name == "GameGuild API V2 (pt-BR)");
    }

    [Fact]
    public async Task ConfiguredOptions_AppearInGeneratedSwaggerDocument()
    {
        var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(OpenApiExtensions).Assembly.GetName().Name,
            EnvironmentName = "Testing",
            ContentRootPath = AppContext.BaseDirectory,
            Args = []
        });
        builder.Configuration.Sources.Clear();
        builder.Services.SetupControllers(builder.Configuration, null);
        builder.Services.SetupApiVersioning(builder.Configuration, null);
        builder.Services.SetupApiExplorer(builder.Configuration, null);
        builder.Services.SetupOpenApi(builder.Configuration, new OpenApiOptions
        {
            Extensions = new Dictionary<string, string> { ["x-api-audience"] = "\"developer\"" },
            Schemas = new Dictionary<string, OpenApiSchemaDocumentationOptions>
            {
                ["Identity_Users_UserDto"] = new()
                {
                    Description = "Documented account profile.",
                    ExampleJson = "{\"id\":\"00000000-0000-0000-0000-000000000001\",\"email\":\"ada@example.com\",\"name\":\"Ada\",\"createdAt\":\"2026-09-30T12:00:00Z\"}",
                    Properties = new Dictionary<string, OpenApiPropertyDocumentationOptions>
                    {
                        ["id"] = new() { Description = "Account ID." }
                    }
                }
            }
        });

        await using var app = builder.Build();
        var document = app.Services.GetRequiredService<ISwaggerProvider>().GetSwagger("v1");

        document.Paths.Should().NotBeEmpty();
        document.Extensions["x-api-audience"].Should().BeOfType<OpenApiString>()
            .Which.Value.Should().Be("developer");
        var schema = document.Components.Schemas["Identity_Users_UserDto"];
        schema.Description.Should().Be("Documented account profile.");
        schema.Example.Should().BeOfType<OpenApiObject>();
        schema.Properties["id"].Description.Should().Be("Account ID.");
        schema.Properties["email"].Description.Should().Be("User email address");
        document.Components.Schemas["AI_AiChatMessage"].Description
            .Should().Be("Chat message payload for AI requests.");
        document.Paths.Values.SelectMany(path => path.Operations.Values)
            .Count(operation => !string.IsNullOrWhiteSpace(operation.Summary))
            .Should().BeGreaterThan(500);
    }

    [Fact]
    public void SetupOpenApi_RegistersConfiguredDocumentFilter()
    {
        var services = new ServiceCollection();
        services.SetupOpenApi(new ConfigurationBuilder().Build(), new OpenApiOptions());

        using var provider = services.BuildServiceProvider();
        var descriptors = provider.GetRequiredService<IOptions<SwaggerGenOptions>>().Value.DocumentFilterDescriptors;

        descriptors.Should().Contain(descriptor => descriptor.Type == typeof(ConfiguredOpenApiDocumentFilter));
        provider.GetRequiredService<IOptions<SwaggerGenOptions>>().Value.SchemaFilterDescriptors
            .Count(descriptor => descriptor.Type.Name == "XmlCommentsSchemaFilter")
            .Should().Be(1);
    }

    [Fact]
    public void ConfiguredFilter_AppliesStructuredExtensionsAndSchemaExamples()
    {
        var options = new OpenApiOptions
        {
            Extensions = new Dictionary<string, string>
            {
                ["x-api-audience"] = "{\"roles\":[\"developer\",\"operator\"],\"public\":true,\"limit\":12}",
                ["x-fraction"] = "1.5",
                ["x-large"] = "2147483648",
                ["x-message"] = "\"hello\"",
                ["x-null"] = "null"
            },
            Schemas = new Dictionary<string, OpenApiSchemaDocumentationOptions>
            {
                ["ExampleDto"] = new()
                {
                    Description = "An example response.",
                    ExampleJson = "{\"name\":\"Ada\",\"count\":2}",
                    Properties = new Dictionary<string, OpenApiPropertyDocumentationOptions>
                    {
                        ["name"] = new() { Description = "Display name.", ExampleJson = "\"Ada\"" }
                    }
                }
            }
        };
        options.Validate();
        var schema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema> { ["name"] = new() { Type = "string" } }
        };
        var untouched = new OpenApiSchema { Description = "Existing documentation" };
        var document = new OpenApiDocument
        {
            Info = new OpenApiInfo { Title = "Test API", Version = "v1" },
            Paths = new OpenApiPaths(),
            Components = new OpenApiComponents
            {
                Schemas = new Dictionary<string, OpenApiSchema>
                {
                    ["ExampleDto"] = schema,
                    ["UntouchedDto"] = untouched
                }
            }
        };

        new ConfiguredOpenApiDocumentFilter(options).Apply(document, null!);

        var extension = document.Extensions["x-api-audience"].Should().BeOfType<OpenApiObject>().Subject;
        extension["roles"].Should().BeOfType<OpenApiArray>()
            .Which.Cast<OpenApiString>().Select(value => value.Value).Should().Equal("developer", "operator");
        extension["public"].Should().BeOfType<OpenApiBoolean>().Which.Value.Should().BeTrue();
        extension["limit"].Should().BeOfType<OpenApiInteger>().Which.Value.Should().Be(12);
        document.Extensions["x-fraction"].Should().BeOfType<OpenApiDouble>().Which.Value.Should().Be(1.5);
        document.Extensions["x-large"].Should().BeOfType<OpenApiLong>().Which.Value.Should().Be(2_147_483_648);
        document.Extensions["x-message"].Should().BeOfType<OpenApiString>().Which.Value.Should().Be("hello");
        document.Extensions["x-null"].Should().BeOfType<OpenApiNull>();
        schema.Description.Should().Be("An example response.");
        schema.Example.Should().BeOfType<OpenApiObject>();
        schema.Properties["name"].Description.Should().Be("Display name.");
        schema.Properties["name"].Example.Should().BeOfType<OpenApiString>().Which.Value.Should().Be("Ada");
        untouched.Description.Should().Be("Existing documentation");

        using var output = new StringWriter(CultureInfo.InvariantCulture);
        document.SerializeAsV3(new OpenApiJsonWriter(output));
        using var serialized = JsonDocument.Parse(output.ToString());
        serialized.RootElement.GetProperty("openapi").GetString().Should().StartWith("3.0.");
        serialized.RootElement.GetProperty("x-api-audience").GetProperty("roles")[0]
            .GetString().Should().Be("developer");
        serialized.RootElement.GetProperty("components").GetProperty("schemas")
            .GetProperty("ExampleDto").GetProperty("example").GetProperty("name")
            .GetString().Should().Be("Ada");
    }
}
