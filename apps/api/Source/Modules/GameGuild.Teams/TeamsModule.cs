using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Teams;

public static class TeamsModule
{
    public static IServiceCollection AddTeamsModule(this IServiceCollection services)
    {
        services.AddScoped<ITeamAuthorizationService, TeamAuthorizationService>();
        return services;
    }
}
