using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;


namespace GameGuild.Tests.Fixtures;

/// <summary>
/// Minimal startup class for testing GraphQL infrastructure with mock modules only
/// </summary>
public class MockModuleTestStartup {
  public void ConfigureServices(IServiceCollection services) {
    // Services are configured in the fixture
    // This is just a placeholder for the test server
    services.AddControllers();
  }

  public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
    app.UseRouting();

    app.UseEndpoints(endpoints => {
      endpoints.MapControllers();
      endpoints.MapGraphQL("/graphql");
    });
  }
}
