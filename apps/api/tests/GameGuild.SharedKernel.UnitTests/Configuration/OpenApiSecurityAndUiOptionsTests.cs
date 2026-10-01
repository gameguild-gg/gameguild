using FluentAssertions;
using GameGuild.Configuration.PresentationLayer.OpenAPI;
using Microsoft.Extensions.Configuration;

namespace GameGuild.SharedKernel.UnitTests.Configuration;

public sealed class OpenApiSecurityAndUiOptionsTests
{
    [Fact]
    public void Configuration_BindsAdditionalSchemesAndUiSettings()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["OpenApi:SecuritySchemes:ApiKey:Kind"] = "ApiKeyHeader",
            ["OpenApi:SecuritySchemes:ApiKey:HeaderName"] = "X-API-Key",
            ["OpenApi:SecuritySchemes:ApiKey:AuthenticationScheme"] = "ApiKey",
            ["OpenApi:Ui:RoutePrefix"] = "developer/docs",
            ["OpenApi:Ui:EnableFilter"] = "true"
        }).Build();

        var options = OpenApiOptionsBuilder.Build(configuration);

        options.SecuritySchemes["ApiKey"].AuthenticationScheme.Should().Be("ApiKey");
        options.Ui.RoutePrefix.Should().Be("developer/docs");
        options.Ui.EnableFilter.Should().BeTrue();
    }

    [Fact]
    public void Validate_RejectsCollisionWithDefaultBearer()
    {
        var options = new OpenApiOptions
        {
            SecuritySchemes = new Dictionary<string, OpenApiSecuritySchemeOptions>
            {
                ["bearer"] = new() { Kind = OpenApiSecuritySchemeKind.HttpBearer }
            }
        };

        var action = () => options.Validate();

        action.Should().Throw<InvalidOperationException>().WithMessage("*Bearer*");
    }

    [Theory]
    [InlineData("javascript:alert(1)", "https://identity.example.com/token")]
    [InlineData("https://identity.example.com/authorize", "/token")]
    public void Validate_RejectsInvalidOAuth2Urls(string authorizationUrl, string tokenUrl)
    {
        var options = new OpenApiOptions
        {
            SecuritySchemes = new Dictionary<string, OpenApiSecuritySchemeOptions>
            {
                ["OAuth2"] = new()
                {
                    Kind = OpenApiSecuritySchemeKind.OAuth2AuthorizationCode,
                    AuthorizationUrl = authorizationUrl,
                    TokenUrl = tokenUrl
                }
            }
        };

        var action = () => options.Validate();

        action.Should().Throw<InvalidOperationException>().WithMessage("*OAuth2*");
    }

    [Fact]
    public void Validate_RejectsInvalidApiKeyHeaderAndUiRoute()
    {
        var options = new OpenApiOptions
        {
            SecuritySchemes = new Dictionary<string, OpenApiSecuritySchemeOptions>
            {
                ["ApiKey"] = new() { HeaderName = "X API Key" }
            }
        };
        var action = () => options.Validate();
        action.Should().Throw<InvalidOperationException>().WithMessage("*header name*");

        options.SecuritySchemes["ApiKey"].HeaderName = "X-API-Key";
        options.Ui.RoutePrefix = "../documentation";
        action.Should().Throw<InvalidOperationException>().WithMessage("*route prefix*");
    }
}
