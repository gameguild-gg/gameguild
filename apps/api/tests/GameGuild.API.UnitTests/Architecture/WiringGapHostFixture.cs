using GameGuild.API.Database;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace GameGuild.API.UnitTests.Architecture;

/// <summary>
///     Host composition fixture for the wiring-gap guards: builds the REAL API
///     service collection (Program + product composition, all modules) with the
///     same recipe the existing host-composition tests use (see
///     <c>Core/AuthFacadeApiRegistrationTests</c>), capturing the
///     <see cref="ServiceDescriptor"/> list BEFORE any provider is built.
///     Guards G1 (command-handler registration) and G3 (DelegatingHandler
///     lifetime) assert against these descriptors, so they see exactly what the
///     production host registers — including modules whose wiring only exists at
///     registration time.
/// </summary>
/// <remarks>
///     Not sealed: <see cref="RouteSmokeHostFixture"/> (guard E9) derives from this
///     recipe to add a synthetic authentication scheme and stays PG-free the same way.
/// </remarks>
public class WiringGapHostFixture : WebApplicationFactory<Program>
{
    static WiringGapHostFixture() => Core.TestDataProtectionEnvironment.EnsureConfigured();

    public WiringGapHostFixture()
    {
        // Touching Services builds the host once and populates Registrations;
        // the DbContext provider is replaced with the in-memory store, so no
        // PostgreSQL dependency is required.
        _ = Services;
    }

    /// <summary>
    ///     Every service descriptor registered by the real host composition.
    /// </summary>
    public IReadOnlyList<ServiceDescriptor> Registrations { get; private set; } = [];

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?> { ["Database:RunStartupInitialization"] = "false" }));
        builder.ConfigureTestServices(services =>
        {
            // Exercise the complete API composition; only its database provider is replaced.
            services.RemoveAll<ApplicationDbContext>();
            services.RemoveAll<DbContext>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase($"WiringGapGuards_{Guid.NewGuid():N}"));
            services.AddScoped<DbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
            services.AddHttpLogging(_ => { });
            Registrations = services.ToArray();
        });
    }
}

[CollectionDefinition(Name)]
public sealed class WiringGapHostCollection : ICollectionFixture<WiringGapHostFixture>
{
    public const string Name = "WiringGapHost";
}
