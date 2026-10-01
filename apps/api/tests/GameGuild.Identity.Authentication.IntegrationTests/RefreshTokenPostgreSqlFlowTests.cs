using System.Net.Http.Json;
using GameGuild.API.Database;
using GameGuild.Identity.Authentication;
using GameGuild.Identity.Tenants;
using GameGuild.TestSupport.Finance.Economy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Xunit;

namespace GameGuild.Tests.Authentication.Integration;

public sealed class RefreshTokenPostgreSqlFlowTests : IAsyncLifetime
{
    private EconomyPostgreSqlTestDatabase _database = null!;
    private PostgreSqlAuthenticationApiFactory _factory = null!;
    private HttpClient _client = null!;
    private IServiceScope _scope = null!;

    public async Task InitializeAsync()
    {
        _database = await EconomyPostgreSqlTestDatabase.CreateAsync("authentication_refresh_flows");
        await ApplyMigrationsAsync(_database.ConnectionString);

        _factory = new PostgreSqlAuthenticationApiFactory(_database.ConnectionString);
        _client = _factory.CreateClient();
        _scope = _factory.Services.CreateScope();

        var dbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (!await dbContext.Set<Tenant>().AnyAsync(tenant => tenant.IsDefault))
        {
            dbContext.Add(new Tenant
            {
                Name = "Public tenant",
                Slug = "public",
                AdminEmail = "admin@public.test",
                IsDefault = true,
                IsActive = true
            });
            await dbContext.SaveChangesAsync();
        }
    }

    public async Task DisposeAsync()
    {
        _scope?.Dispose();
        _client?.Dispose();
        _factory?.Dispose();

        if (_database is not null)
        {
            await _database.DisposeAsync();
        }
    }

    [Fact]
    public async Task RefreshToken_ShouldReturnNewToken_WhenValidRefreshTokenProvided()
    {
        var authService = _scope.ServiceProvider.GetRequiredService<IAuthService>();
        var signUpResult = await authService.LocalSignUpAsync(new LocalSignUpRequest
        {
            Email = $"refresh.{Guid.NewGuid():N}@example.test",
            Username = $"refresh_{Guid.NewGuid():N}",
            Password = "RefreshTest123!"
        });

        using var response = await _client.PostAsJsonAsync(
            "/v1/auth/tokens:refresh",
            new RefreshTokenRequest { RefreshToken = signUpResult.RefreshToken });

        var content = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"Response status: {response.StatusCode}, Content: {content}");

        var refreshResult = await response.Content.ReadFromJsonAsync<SignInResponse>();
        Assert.NotNull(refreshResult);
        Assert.False(string.IsNullOrWhiteSpace(refreshResult.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(refreshResult.RefreshToken));
        Assert.NotEqual(signUpResult.RefreshToken, refreshResult.RefreshToken);
        Assert.Equal(signUpResult.UserId, refreshResult.UserId);
    }

    [Fact]
    public async Task LocalAuth_CompleteFlow_SignUpSignInRefreshRevoke_ShouldWorkCorrectly()
    {
        var authService = _scope.ServiceProvider.GetRequiredService<IAuthService>();
        var tokenHasher = _scope.ServiceProvider.GetRequiredService<IRefreshTokenHasher>();
        var dbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var email = $"local.flow.{Guid.NewGuid():N}@example.test";
        var password = $"Refresh{Guid.NewGuid():N}!9";

        var signUpResult = await authService.LocalSignUpAsync(new LocalSignUpRequest
        {
            Email = email,
            Username = $"user_{Guid.NewGuid():N}",
            Password = password
        });
        Assert.False(string.IsNullOrWhiteSpace(signUpResult.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(signUpResult.RefreshToken));

        var signInResult = await authService.LocalSignInAsync(new LocalSignInRequest
        {
            Email = email,
            Password = password
        });
        Assert.Equal(signUpResult.UserId, signInResult.UserId);
        Assert.False(string.IsNullOrWhiteSpace(signInResult.RefreshToken));

        var refreshResult = await authService.RefreshTokenAsync(new RefreshTokenRequest
        {
            RefreshToken = signInResult.RefreshToken
        });
        Assert.False(string.IsNullOrWhiteSpace(refreshResult.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(refreshResult.RefreshToken));
        Assert.NotEqual(signInResult.RefreshToken, refreshResult.RefreshToken);

        await authService.RevokeRefreshTokenAsync(refreshResult.RefreshToken, "127.0.0.1");

        var revokedHash = tokenHasher.HashToken(refreshResult.RefreshToken);
        var revokedToken = await dbContext.Set<RefreshToken>()
            .AsNoTracking()
            .SingleOrDefaultAsync(token => token.Token == revokedHash);
        Assert.NotNull(revokedToken);
        Assert.True(revokedToken.IsRevoked);
    }

    private static async Task ApplyMigrationsAsync(string connectionString)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName))
            .ConfigureWarnings(warnings =>
                warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;

        await using var dbContext = new ApplicationDbContext(options);
        await dbContext.Database.MigrateAsync();
    }

    private sealed class PostgreSqlAuthenticationApiFactory(string connectionString)
        : WebApplicationFactory<GameGuild.API.Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            var connection = new NpgsqlConnectionStringBuilder(connectionString);

            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = connectionString,
                    ["ConnectionStrings:MigrationConnection"] = connectionString,
                    ["Database:FailStartupOnMigrationFailure"] = "true",
                    ["Database:GrantRuntimeRoleAfterMigrations"] = "false",
                    ["Database:RunStartupInitialization"] = "false",
                    ["POSTGRES_HOST"] = connection.Host,
                    ["POSTGRES_PORT"] = connection.Port.ToString(),
                    ["POSTGRES_DB"] = connection.Database,
                    ["POSTGRES_USER"] = connection.Username,
                    ["POSTGRES_PASSWORD"] = connection.Password,
                    ["POSTGRES_MIN_POOL_SIZE"] = "0"
                });
            });
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ApplicationDbContext>();
                services.RemoveAll<DbContext>();
                services.RemoveAll<DbContextOptions>();
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();

                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseNpgsql(connectionString, npgsql =>
                        npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));
                services.AddScoped<DbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
                services.AddHttpLogging(_ => { });
            });
        }
    }
}
