using GameGuild;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.AI;

/// <summary>
///     AI foundation module providing provider-agnostic chat and generation endpoints.
/// </summary>
public sealed class AiModule : ModuleBase
{
    /// <inheritdoc />
    public override string Name => "AI";

    /// <inheritdoc />
    public override int Order => 120;

    /// <inheritdoc />
    public override IServiceCollection ConfigureServices(IServiceCollection services, IConfiguration configuration)
        => services.AddAiModule(configuration);

    /// <inheritdoc />
    public override IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints) => endpoints;
}