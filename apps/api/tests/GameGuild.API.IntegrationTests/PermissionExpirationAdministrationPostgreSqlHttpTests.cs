using System.Globalization;
using System.Net;
using System.Text.Json;
using GameGuild.API.Database;
using GameGuild.API.IntegrationTests.Infrastructure;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Tenants;
using GameGuild.Identity.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.API.IntegrationTests;

/// <summary>Exercises real MVC binding, tenant/actor middleware, query guards and PostgreSQL reads.
/// Only authentication uses the shared fixture's explicit test scheme; the sender,
/// authorization context, expiration service and repository are not replaced.</summary>
[Collection(ApiPostgreSqlCollection.Name)]
public sealed class PermissionExpirationAdministrationPostgreSqlHttpTests(ApiPostgreSqlFixture fixture)
{
    private const string Route = "/api/v1/authorization/tenants/permissions:expiring";

    [Fact]
    public async Task TenantAdminCanReadExpiringGrantsForTheAuthenticatedRequestTenant()
    {
        var seeded = await SeedAsync();
        using var client = fixture.CreateAuthenticatedClient(seeded.AdminId, seeded.TenantId);
        using var response = await client.GetAsync(Url(seeded.Cutoff));
        await AssertTenantResponseAsync(response, seeded);
    }

    [Fact]
    public async Task QueryTenantCannotReplaceTheValidatedRequestTenant()
    {
        var seeded = await SeedAsync();
        using var client = fixture.CreateAuthenticatedClient(seeded.AdminId, seeded.TenantId);
        using var response = await client.GetAsync(Url(seeded.Cutoff) + $"&tenantId={seeded.OtherTenantId:D}");
        await AssertTenantResponseAsync(response, seeded);
    }

    [Fact]
    public async Task OrdinaryMemberCannotReadAdministrativeExpirationData()
    {
        var seeded = await SeedAsync();
        using var client = fixture.CreateAuthenticatedClient(seeded.MemberId, seeded.TenantId);
        using var response = await client.GetAsync(Url(seeded.Cutoff));
        await AssertDeniedAsync(response, HttpStatusCode.Forbidden, seeded);
    }

    [Fact]
    public async Task AnonymousRequestCannotReadAdministrativeExpirationData()
    {
        var seeded = await SeedAsync();
        using var client = fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", seeded.TenantId.ToString("D"));
        using var response = await client.GetAsync(Url(seeded.Cutoff));
        await AssertDeniedAsync(response, HttpStatusCode.Unauthorized, seeded);
    }

    [Fact]
    public async Task TenantHeaderCannotChangeTheAuthenticatedAdministratorsTenant()
    {
        var seeded = await SeedAsync();
        using var client = fixture.CreateAuthenticatedClient(seeded.AdminId, seeded.TenantId);
        client.DefaultRequestHeaders.Remove("X-Tenant-Id");
        client.DefaultRequestHeaders.Add("X-Tenant-Id", seeded.OtherTenantId.ToString("D"));
        using var response = await client.GetAsync(Url(seeded.Cutoff));
        await AssertDeniedAsync(response, HttpStatusCode.Forbidden, seeded);
    }

    private static string Url(DateTime cutoff) => Route + "?expiresBefore=" +
        Uri.EscapeDataString(cutoff.ToString("O", CultureInfo.InvariantCulture));

    private static async Task AssertTenantResponseAsync(HttpResponseMessage response, Seeded seeded)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK,
            $"Real MVC expiration query returned {(int)response.StatusCode} for a persisted TenantAdmin membership: {body}");
        using var document = JsonDocument.Parse(body);
        Assert.Equal(seeded.TenantId, document.RootElement.GetProperty("tenantId").GetGuid());
        var grants = document.RootElement.GetProperty("expiring").EnumerateArray().ToArray();
        var grant = Assert.Single(grants);
        Assert.Equal(seeded.GrantId, grant.GetProperty("permissionId").GetGuid());
        Assert.Equal(seeded.MemberId, grant.GetProperty("userId").GetGuid());
        Assert.DoesNotContain(seeded.OtherGrantId.ToString("D"), body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(seeded.LaterGrantId.ToString("D"), body, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task AssertDeniedAsync(HttpResponseMessage response, HttpStatusCode expected, Seeded seeded)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == expected, $"Expected HTTP {(int)expected}; got {(int)response.StatusCode}: {body}");
        Assert.DoesNotContain(seeded.GrantId.ToString("D"), body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(seeded.OtherGrantId.ToString("D"), body, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<Seeded> SeedAsync()
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var admin = Account("administrator");
        var member = Account("member");
        var laterMember = Account("later member");
        var otherMember = Account("other member");
        var tenant = CreateTenant();
        var otherTenant = CreateTenant();
        var cutoff = DateTime.UtcNow.AddHours(1);
        var grant = Grant(tenant.Id, member.Id, admin.Id, cutoff.AddMinutes(-30));
        var otherGrant = Grant(otherTenant.Id, otherMember.Id, otherMember.Id, cutoff.AddMinutes(-30));
        var laterGrant = Grant(tenant.Id, laterMember.Id, admin.Id, cutoff.AddHours(3));
        db.AddRange(admin, member, laterMember, otherMember, tenant, otherTenant,
            Membership(tenant.Id, admin.Id, "TenantAdmin"),
            Membership(tenant.Id, member.Id, "Member"),
            Membership(tenant.Id, laterMember.Id, "Member"),
            Membership(otherTenant.Id, otherMember.Id, "TenantAdmin"),
            grant, otherGrant, laterGrant);
        await db.SaveChangesAsync();

        // The positive oracle must be backed by real persisted tenancy, membership and
        // grant rows before it can attribute an HTTP failure to MVC/query integration.
        Assert.True(await db.Set<TenantMember>().AsNoTracking().AnyAsync(row =>
            row.TenantId == tenant.Id && row.UserId == admin.Id && row.Role == "TenantAdmin" && row.IsActive));
        Assert.Equal(3, await db.Set<TenantPermission>().AsNoTracking().CountAsync(row =>
            row.Id == grant.Id || row.Id == otherGrant.Id || row.Id == laterGrant.Id));
        var userIds = new[] { admin.Id, member.Id, laterMember.Id, otherMember.Id };
        var accounts = await db.Set<User>().AsNoTracking().Where(row => userIds.Contains(row.Id))
            .Select(row => new { row.Username, row.Email }).ToArrayAsync();
        Assert.Equal(4, accounts.Length);
        Assert.All(accounts, account =>
        {
            Assert.False(string.IsNullOrWhiteSpace(account.Username));
            Assert.False(string.IsNullOrWhiteSpace(account.Email));
        });
        Assert.Equal(4, accounts.Select(account => account.Username).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(4, accounts.Select(account => account.Email).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        return new(tenant.Id, otherTenant.Id, admin.Id, member.Id, grant.Id, otherGrant.Id, laterGrant.Id, cutoff);
    }

    private static User Account(string label)
    {
        var marker = Guid.NewGuid().ToString("N");
        var user = User.CreateOAuthUser($"expiration-{marker}@example.test", $"Expiration {label} {marker}");
        user.Username = $"expiration-{marker}";
        return user;
    }

    private static Tenant CreateTenant()
    {
        var id = Guid.NewGuid();
        return new Tenant { Id = id, Name = $"Expiration tenant {id:N}", Slug = $"expiration-{id:N}",
            AdminEmail = $"expiration-{id:N}@example.test", IsActive = true };
    }

    private static TenantMember Membership(Guid tenantId, Guid userId, string role) => new()
    {
        Id = Guid.NewGuid(), TenantId = tenantId, UserId = userId, Role = role, IsActive = true
    };

    private static TenantPermission Grant(Guid tenantId, Guid userId, Guid grantor, DateTime expiresAt) => new()
    {
        Id = Guid.NewGuid(), TenantId = tenantId, UserId = userId, GrantedBy = grantor,
        Permissions = ["Read"], ExpiresAt = expiresAt, IsActive = true, Version = 1
    };

    private sealed record Seeded(Guid TenantId, Guid OtherTenantId, Guid AdminId, Guid MemberId,
        Guid GrantId, Guid OtherGrantId, Guid LaterGrantId, DateTime Cutoff);
}
