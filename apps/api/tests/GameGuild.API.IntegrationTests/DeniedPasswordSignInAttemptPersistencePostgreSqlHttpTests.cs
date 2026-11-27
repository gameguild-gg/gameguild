using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using GameGuild.API.Database;
using GameGuild.API.IntegrationTests.Infrastructure;
using GameGuild.Identity.Authentication;
using GameGuild.Identity.Tenants;
using GameGuild.Identity.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GameGuild.API.IntegrationTests;

/// <summary>
///     Issue #1406. A denied password sign-in ends in an exception, and the sign-in
///     command's transaction rolls back with it. The failure it records has to survive
///     that rollback: account lockout, the source-IP throttle and the CAPTCHA threshold
///     all count failed rows.
/// </summary>
[Collection(ApiPostgreSqlCollection.Name)]
public sealed class DeniedPasswordSignInAttemptPersistencePostgreSqlHttpTests(ApiPostgreSqlFixture fixture)
{
    private const int FailureCeiling = 3;

    private static readonly string PolymorphicEndpoint = "/" + typeof(AuthController).GetMethod(nameof(AuthController.PolymorphicSignIn))!
        .GetCustomAttribute<HttpPostAttribute>()!.Template!.Replace("v{version:apiVersion}", "v1", StringComparison.Ordinal);

    private static readonly string LocalEndpoint = "/" + typeof(AuthController).GetMethod(nameof(AuthController.LocalSignIn))!
        .GetCustomAttribute<HttpPostAttribute>()!.Template!.Replace("v{version:apiVersion}", "v1", StringComparison.Ordinal);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeniedPasswordSignInLeavesItsFailedAttemptBehind(bool localEndpoint)
    {
        using var factory = CreateFactory();
        var account = await SeedAsync(factory);
        using var client = factory.CreateClient();

        using var response = await PostAsync(client, account, localEndpoint, SyntheticPassword());

        await AssertGenericDenialAsync(factory, response);
        Assert.Equal(1, await CountFailuresAsync(factory, account));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RealFailuresLockTheAccountBeforeTheNextPasswordIsChecked(bool localEndpoint)
    {
        using var factory = CreateFactory();
        var account = await SeedAsync(factory);
        using var client = factory.CreateClient();

        // Control: the password signs in while the account has no failures.
        using (var control = await PostAsync(client, account, localEndpoint, account.Password))
        {
            using var payload = JsonDocument.Parse(await control.Content.ReadAsStringAsync());
            Assert.Equal(HttpStatusCode.OK, control.StatusCode);
            Assert.True(payload.RootElement.GetProperty("success").GetBoolean());
        }
        var sessionsBefore = await CountSessionsAsync(factory, account);

        for (var failure = 0; failure < FailureCeiling; failure++)
        {
            using var denied = await PostAsync(client, account, localEndpoint, SyntheticPassword());
            await AssertGenericDenialAsync(factory, denied);
        }
        Assert.Equal(FailureCeiling, await CountFailuresAsync(factory, account));

        // The same password that signed in a moment ago is now refused: the failures
        // above were counted, so admission locks the account before any credential work.
        using var locked = await PostAsync(client, account, localEndpoint, account.Password);
        await AssertGenericDenialAsync(factory, locked);
        Assert.Equal(sessionsBefore, await CountSessionsAsync(factory, account));
    }

    private WebApplicationFactory<Program> CreateFactory()
    {
        var bytes = RandomNumberGenerator.GetBytes(16);
        bytes[0] = 0x20; bytes[1] = 0x01; bytes[2] = 0x0d; bytes[3] = 0xb8;
        return fixture.CreateFactory(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?> { ["PresentationLayer:Authentication:PasswordPolicy:BCryptWorkFactor"] = "10" }));
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IStartupFilter>(new SourceAddressStartupFilter(new IPAddress(bytes)));
                services.RemoveAll<global::GameGuild.Configuration.ApplicationLayer.AuthenticationSecurityOptions>();
                services.AddSingleton(new global::GameGuild.Configuration.ApplicationLayer.AuthenticationSecurityOptions
                {
                    MaxFailedAttemptsPerHour = FailureCeiling,
                    // Kept above the account ceiling so it is the account lockout that engages.
                    MaxAttemptsPerIpPerHour = FailureCeiling * 4,
                    EnableIpThrottling = true,
                    AccountLockoutDurationMinutes = 30,
                    EnableProgressiveDelay = false,
                    TurnstileRequiredAfterFailedAttempts = 0
                });
            });
        });
    }

    private static Task<HttpResponseMessage> PostAsync(HttpClient client, Account account, bool localEndpoint, string password) => localEndpoint
        ? client.PostAsJsonAsync(LocalEndpoint, new { email = account.User.Email, password, account.TenantId })
        : client.PostAsJsonAsync(PolymorphicEndpoint, new
        {
            credential = account.User.Email, credentialType = CredentialType.Email, password, account.TenantId
        });

    private static async Task AssertGenericDenialAsync(WebApplicationFactory<Program> factory, HttpResponseMessage response)
    {
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        Assert.Equal(
            scope.ServiceProvider.GetRequiredService<IUserEnumerationProtectionService>().GetGenericErrorMessage("login"),
            payload.RootElement.GetProperty("detail").GetString());
    }

    private static async Task<int> CountFailuresAsync(WebApplicationFactory<Program> factory, Account account)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await db.Set<AuthenticationAttempt>().AsNoTracking()
            .CountAsync(attempt => attempt.Email == account.User.Email && !attempt.IsSuccessful);
    }

    private static async Task<int> CountSessionsAsync(WebApplicationFactory<Program> factory, Account account)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await db.Set<UserSession>().AsNoTracking().CountAsync(session => session.UserId == account.User.Id);
    }

    private static async Task<Account> SeedAsync(WebApplicationFactory<Program> factory)
    {
        var marker = Guid.NewGuid().ToString("N");
        var password = SyntheticPassword();
        using var scope = factory.Services.CreateScope();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var user = User.CreateWithPassword($"persist-{marker}@example.test", "Synthetic persistence account", hasher.HashPassword(password), $"persist-{marker}");
        user.VerifyEmail();
        var tenant = Guid.NewGuid();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Set<User>().Add(user);
        db.Set<Tenant>().Add(new Tenant { Id = tenant, Name = $"Persistence {marker}", Slug = $"persist-{marker}", AdminEmail = $"admin-{marker}@example.test", IsActive = true });
        db.Set<TenantMember>().Add(new TenantMember { Id = Guid.NewGuid(), TenantId = tenant, UserId = user.Id, Role = "Member", IsActive = true });
        await db.SaveChangesAsync();
        return new Account(user, tenant, password);
    }

    private static string SyntheticPassword() => "aA7!" + Convert.ToHexString(RandomNumberGenerator.GetBytes(20));

    private sealed record Account(User User, Guid TenantId, string Password);

    private sealed class SourceAddressStartupFilter(IPAddress address) : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use(async (context, continuation) =>
            {
                context.Connection.RemoteIpAddress = address;
                await continuation(context);
            });
            next(app);
        };
    }
}
