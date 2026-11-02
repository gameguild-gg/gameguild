using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Social.Mentorship;

/// <summary>
///     Composition entry points for the mentorship program (issue #102). Command and
///     query handlers are discovered by the host CQRS assembly scan; these entry
///     points register module-level services and expose the module descriptor.
/// </summary>
public static class MentorshipModule
{
    /// <summary>
    ///     Adds mentorship services to the dependency injection container.
    /// </summary>
    public static IServiceCollection AddMentorshipModule(this IServiceCollection services)
    {
        // Command and query handlers are auto-registered by the host CQRS scan over
        // module assemblies (see ApplicationLayerExtensions); nothing extra to wire yet.
        return services;
    }
}

/// <summary>Module descriptor for the Social.Mentorship module.</summary>
public sealed class MentorshipSocialModule : ModuleBase
{
    public override string Name => "Social.Mentorship";
    public override int Order => 165;

    public override IServiceCollection ConfigureServices(IServiceCollection services, IConfiguration configuration)
        => services.AddMentorshipModule();
}
