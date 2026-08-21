namespace GameGuild.API.Setup;

using Swashbuckle.AspNetCore.SwaggerGen;

internal interface IApiProductComposition
{
    string ApplicationName { get; }

    IReadOnlyList<string> EnabledModules { get; }

    IReadOnlyList<string> DisabledModules { get; }

    void ConfigureServices(WebApplicationBuilder builder);

    void ConfigureOpenApi(SwaggerGenOptions options);

    Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken);

    Task<bool> InitializeAsync(
        WebApplication app,
        bool databaseInitialized,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken);
}
