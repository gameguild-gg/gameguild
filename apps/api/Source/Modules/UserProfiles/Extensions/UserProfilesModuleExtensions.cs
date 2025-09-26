namespace GameGuild.Modules.UserProfiles;

public static class UserProfilesModuleExtensions
{
    public static IServiceCollection AddUserProfilesModule(this IServiceCollection services)
    {
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<IUserProfileService, UserProfileService>();

        return services;
    }
}
