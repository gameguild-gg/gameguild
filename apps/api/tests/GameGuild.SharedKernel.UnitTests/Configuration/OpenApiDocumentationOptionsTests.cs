using FluentAssertions;
using GameGuild.Configuration.PresentationLayer.OpenAPI;
using Microsoft.Extensions.Configuration;

namespace GameGuild.SharedKernel.UnitTests.Configuration;

public sealed class OpenApiDocumentationOptionsTests
{
    [Fact]
    public void Build_BindsAndValidatesDocumentExtensionsAndSchemaDocumentation()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OpenApi:Extensions:x-api-audience"] = "{\"roles\":[\"developer\"],\"public\":true}",
                ["OpenApi:Schemas:ExampleDto:Description"] = "An example response.",
                ["OpenApi:Schemas:ExampleDto:ExampleJson"] = "{\"name\":\"Ada\"}",
                ["OpenApi:Schemas:ExampleDto:Properties:name:Description"] = "Display name.",
                ["OpenApi:Schemas:ExampleDto:Properties:name:ExampleJson"] = "\"Ada\""
            })
            .Build();

        var options = OpenApiOptionsBuilder.Build(configuration);

        options.Extensions.Should().ContainKey("x-api-audience");
        options.Schemas["ExampleDto"].Description.Should().Be("An example response.");
        options.Schemas["ExampleDto"].Properties["name"].ExampleJson.Should().Be("\"Ada\"");
    }

    [Theory]
    [InlineData("audience", "true")]
    [InlineData("x-", "true")]
    [InlineData("X-upper", "true")]
    [InlineData("x-bad:name", "true")]
    [InlineData("x-valid", "{invalid")]
    [InlineData("x-valid", "1e1000")]
    public void Validate_RejectsInvalidExtensionNamesAndValues(string name, string json)
    {
        var options = new OpenApiOptions { Extensions = new Dictionary<string, string> { [name] = json } };

        var act = options.Validate;

        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData("{invalid")]
    [InlineData("[1,")]
    public void Validate_RejectsMalformedSchemaExamples(string json)
    {
        var options = new OpenApiOptions
        {
            Schemas = new Dictionary<string, OpenApiSchemaDocumentationOptions>
            {
                ["ExampleDto"] = new() { ExampleJson = json }
            }
        };

        var act = options.Validate;

        act.Should().Throw<InvalidOperationException>().WithMessage("*schema 'ExampleDto' example*");
    }

    [Fact]
    public void Validate_RejectsMalformedPropertyExample()
    {
        var options = new OpenApiOptions
        {
            Schemas = new Dictionary<string, OpenApiSchemaDocumentationOptions>
            {
                ["ExampleDto"] = new()
                {
                    Properties = new Dictionary<string, OpenApiPropertyDocumentationOptions>
                    {
                        ["name"] = new() { ExampleJson = "{bad" }
                    }
                }
            }
        };

        var act = options.Validate;

        act.Should().Throw<InvalidOperationException>().WithMessage("*property 'name' example*");
    }
}
