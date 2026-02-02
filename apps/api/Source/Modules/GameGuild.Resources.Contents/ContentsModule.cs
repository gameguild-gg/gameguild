using GameGuild.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Resources.Contents;

/// <summary>
///     Contents module - provides content versioning, review workflow, and publishing
/// </summary>
public class ContentsModule : IModule
{
    /// <inheritdoc />
    public string Name => "Contents";

    /// <inheritdoc />
    public IServiceCollection ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Register content versioning services
        services.AddScoped<IContentVersioningService, ContentVersioningService>();

        return services;
    }

    /// <inheritdoc />
    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        // Map endpoints here
        return endpoints;
    }
}
