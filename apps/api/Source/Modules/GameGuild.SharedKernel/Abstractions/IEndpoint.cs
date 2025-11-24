using Microsoft.AspNetCore.Routing;

namespace GameGuild.Abstractions;

public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}
