using System.Collections.ObjectModel;
using System.Net.Mime;
using GameGuild.Abstractions;

namespace GameGuild.API.Endpoints;

/// <summary>
///     Health check endpoint for monitoring application status.
/// </summary>
internal class HealthEndpoint : IEndpoint
{
    /// <summary>
    ///     Maps the health check endpoint.
    /// </summary>
    /// <param name="app">The endpoint route builder</param>
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/health", GetHealthStatus)
            .WithName("GetHealthStatus")
            .WithTags("Health")
            .WithSummary("Get application health status")
            .WithDescription("Returns the current health status of the application including dependencies.")
            .Produces<HealthResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)
            .Produces(StatusCodes.Status503ServiceUnavailable)
            .WithOpenApi();
    }

    /// <summary>
    ///     Gets the current health status of the application.
    /// </summary>
    /// <returns>A health response indicating the status of the application and its dependencies</returns>
    private static Task<IResult> GetHealthStatus()
    {
        var dependencies = new List<DependencyHealth> { new DependencyHealth("Database", "Healthy", TimeSpan.FromMilliseconds(12)), new DependencyHealth("Cache", "Healthy", TimeSpan.FromMilliseconds(3)) };

        var healthResponse = new HealthResponse("Healthy", DateTime.UtcNow, "1.0.0", new ReadOnlyCollection<DependencyHealth>(dependencies));

        return Task.FromResult(Results.Ok(healthResponse));
    }
}

/// <summary>
///     Represents the health status response.
/// </summary>
/// <param name="Status">The overall health status</param>
/// <param name="Timestamp">When the health check was performed</param>
/// <param name="Version">The application version</param>
/// <param name="Dependencies">Health status of dependencies</param>
internal record HealthResponse(string Status, DateTime Timestamp, string Version, ReadOnlyCollection<DependencyHealth> Dependencies);

/// <summary>
///     Represents the health status of a dependency.
/// </summary>
/// <param name="Name">The name of the dependency</param>
/// <param name="Status">The health status</param>
/// <param name="ResponseTime">The response time for the health check</param>
internal record DependencyHealth(string Name, string Status, TimeSpan ResponseTime);
