using GameGuild.Social.Blog.Configuration;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Social.Blog;

public static class BlogDependencyInjection
{
    public static IServiceCollection AddSocialBlogModule(this IServiceCollection services)
    {
        // Command/query/handler registrations are (re)introduced with the todo-2 service rewrite.
        return services;
    }
}

public sealed class SocialBlogModule : ModuleBase
{
    public override string Name => "Social.Blog";
    public override int Order => 163;

    public override IServiceCollection ConfigureServices(IServiceCollection services, IConfiguration configuration)
        => services.AddSocialBlogModule();

    public override IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints) => endpoints;
}
