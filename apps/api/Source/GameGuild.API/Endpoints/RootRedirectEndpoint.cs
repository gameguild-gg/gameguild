using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;

namespace GameGuild.API.Endpoints;

/// <summary>
///     Endpoint for the root path "/". Returns API metadata or redirects to documentation in dev/staging.
/// </summary>
public sealed class RootRedirectEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/", (IHostEnvironment env) => env.IsDevelopment() || env.IsStaging()
            ? Results.Redirect("/documentation")
            : Results.Ok(new { name = "GameGuild API", status = "Healthy", environment = env.EnvironmentName }))
            .ExcludeFromDescription();
    }
}
