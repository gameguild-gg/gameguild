namespace GameGuild.Modules.Users;

/// <summary> Extension methods for registering Users module services </summary>
public static class UsersModule
{
    /// <summary> Registers all Users module services </summary>
    public static IServiceCollection AddUsersModule(this IServiceCollection services)
    {
        // Register Users repository and services
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserService, UserService>();

        // CQRS handlers are automatically registered by assembly scanning

        return services;
    }
}
