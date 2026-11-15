using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GameGuild.API.Database;
using GameGuild.Configuration.ApplicationLayer;
using GameGuild.Compliance.Audit;
using GameGuild.Identity.Authentication;
using GameGuild.Identity.Tenants;
using GameGuild.Identity.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace GameGuild.API.IntegrationTests;

public sealed partial class ApiKeyPolicyPostgreSqlHttpTests
{
    [Fact]
    public async Task TenantIsResolvedFromTheAuthenticatedKeyWithoutAClientOverride()
    {
        using var factory = CreateFactory();
        var owner = await Seed(factory.Services, "valid", ApiKeyScopes.ManageApiKeys);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", owner.Plaintext);
        using var response = await client.GetAsync(Route);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Contains(body.RootElement.EnumerateArray(), row => row.GetProperty("id").GetGuid() == owner.KeyId);
    }

    [Theory]
    [InlineData("foreign-tenant")]
    [InlineData("inactive-membership")]
    public async Task ValidKeyCannotBypassTenantMembershipOrItsTenantClaim(string boundary)
    {
        using var factory = CreateFactory();
        var owner = await Seed(factory.Services, "valid", ApiKeyScopes.ManageApiKeys);
        var foreign = await Seed(factory.Services, "valid", ApiKeyScopes.ManageApiKeys);
        if (boundary == "inactive-membership")
        {
            await using var write = factory.Services.CreateAsyncScope();
            var db = write.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var membership = await db.Set<TenantMember>().SingleAsync(row =>
                row.UserId == owner.OwnerId && row.TenantId == owner.TenantId);
            membership.IsActive = false;
            await db.SaveChangesAsync();
        }
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", owner.Plaintext);
        client.DefaultRequestHeaders.Add("X-Tenant-Id",
            (boundary == "foreign-tenant" ? foreign.TenantId : owner.TenantId).ToString());
        using var response = await client.GetAsync(Route);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ApiKeyAloneCannotMintAnotherCredentialWithoutAUserSessionAndVerification()
    {
        using var factory = CreateFactory();
        var owner = await Seed(factory.Services, "valid", ApiKeyScopes.ManageApiKeys);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", owner.Plaintext);
        client.DefaultRequestHeaders.Add("X-Tenant-Id", owner.TenantId.ToString());
        using var response = await client.PostAsJsonAsync(Route, new
        {
            name = "Synthetic key-only mint refusal",
            scopes = new[] { ApiKeyScopes.ManageApiKeys },
            userId = owner.OwnerId,
            tenantId = owner.TenantId
        });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await using var read = factory.Services.CreateAsyncScope();
        var db = read.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(1, await db.Set<ApiKey>().CountAsync(row => row.UserId == owner.OwnerId));
        Assert.False(await db.Set<AuditLog>().AnyAsync(row =>
            row.UserId == owner.OwnerId && row.ActionType == ApiKeyAuditActions.Created));
    }

    [Fact]
    public async Task ActualHttpLifecyclePreservesOwnershipHashingRevocationAndSafeAudit()
    {
        using var factory = CreateFactory();
        var owner = await Seed(factory.Services, "valid", ApiKeyScopes.ManageApiKeys);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", owner.Plaintext);
        client.DefaultRequestHeaders.Add("X-Tenant-Id", owner.TenantId.ToString());

        // Credential issuance requires a real user/session and a receipt bound to this intent.
        // The configured ApiKey policy remains responsible for the management operations below.
        using var ownerFactory = CreateCredentialOwnerFactory();
        var (accessToken, backupCode) = await EnrollCredentialOwnerAsync(ownerFactory, owner);
        using var ownerClient = ownerFactory.CreateClient();
        ownerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        ownerClient.DefaultRequestHeaders.Add("X-Tenant-Id", owner.TenantId.ToString());
        var intent = new
        {
            name = "Synthetic HTTP lifecycle",
            scopes = new[] { ApiKeyScopes.ManageApiKeys },
            userId = Guid.NewGuid(),
            tenantId = Guid.NewGuid()
        };
        using var demandResponse = await ownerClient.PostAsJsonAsync(Route, intent);
        Assert.Equal(HttpStatusCode.Forbidden, demandResponse.StatusCode);
        var demand = await demandResponse.Content.ReadFromJsonAsync<StepUpAuthenticationRequiredResponse>(CredentialOwnerJsonOptions);
        Assert.NotNull(demand);
        Assert.True(demand.StepUpRequired);
        Assert.NotEqual(Guid.Empty, demand.ChallengeId);
        await using (var beforeVerification = ownerFactory.Services.CreateAsyncScope())
        {
            var pendingDb = beforeVerification.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Equal(1, await pendingDb.Set<ApiKey>().CountAsync(row => row.UserId == owner.OwnerId));
        }
        using var verification = await ownerClient.PostAsJsonAsync(
            $"/v1/auth/step-up/challenges/{demand.ChallengeId}:verify",
            new VerifyStepUpChallengeRequest(MfaMethod.BackupCode, backupCode), CredentialOwnerJsonOptions);
        Assert.Equal(HttpStatusCode.OK, verification.StatusCode);
        var receipt = await verification.Content.ReadFromJsonAsync<StepUpReceiptResponse>(CredentialOwnerJsonOptions);
        Assert.NotNull(receipt);
        Assert.False(string.IsNullOrWhiteSpace(receipt.Receipt));
        using var creation = await ownerClient.PostAsJsonAsync(Route, new
        {
            intent.name,
            intent.scopes,
            intent.userId,
            intent.tenantId,
            stepUpReceipt = receipt.Receipt
        });
        Assert.Equal(HttpStatusCode.Created, creation.StatusCode);
        var created = (await creation.Content.ReadFromJsonAsync<CreateApiKeyResponse>())!;
        Assert.NotNull(created);
        using var listing = await client.GetAsync(Route);
        Assert.Equal(HttpStatusCode.OK, listing.StatusCode);
        var listed = await listing.Content.ReadAsStringAsync();
        Assert.DoesNotContain(created.ApiKey, listed, StringComparison.Ordinal);
        Assert.DoesNotContain("\"keyHash\"", listed, StringComparison.OrdinalIgnoreCase);

        using var rotation = await client.PostAsJsonAsync(Route + "/" + created.Id + ":rotate",
            new { gracePeriodMinutes = 0 });
        Assert.Equal(HttpStatusCode.OK, rotation.StatusCode);
        var rotated = (await rotation.Content.ReadFromJsonAsync<RotateApiKeyResponse>())!;
        Assert.True(rotated.OldKeyRevoked);
        Assert.Null(rotated.OldKeyGraceEndsAt);
        Assert.Equal(created.Id, rotated.OldKeyId);

        using var oldClient = factory.CreateClient();
        oldClient.DefaultRequestHeaders.Add("X-API-Key", created.ApiKey);
        oldClient.DefaultRequestHeaders.Add("X-Tenant-Id", owner.TenantId.ToString());
        using var oldResponse = await oldClient.GetAsync(Route);
        Assert.Equal(HttpStatusCode.Unauthorized, oldResponse.StatusCode);
        using var replacementClient = factory.CreateClient();
        replacementClient.DefaultRequestHeaders.Add("X-API-Key", rotated.NewKey.ApiKey);
        replacementClient.DefaultRequestHeaders.Add("X-Tenant-Id", owner.TenantId.ToString());
        using var replacementResponse = await replacementClient.GetAsync(Route);
        Assert.Equal(HttpStatusCode.OK, replacementResponse.StatusCode);
        using var revocation = await client.PostAsJsonAsync(Route + "/" + rotated.NewKey.Id + ":revoke",
            new { reason = "Synthetic HTTP revocation" });
        Assert.Equal(HttpStatusCode.OK, revocation.StatusCode);
        using var rejectedReplacement = await replacementClient.GetAsync(Route);
        Assert.Equal(HttpStatusCode.Unauthorized, rejectedReplacement.StatusCode);

        await using var read = factory.Services.CreateAsyncScope();
        var db = read.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var old = await db.Set<ApiKey>().AsNoTracking().SingleAsync(row => row.Id == created.Id);
        var replacement = await db.Set<ApiKey>().AsNoTracking().SingleAsync(row => row.Id == rotated.NewKey.Id);
        Assert.Equal(owner.OwnerId, old.UserId);
        Assert.Equal(owner.TenantId, old.TenantId);
        Assert.Equal(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(created.ApiKey))).ToLowerInvariant(), old.KeyHash);
        Assert.Equal(created.Id, replacement.ReplacesKeyId);
        Assert.Equal(owner.OwnerId, replacement.UserId);
        Assert.Equal(owner.TenantId, replacement.TenantId);
        Assert.False(old.IsActive);
        Assert.NotNull(old.RevokedAt);
        Assert.False(replacement.IsActive);
        Assert.NotNull(replacement.RevokedAt);
        Assert.True(replacement.UsageCount > 0);
        var audit = await db.Set<AuditLog>().AsNoTracking().Where(row =>
            row.UserId == owner.OwnerId && row.ResourceType == "ApiKey" &&
            (row.ActionType == ApiKeyAuditActions.Created || row.ActionType == ApiKeyAuditActions.Rotated ||
             row.ActionType == ApiKeyAuditActions.Revoked)).ToListAsync();
        Assert.Single(audit, row => row.ActionType == ApiKeyAuditActions.Created && row.ResourceId == created.Id.ToString());
        Assert.Single(audit, row => row.ActionType == ApiKeyAuditActions.Rotated && row.ResourceId == rotated.NewKey.Id.ToString());
        Assert.Single(audit, row => row.ActionType == ApiKeyAuditActions.Revoked && row.ResourceId == rotated.NewKey.Id.ToString());
        Assert.All(audit, row => { Assert.True(row.Success); Assert.Equal(owner.TenantId, row.TenantId); });
        var auditJson = JsonSerializer.Serialize(audit);
        Assert.DoesNotContain(created.ApiKey, auditJson, StringComparison.Ordinal);
        Assert.DoesNotContain(rotated.NewKey.ApiKey, auditJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ActualManagementRouteCannotListRotateOrRevokeAnotherOwnersCredential()
    {
        using var factory = CreateFactory();
        var owner = await Seed(factory.Services, "valid", ApiKeyScopes.ManageApiKeys);
        var foreign = await Seed(factory.Services, "valid", ApiKeyScopes.ManageApiKeys);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", owner.Plaintext);
        client.DefaultRequestHeaders.Add("X-Tenant-Id", owner.TenantId.ToString());
        using var listing = await client.GetAsync(Route);
        Assert.Equal(HttpStatusCode.OK, listing.StatusCode);
        using var listed = JsonDocument.Parse(await listing.Content.ReadAsStringAsync());
        Assert.DoesNotContain(listed.RootElement.EnumerateArray(), row => row.GetProperty("id").GetGuid() == foreign.KeyId);
        foreach (var operation in new[] { "rotate", "revoke" })
        {
            using var denied = await client.PostAsJsonAsync(Route + "/" + foreign.KeyId + ":" + operation, new { });
            Assert.Equal(HttpStatusCode.BadRequest, denied.StatusCode);
            Assert.Contains("ApiKey.NotFound", await denied.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        }
        await using var read = factory.Services.CreateAsyncScope();
        var db = read.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var key = await db.Set<ApiKey>().AsNoTracking().SingleAsync(row => row.Id == foreign.KeyId);
        Assert.True(key.IsActive);
        Assert.Null(key.RevokedAt);
        Assert.Null(key.RotationGraceEndsAt);
        Assert.False(await db.Set<ApiKey>().AnyAsync(row => row.ReplacesKeyId == foreign.KeyId));
    }

    private static readonly JsonSerializerOptions CredentialOwnerJsonOptions = new(JsonSerializerDefaults.Web)
    { Converters = { new JsonStringEnumConverter() } };

    private WebApplicationFactory<Program> CreateCredentialOwnerFactory() =>
        fixture.CreateFactory(builder => builder.ConfigureTestServices(services =>
        {
            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            });
            services.PostConfigure<MfaOptions>(options =>
            {
                options.Enabled = true;
                options.RequireMfaByDefault = false;
            });
            services.RemoveAll<MfaOptions>();
            services.AddSingleton(provider => provider.GetRequiredService<IOptions<MfaOptions>>().Value);
        }));

    private static async Task<(string AccessToken, string BackupCode)> EnrollCredentialOwnerAsync(
        WebApplicationFactory<Program> factory, SeededKey owner)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        var schemes = provider.GetRequiredService<IAuthenticationSchemeProvider>();
        Assert.Equal(typeof(JwtBearerHandler),
            (await schemes.GetSchemeAsync(JwtBearerDefaults.AuthenticationScheme))!.HandlerType);
        Assert.Equal(JwtBearerDefaults.AuthenticationScheme,
            (await schemes.GetDefaultAuthenticateSchemeAsync())!.Name);
        var db = provider.GetRequiredService<ApplicationDbContext>();
        var user = await db.Set<User>().SingleAsync(row => row.Id == owner.OwnerId);
        var now = DateTime.UtcNow;
        var fingerprint = Guid.NewGuid().ToString("N");
        var session = new UserSession
        {
            Id = Guid.NewGuid(), UserId = user.Id, DeviceFingerprint = fingerprint, DeviceInfo = "{}",
            RefreshToken = new RefreshTokenHasher().HashToken(Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))),
            IpAddress = "127.0.0.1", UserAgent = "Synthetic API-key owner verification", IsActive = true,
            CreatedAt = now.AddMinutes(-1), UpdatedAt = now, LastUsedAt = now, ExpiresAt = now.AddHours(1)
        };
        db.AddRange(session,
            new TrustedDevice
            {
                Id = Guid.NewGuid(), UserId = user.Id, DeviceFingerprint = fingerprint,
                DeviceName = "Synthetic previously trusted device", DeviceInfo = "{}", IsActive = true,
                CreatedAt = now, UpdatedAt = now, TrustedAt = now, LastUsedAt = now, ExpiresAt = now.AddDays(1)
            },
            new UserMfaConfiguration
            {
                Id = Guid.NewGuid(), UserId = user.Id, IsEnabled = true, IsSetupComplete = true,
                CreatedAt = now, UpdatedAt = now
            });
        await db.SaveChangesAsync();
        var backupCode = Convert.ToHexString(RandomNumberGenerator.GetBytes(12));
        await provider.GetRequiredService<IBackupCodeMfaService>()
            .StoreBackupCodesForSetupAsync(user.Id, [backupCode]);
        var accessToken = await provider.GetRequiredService<IJwtTokenService>().GenerateAccessTokenAsync(
            user.Id, user.Email, [], owner.TenantId, user.TokenVersion, DateTimeOffset.UtcNow, session.Id);
        return (accessToken, backupCode);
    }
}
