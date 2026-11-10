using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Asp.Versioning;
using GameGuild.API.Setup;
using GameGuild.Configuration.PresentationLayer.OpenAPI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GameGuild.API.UnitTests.Core;

public sealed class OpenApiRequestBodyRequiredHttpTests
{
    [Theory]
    [InlineData(false, "required", true)]
    [InlineData(true, "required", true)]
    [InlineData(false, "allow", false)]
    [InlineData(true, "allow", false)]
    [InlineData(false, "disallow", true)]
    [InlineData(true, "disallow", true)]
    [InlineData(false, "required-attribute", true)]
    [InlineData(true, "required-attribute", true)]
    [InlineData(false, "inferred", true)]
    [InlineData(true, "inferred", true)]
    [InlineData(false, "nullable", false)]
    [InlineData(true, "nullable", false)]
    public async Task Serialized_body_requirement_matches_actual_Mvc_binding_and_validation(
        bool allowEmptyGlobally, string action, bool expectedRequired)
    {
        using var host = await CreateHostAsync(allowEmptyGlobally);
        using var client = host.GetTestClient();
        var route = "/v1/openapi-body-sample/" + action;

        using var swagger = await client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, swagger.StatusCode);
        using var document = JsonDocument.Parse(await swagger.Content.ReadAsStringAsync());
        var body = document.RootElement.GetProperty("paths").GetProperty(route)
            .GetProperty("post").GetProperty("requestBody");
        Assert.Equal(expectedRequired, body.TryGetProperty("required", out var required) && required.GetBoolean());

        using var missing = await client.PostAsync(route, new StringContent("", Encoding.UTF8, "application/json"));
        Assert.Equal(expectedRequired ? HttpStatusCode.BadRequest : HttpStatusCode.OK, missing.StatusCode);
        using var supplied = await client.PostAsJsonAsync(route, new OpenApiBodySampleInput("present"));
        Assert.Equal(HttpStatusCode.OK, supplied.StatusCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Query_only_operations_do_not_acquire_a_request_body(bool allowEmptyGlobally)
    {
        using var host = await CreateHostAsync(allowEmptyGlobally);
        using var client = host.GetTestClient();
        using var swagger = await client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, swagger.StatusCode);
        using var document = JsonDocument.Parse(await swagger.Content.ReadAsStringAsync());
        var operation = document.RootElement.GetProperty("paths").GetProperty("/v1/openapi-body-sample/query")
            .GetProperty("get");
        Assert.False(operation.TryGetProperty("requestBody", out _));
        using var response = await client.GetAsync("/v1/openapi-body-sample/query");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static Task<IHost> CreateHostAsync(bool allowEmptyGlobally)
    {
        var configuration = new ConfigurationBuilder().Build();
        return new HostBuilder().ConfigureWebHost(web =>
        {
            web.UseTestServer();
            web.ConfigureServices(services =>
            {
                services.AddControllers(options => options.AllowEmptyInputInBodyModelBinding = allowEmptyGlobally)
                    .AddApplicationPart(typeof(OpenApiBodySampleController).Assembly)
                    .ConfigureApplicationPartManager(manager => manager.FeatureProviders.Add(new SampleControllerFilter()));
                services.SetupApiVersioning(configuration, null);
                services.SetupApiExplorer(configuration, null);
                services.SetupOpenApi(configuration, OpenApiOptions.CreateDefault());
            });
            web.Configure(app =>
            {
                app.UseSwagger();
                app.UseRouting();
                app.UseEndpoints(endpoints => endpoints.MapControllers());
            });
        }).StartAsync();
    }

    private sealed class SampleControllerFilter : IApplicationFeatureProvider<ControllerFeature>
    {
        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
        {
            ArgumentNullException.ThrowIfNull(parts);
            foreach (var controller in feature.Controllers.Where(type => type.AsType() != typeof(OpenApiBodySampleController)).ToArray())
            {
                feature.Controllers.Remove(controller);
            }
        }
    }
}

public sealed record OpenApiBodySampleInput([Required] string Value);

// This controller is isolated to the test application's explicit feature selection.
[ApiController]
[AllowAnonymous]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/openapi-body-sample")]
public sealed class OpenApiBodySampleController : ControllerBase
{
    [HttpPost("required")]
    public IActionResult RequiredBody([FromBody] OpenApiBodySampleInput body) => Ok(new { body });

    [HttpPost("allow")]
    public IActionResult AllowedBody([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] OpenApiBodySampleInput? body) => Ok(new { body });

    [HttpPost("disallow")]
    public IActionResult DisallowedEmptyBody([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Disallow)] OpenApiBodySampleInput? body) => Ok(new { body });

    [HttpPost("required-attribute")]
    public IActionResult RequiredAttribute([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow), Required] OpenApiBodySampleInput? body) => Ok(new { body });

    [HttpPost("inferred")]
    public IActionResult InferredBody(OpenApiBodySampleInput body) => Ok(new { body });

    [HttpPost("nullable")]
    public IActionResult NullableBody([FromBody] OpenApiBodySampleInput? body = null) => Ok(new { body });

    [HttpGet("query")]
    public IActionResult QueryOnly([FromQuery] string? value) => Ok(new { value });
}
