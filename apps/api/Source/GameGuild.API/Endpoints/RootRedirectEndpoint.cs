
namespace GameGuild.API.Endpoints;

/// <summary>
///     Endpoint that redirects the root path "/" to the API documentation at "/documentation".
///     This provides a convenient entry point for developers accessing the API.
/// </summary>
public sealed class RootRedirectEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/", () => Results.Redirect("/documentation"))
            .ExcludeFromDescription();
    }
}
