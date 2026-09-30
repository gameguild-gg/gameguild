using System.Globalization;
using System.Text.Json;
using FluentAssertions;
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

namespace GameGuild.API.UnitTests.Core;

public sealed class OpenApiConfiguredDocumentationTests
{
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
    }

    [Fact]
    public void SetupOpenApi_RegistersConfiguredDocumentFilter()
    {
        var services = new ServiceCollection();
        services.SetupOpenApi(new ConfigurationBuilder().Build(), new OpenApiOptions());

        using var provider = services.BuildServiceProvider();
        var descriptors = provider.GetRequiredService<IOptions<SwaggerGenOptions>>().Value.DocumentFilterDescriptors;

        descriptors.Should().Contain(descriptor => descriptor.Type == typeof(ConfiguredOpenApiDocumentFilter));
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
