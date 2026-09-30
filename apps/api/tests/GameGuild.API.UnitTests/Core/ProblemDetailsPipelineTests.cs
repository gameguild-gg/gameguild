using System.Text.Json;
using FluentAssertions;
using GameGuild.API;
using GameGuild.API.Setup;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProblemDetailsLocalizedText = GameGuild.Configuration.PresentationLayer.ProblemDetails.ProblemDetailsLocalizedText;
using ProblemDetailsOptions = GameGuild.Configuration.PresentationLayer.ProblemDetails.ProblemDetailsOptions;

namespace GameGuild.API.UnitTests.Core;

public sealed class ProblemDetailsPipelineTests
{
    [Fact]
    public async Task UseProblemDetailsStatusCodePages_ShouldFormatEmptyMinimalApiErrors()
    {
        var options = ProblemDetailsOptions.CreateDefault();
        options.LocalizedMessages["pt-BR"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["default"] = new ProblemDetailsLocalizedText
            {
                Title = "Ocorreu um erro.",
                Detail = "A solicitação não foi encontrada.",
            },
        };
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLocalization();
        services.Configure<RequestLocalizationOptions>(localization =>
        {
            localization.SetDefaultCulture("en-US")
                .AddSupportedCultures("en-US", "pt-BR")
                .AddSupportedUICultures("en-US", "pt-BR");
        });
        services.SetupProblemDetails(new ConfigurationBuilder().Build(), options);
        using var provider = services.BuildServiceProvider();
        var app = new ApplicationBuilder(provider);
        app.UseRequestLocalization();
        app.UseProblemDetailsStatusCodePages();
        app.Run(context =>
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return Task.CompletedTask;
        });
        var pipeline = app.Build();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = provider,
            TraceIdentifier = "trace-minimal-404",
        };
        httpContext.Request.Headers.AcceptLanguage = "pt-BR";
        httpContext.Response.Body = new MemoryStream();

        await pipeline(httpContext);

        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        httpContext.Response.ContentType.Should().StartWith("application/problem+json");
        httpContext.Response.Body.Position = 0;
        using var json = await JsonDocument.ParseAsync(httpContext.Response.Body);
        json.RootElement.GetProperty("status").GetInt32().Should().Be(StatusCodes.Status404NotFound);
        json.RootElement.GetProperty("traceId").GetString().Should().Be("trace-minimal-404");
        json.RootElement.GetProperty("title").GetString().Should().Be("Ocorreu um erro.");
        json.RootElement.GetProperty("detail").GetString().Should().Be("A solicitação não foi encontrada.");
    }
}
