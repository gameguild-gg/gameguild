using GameGuild;
using System.Net;

namespace GameGuild.AI;

internal static class AiProviderErrorMapper
{
    public static Error Map(string providerName, HttpStatusCode statusCode, string responseBody)
    {
        var description = string.IsNullOrWhiteSpace(responseBody)
            ? $"{providerName} returned {(int)statusCode}."
            : $"{providerName} returned {(int)statusCode}: {Truncate(responseBody)}";

        return statusCode switch
        {
            HttpStatusCode.Unauthorized => Error.Unauthorized("AI.ProviderUnauthorized", $"{providerName} credentials were rejected."),
            HttpStatusCode.Forbidden => Error.Forbidden("AI.ProviderForbidden", $"{providerName} rejected the request."),
            (HttpStatusCode)429 => Error.Conflict("AI.ProviderRateLimited", $"{providerName} rate limit exceeded."),
            HttpStatusCode.BadRequest => Error.Validation("AI.ProviderBadRequest", description),
            HttpStatusCode.NotFound => Error.Problem("AI.ProviderEndpointNotFound", description),
            _ when (int)statusCode >= 500 => Error.Failure("AI.ProviderFailure", description),
            _ => Error.Problem("AI.ProviderRequestFailed", description)
        };
    }

    private static string Truncate(string value)
    {
        const int maxLength = 400;
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}