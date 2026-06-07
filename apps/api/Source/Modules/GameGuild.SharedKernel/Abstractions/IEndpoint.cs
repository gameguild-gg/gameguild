using Microsoft.AspNetCore.Routing;

namespace GameGuild;

/// <summary>
///     Marker interface for minimal-API endpoint definitions.
///     Implementations are discovered via assembly scanning and registered with
///     <see cref="EndpointExtensions.AddEndpoints" />.
/// </summary>
public interface IEndpoint
{
    /// <summary>
    ///     Maps the endpoint routes onto the given <paramref name="app" /> route builder.
    /// </summary>
    void MapEndpoint(IEndpointRouteBuilder app);
}
