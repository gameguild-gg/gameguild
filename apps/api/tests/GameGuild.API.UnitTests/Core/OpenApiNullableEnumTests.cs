using System.Text.Json;
using System.Text.Json.Serialization;
using GameGuild.Configuration.PresentationLayer.OpenAPI;
using GameGuild.Identity.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Writers;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace GameGuild.API.UnitTests.Core;

public sealed class OpenApiNullableEnumTests
{
    private sealed record EnumContract(
        RiskLevel RequiredRisk,
        [property: JsonPropertyName("risk_alias")] RiskLevel? OptionalRisk);

    [Fact]
    public void ConfiguredSchemaGenerator_PreservesNullableSignInRiskLevelOnTheWire()
    {
        var properties = GenerateProperties(typeof(SignInResponse), JsonNamingPolicy.CamelCase);
        var riskLevel = properties.GetProperty("riskLevel");

        Assert.True(riskLevel.TryGetProperty("nullable", out var nullable) && nullable.GetBoolean());
        Assert.Contains("RiskLevel", riskLevel.GetProperty("allOf")[0].GetProperty("$ref").GetString());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NullableEnumProperties_RespectNamesWithoutChangingRequiredEnums(bool snakeCase)
    {
        var properties = GenerateProperties(typeof(EnumContract), snakeCase ? JsonNamingPolicy.SnakeCaseLower : JsonNamingPolicy.CamelCase);
        var optional = properties.GetProperty("risk_alias");
        var required = properties.GetProperty(snakeCase ? "required_risk" : "requiredRisk");

        Assert.True(optional.GetProperty("nullable").GetBoolean());
        Assert.True(optional.TryGetProperty("allOf", out _));
        Assert.True(required.TryGetProperty("$ref", out _));
        Assert.False(required.TryGetProperty("nullable", out var nullable) && nullable.GetBoolean());
    }

    private static JsonElement GenerateProperties(Type contractType, JsonNamingPolicy namingPolicy)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = namingPolicy;
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });
        services.SetupOpenApi(new ConfigurationBuilder().Build(), OpenApiOptions.CreateDefault());
        using var provider = services.BuildServiceProvider();
        var repository = new SchemaRepository();
        var generator = provider.GetRequiredService<ISchemaGenerator>();
        var reference = generator.GenerateSchema(contractType, repository);
        var document = new OpenApiDocument
        {
            Info = new OpenApiInfo { Title = "Nullable contract test", Version = "1" },
            Paths = new OpenApiPaths(),
            Components = new OpenApiComponents { Schemas = repository.Schemas }
        };
        using var output = new StringWriter();
        document.SerializeAsV3(new OpenApiJsonWriter(output));
        using var json = JsonDocument.Parse(output.ToString());
        return json.RootElement.GetProperty("components").GetProperty("schemas")
            .GetProperty(reference.Reference.Id).GetProperty("properties").Clone();
    }
}
