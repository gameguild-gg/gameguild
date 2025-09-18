namespace GameGuild.Modules.Credentials;

/// <summary> Extension methods for registering Credentials module services </summary>
public static class CredentialsModule {
    /// <summary> Registers all Credentials module services </summary>
    public static IServiceCollection AddCredentialsModule(this IServiceCollection services) {
        // Register Credentials services
        services.AddScoped<ICredentialService, CredentialService>();

        // CQRS handlers are automatically registered by assembly scanning

        return services;
    }
}
