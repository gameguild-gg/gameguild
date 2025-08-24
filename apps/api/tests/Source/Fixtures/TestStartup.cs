using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;


namespace GameGuild.Tests.Fixtures;

/// <summary>
/// A startup class for the test server that configures test services
/// </summary>
public class TestStartup {
  public void ConfigureServices(IServiceCollection services) {
    // Add controllers and other services
    services.AddControllers();
  }

  public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
    // Do NOT add HTTPS redirection for tests
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
