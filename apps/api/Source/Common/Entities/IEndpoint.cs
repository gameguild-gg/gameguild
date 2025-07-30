using Microsoft.AspNetCore.Routing;

namespace GameGuild.Common;

public interface IEndpoint {
  void MapEndpoint(IEndpointRouteBuilder app);
}
