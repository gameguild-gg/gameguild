using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using GameGuild.API.Database;
using GameGuild.API.IntegrationTests.Infrastructure;
using GameGuild.CQRS.Models;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Tenants;
using GameGuild.Identity.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.API.IntegrationTests;

[Collection(ApiPostgreSqlCollection.Name)]
public sealed class DataMaskingPostgreSqlIntegrationTests(ApiPostgreSqlFixture fixture)
{
    private const string TestAuthenticationScheme = "DataMaskingTest";

    [Fact]
    public async Task GetUserById_AppliesPersistedTenantRuleUsingRequestActorContext()
    {
        var tenantAId = Guid.NewGuid();
        var tenantBId = Guid.NewGuid();
        var userAId = Guid.NewGuid();
        var userBId = Guid.NewGuid();

        using var factory = fixture.Factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthenticationScheme;
                    options.DefaultChallengeScheme = TestAuthenticationScheme;
                    options.DefaultForbidScheme = TestAuthenticationScheme;
                }).AddScheme<AuthenticationSchemeOptions, DataMaskingTestAuthenticationHandler>(
                    TestAuthenticationScheme,
                    _ => { });
            }));

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await SeedAsync(dbContext, tenantAId, tenantBId, userAId, userBId);
        }

        using var tenantAClient = CreateClient(factory, tenantAId, userAId);
        using var tenantBClient = CreateClient(factory, tenantBId, userBId);

        var tenantAResponse = await tenantAClient.GetAsync($"/v1/users/{userAId}");
        var tenantBResponse = await tenantBClient.GetAsync($"/v1/users/{userBId}");

        Assert.Equal(HttpStatusCode.OK, tenantAResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, tenantBResponse.StatusCode);

        var tenantAUser = await tenantAResponse.Content.ReadFromJsonAsync<UserDto>();
        var tenantBUser = await tenantBResponse.Content.ReadFromJsonAsync<UserDto>();

        Assert.NotNull(tenantAUser);
        Assert.NotNull(tenantBUser);
        Assert.Equal("[REDACTED]", tenantAUser.Email);
        Assert.Equal($"masking-{userBId:N}@example.test", tenantBUser.Email);
        Assert.Equal("Masking User A", tenantAUser.Name);
        Assert.Equal("Masking User B", tenantBUser.Name);
    }

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory, Guid tenantId, Guid actorId)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TenantResolver.TenantIdHeader, tenantId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Actor", actorId.ToString());
        return client;
    }

    private static async Task SeedAsync(
        ApplicationDbContext dbContext,
        Guid tenantAId,
        Guid tenantBId,
        Guid userAId,
        Guid userBId)
    {
        var tenantA = new Tenant
        {
            Id = tenantAId,
            Name = $"Masking Tenant {tenantAId:N}",
            Slug = $"masking-{tenantAId:N}",
            IsActive = true
        };
        var tenantB = new Tenant
        {
            Id = tenantBId,
            Name = $"Masking Tenant {tenantBId:N}",
            Slug = $"masking-{tenantBId:N}",
            IsActive = true
        };
        var userA = User.CreateOAuthUser($"masking-{userAId:N}@example.test", "Masking User A");
        userA.Id = userAId;
        var userB = User.CreateOAuthUser($"masking-{userBId:N}@example.test", "Masking User B");
        userB.Id = userBId;

        dbContext.Set<Tenant>().AddRange(tenantA, tenantB);
        dbContext.Set<User>().AddRange(userA, userB);
        dbContext.Set<TenantMember>().AddRange(
            new TenantMember { TenantId = tenantAId, UserId = userAId, Role = "Member", IsActive = true },
            new TenantMember { TenantId = tenantBId, UserId = userBId, Role = "Member", IsActive = true });
        dbContext.Set<DataMaskingRule>().Add(new DataMaskingRule
        {
            TenantId = new TenantId(tenantAId),
            Name = "Mask user email for tenant A",
            ResourceType = "User",
            FieldName = "Email",
            MaskingType = MaskingType.Redact,
            CreatedBy = userAId,
            IsEnabled = true
        });
        dbContext.Set<PolicyDefinitionEntity>().Add(new PolicyDefinitionEntity
        {
            PolicyName = Policies.UsersReadSelf,
            RequireAuthentication = true,
            RequiredPermissionsJson = "[\"users:read:self\"]",
            IsTenantScoped = true,
            IsActive = true,
            PolicyVersion = 1
        });

        await dbContext.SaveChangesAsync();
    }

    public sealed class DataMaskingTestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var actorId = Request.Headers["X-Test-Actor"].ToString();
            var tenantId = Request.Headers[TenantResolver.TenantIdHeader].ToString();
            if (!Guid.TryParse(actorId, out _) || !Guid.TryParse(tenantId, out _))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var identity = new ClaimsIdentity(
            [
                new Claim("sub", actorId),
                new Claim("tenant_id", tenantId),
                new Claim("permission", "users:read:self")
            ],
            Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
        }
    }
}
