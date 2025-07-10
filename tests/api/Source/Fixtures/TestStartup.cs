using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.API.Tests.Fixtures;

/// <summary>
/// A startup class for the test server that configures test services
/// </summary>
public class TestStartup {
    public void ConfigureServices(IServiceCollection services) {
        // Add controllers and other services
        services.AddControllers();

        // Configure GraphQL - commented out to avoid dependency issues
        // services.AddGraphQLServer()
        //     .AddQueryType<TestQuery>()
        //     .AddMutationType<TestMutation>();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints => {
                endpoints.MapControllers();
                endpoints.MapGraphQL("/graphql");
            }
        );
    }
}