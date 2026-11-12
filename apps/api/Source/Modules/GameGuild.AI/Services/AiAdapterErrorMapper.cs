using GameGuild;
using System.Net;

namespace GameGuild.AI;

internal static class AiAdapterErrorMapper
{
    public static Error Map(string adapterName, HttpStatusCode statusCode)
    {
        var description = $"{adapterName} returned {(int)statusCode}.";

        return statusCode switch
        {
            HttpStatusCode.Unauthorized => Error.Unauthorized("AI.ProviderUnauthorized", $"{adapterName} credentials were rejected."),
            HttpStatusCode.Forbidden => Error.Forbidden("AI.ProviderForbidden", $"{adapterName} rejected the request."),
            (HttpStatusCode)429 => Error.Conflict("AI.ProviderRateLimited", $"{adapterName} rate limit exceeded."),
            HttpStatusCode.BadRequest => Error.Validation("AI.ProviderBadRequest", description),
            HttpStatusCode.NotFound => Error.Problem("AI.ProviderEndpointNotFound", description),
            _ when (int)statusCode >= 500 => Error.Failure("AI.ProviderFailure", description),
            _ => Error.Problem("AI.ProviderRequestFailed", description)
        };
    }
}
