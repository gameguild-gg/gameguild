using GameGuild.Announcements.Contracts;
using GameGuild.CQRS;
using GameGuild.Social.Announcements.Services;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Social.Announcements;

/// <summary>
/// Social.Announcements module: community posts and notifications for published content.
/// </summary>
public class AnnouncementsModule : ModuleBase
{
    public override string Name => "Social.Announcements";

    public override IServiceCollection ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ICommandHandler<AnnouncePublicationCommand, Result>, PublicationAnnouncerHandler>();
        services.AddScoped<IRequestHandler<AnnouncePublicationCommand, Result>>(serviceProvider =>
            serviceProvider.GetRequiredService<ICommandHandler<AnnouncePublicationCommand, Result>>());
        return services;
    }

    public override IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints) => endpoints;
}
