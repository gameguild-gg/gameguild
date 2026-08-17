using GameGuild.API.Database;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Tests.Authentication.Integration;

public sealed class AuthenticationApiFactory : WebApplicationFactory<GameGuild.API.Program>
{
    private readonly string _databaseName = $"AuthenticationIntegrationTests_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            var descriptorsToRemove = services
                .Where(descriptor =>
                    descriptor.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                    descriptor.ServiceType == typeof(ApplicationDbContext) ||
                    descriptor.ServiceType.FullName?.Contains("EntityFramework", StringComparison.Ordinal) == true ||
                    descriptor.ImplementationType?.FullName?.Contains("Npgsql", StringComparison.Ordinal) == true)
                .ToList();

            foreach (var descriptor in descriptorsToRemove)
                services.Remove(descriptor);

            services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(_databaseName));
            services.AddMemoryCache();
            services.AddHttpLogging(_ => { });
        });
    }
}
