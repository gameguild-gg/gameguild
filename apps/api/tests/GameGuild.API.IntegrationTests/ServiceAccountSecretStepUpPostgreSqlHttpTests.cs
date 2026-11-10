using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using GameGuild.API.Database;
using GameGuild.API.IntegrationTests.Infrastructure;
using GameGuild.Configuration.ApplicationLayer;
using GameGuild.Identity.Authentication;
using GameGuild.Identity.Tenants;
using GameGuild.Identity.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace GameGuild.API.IntegrationTests;

// Real JWT, actor/tenant authorization, PostgreSQL, backup verification, secret service and transaction pipeline.
[Collection(ApiPostgreSqlCollection.Name)]
public sealed class ServiceAccountSecretStepUpPostgreSqlHttpTests(ApiPostgreSqlFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    { Converters = { new JsonStringEnumConverter() } };
    private static string Route(Guid id) => $"/v1/auth/service-accounts/{id:D}:rotate-secret";

    [Fact]
    public async Task EmptyBodyDemandsVerificationAndExactReceiptRotatesSecretOnlyOnce()
    {
        using var factory = CreateFactory();
        var actor = await SeedAsync(factory, true, true);
        var target = await SeedTargetAsync(factory, actor, actor.TenantId);
        using var client = await ClientAsync(factory, actor, actor.SessionId);
        var demand = await DemandAsync(client, target.Id);
        Assert.Equal("rotate-service-account-secret", demand.Operation!.OperationType);
        Assert.Equal("service-account/" + target.Id.ToString("D"), demand.Operation.TargetReference);
        Assert.Equal(new[] { "BackupCode" }, demand.AvailableMethods);
        Assert.Equal(target.ClientSecretHash, (await ReadTargetAsync(factory, target.Id)).ClientSecretHash);
        using var fabricated = await client.PostAsJsonAsync(Route(target.Id), new RotateServiceAccountSecretRequest(demand.StepUpToken));
        Assert.Equal(HttpStatusCode.BadRequest, fabricated.StatusCode);
        var receipt = await VerifyAsync(client, demand.ChallengeId, actor.BackupCode);
        using var rotated = await client.PostAsJsonAsync(Route(target.Id), new RotateServiceAccountSecretRequest(receipt.Receipt));
        Assert.Equal(HttpStatusCode.OK, rotated.StatusCode);
        var response = await rotated.Content.ReadFromJsonAsync<SecretRotationResponse>();
        Assert.NotNull(response);
        var stored = await ReadTargetAsync(factory, target.Id);
        Assert.Equal(target.SecretRotationCount + 1, stored.SecretRotationCount);
        Assert.NotEqual(target.ClientSecretHash, stored.ClientSecretHash);
        Assert.True(new RefreshTokenHasher().VerifyToken(response.ClientSecret, stored.ClientSecretHash));
        using var replay = await client.PostAsJsonAsync(Route(target.Id), new RotateServiceAccountSecretRequest(receipt.Receipt));
        Assert.Equal(HttpStatusCode.BadRequest, replay.StatusCode);
        Assert.Equal(stored.ClientSecretHash, (await ReadTargetAsync(factory, target.Id)).ClientSecretHash);
    }

    [Theory]
    [InlineData("target")]
    [InlineData("session")]
    [InlineData("owner")]
    public async Task VerifiedReceiptCannotRotateAnotherAccountOrCallerContext(string substitution)
    {
        using var factory = CreateFactory();
        var actor = await SeedAsync(factory, true, true);
        var target = await SeedTargetAsync(factory, actor, actor.TenantId);
        using var original = await ClientAsync(factory, actor, actor.SessionId);
        var demand = await DemandAsync(original, target.Id);
        var receipt = await VerifyAsync(original, demand.ChallengeId, actor.BackupCode);
        var otherActor = substitution == "owner" ? await SeedAsync(factory, true, true) : actor;
        // A second admin in the same tenant isolates actor binding from cross-tenant authorization.
        if (substitution == "owner")
        {
            await using var setup = factory.Services.CreateAsyncScope();
            var db = setup.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Add(new TenantMember { Id = Guid.NewGuid(), UserId = otherActor.UserId, TenantId = actor.TenantId, Role = "Admin", IsActive = true });
            await db.SaveChangesAsync();
            otherActor = otherActor with { TenantId = actor.TenantId };
        }
        var selected = substitution == "target" ? await SeedTargetAsync(factory, actor, actor.TenantId) : target;
        using var caller = await ClientAsync(factory, otherActor, substitution == "session" ? actor.OtherSessionId : otherActor.SessionId);
        using var denied = await caller.PostAsJsonAsync(Route(selected.Id), new RotateServiceAccountSecretRequest(receipt.Receipt));
        Assert.Equal(HttpStatusCode.BadRequest, denied.StatusCode);
        Assert.Equal(selected.ClientSecretHash, (await ReadTargetAsync(factory, selected.Id)).ClientSecretHash);
        await AssertUnconsumedAsync(factory, demand.ChallengeId);
        using var legitimate = await original.PostAsJsonAsync(Route(target.Id), new RotateServiceAccountSecretRequest(receipt.Receipt));
        Assert.Equal(HttpStatusCode.OK, legitimate.StatusCode);
    }

    [Theory]
    [InlineData("anonymous")]
    [InlineData("member")]
    [InlineData("foreign")]
    [InlineData("global")]
    public async Task MissingAuthorityCannotCreateDemandOrRotateSecret(string invalid)
    {
        using var factory = CreateFactory();
        var actor = await SeedAsync(factory, true, true, invalid == "member" ? "Member" : "Admin");
        var owner = invalid == "foreign" ? await SeedAsync(factory, true, true) : actor;
        var target = await SeedTargetAsync(factory, owner, invalid == "global" ? null : owner.TenantId);
        using var client = invalid == "anonymous" ? factory.CreateClient() : await ClientAsync(factory, actor, actor.SessionId);
        using var response = await client.PostAsync(Route(target.Id), null);
        Assert.Equal(invalid == "anonymous" ? HttpStatusCode.Unauthorized : HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(target.ClientSecretHash, (await ReadTargetAsync(factory, target.Id)).ClientSecretHash);
        await using var read = factory.Services.CreateAsyncScope();
        Assert.Empty(await read.ServiceProvider.GetRequiredService<ApplicationDbContext>().Set<StepUpChallenge>().Where(x => x.ActorId == actor.UserId).ToListAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SystemAdminCanExplicitlyVerifyCrossTenantOrGlobalAccount(bool global)
    {
        using var factory = CreateFactory();
        var actor = await SeedAsync(factory, true, true, "SystemAdmin");
        var owner = await SeedAsync(factory, true, true);
        var target = await SeedTargetAsync(factory, owner, global ? null : owner.TenantId);
        using var client = await ClientAsync(factory, actor, actor.SessionId);
        var demand = await DemandAsync(client, target.Id);
        var receipt = await VerifyAsync(client, demand.ChallengeId, actor.BackupCode);
        using var response = await client.PostAsJsonAsync(Route(target.Id), new RotateServiceAccountSecretRequest(receipt.Receipt));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEqual(target.ClientSecretHash, (await ReadTargetAsync(factory, target.Id)).ClientSecretHash);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailureOrCancellationAfterRotationRollsBackSecretAndReceipt(bool cancel)
    {
        var boundary = new SecretCommitBoundary(cancel);
        using var factory = CreateFactory(boundary);
        var actor = await SeedAsync(factory, true, true);
        var target = await SeedTargetAsync(factory, actor, actor.TenantId);
        using var client = await ClientAsync(factory, actor, actor.SessionId);
        var demand = await DemandAsync(client, target.Id);
        var receipt = await VerifyAsync(client, demand.ChallengeId, actor.BackupCode);
        boundary.ActorId = target.Id;
        using var cancellation = new CancellationTokenSource();
        var pending = client.PostAsJsonAsync(Route(target.Id), new RotateServiceAccountSecretRequest(receipt.Receipt), cancellation.Token);
        await boundary.Reached.Task.WaitAsync(TimeSpan.FromSeconds(30));
        Assert.Equal(target.Id, boundary.StoredAccountId);
        if (cancel)
        {
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await pending);
        }
        else
        {
            using var response = await pending;
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }
        await boundary.RolledBack.Task.WaitAsync(TimeSpan.FromSeconds(30));
        var stored = await ReadTargetAsync(factory, target.Id);
        Assert.Equal(target.ClientSecretHash, stored.ClientSecretHash);
        Assert.Equal(target.SecretRotationCount, stored.SecretRotationCount);
        await AssertUnconsumedAsync(factory, demand.ChallengeId);
        boundary.ActorId = null;
        using var retry = await client.PostAsJsonAsync(Route(target.Id), new RotateServiceAccountSecretRequest(receipt.Receipt));
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
    }

    [Fact]
    public async Task ConcurrentRetriesRotateTheSecretOnceForOneReceipt()
    {
        using var factory = CreateFactory();
        var actor = await SeedAsync(factory, true, true);
        var target = await SeedTargetAsync(factory, actor, actor.TenantId);
        using var client = await ClientAsync(factory, actor, actor.SessionId);
        var demand = await DemandAsync(client, target.Id);
        var receipt = await VerifyAsync(client, demand.ChallengeId, actor.BackupCode);
        var responses = await Task.WhenAll(
            client.PostAsJsonAsync(Route(target.Id), new RotateServiceAccountSecretRequest(receipt.Receipt)),
            client.PostAsJsonAsync(Route(target.Id), new RotateServiceAccountSecretRequest(receipt.Receipt)));
        try
        {
            Assert.Equal(1, responses.Count(x => x.StatusCode == HttpStatusCode.OK));
            Assert.Equal(1, responses.Count(x => x.StatusCode == HttpStatusCode.BadRequest));
            Assert.Equal(target.SecretRotationCount + 1, (await ReadTargetAsync(factory, target.Id)).SecretRotationCount);
        }
        finally
        {
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }
    }

    [Fact]
    public async Task MissingAccountReturnsNotFoundWithoutCreatingChallenge()
    {
        using var factory = CreateFactory();
        var actor = await SeedAsync(factory, true, true);
        using var client = await ClientAsync(factory, actor, actor.SessionId);
        using var response = await client.PostAsync(Route(Guid.NewGuid()), null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await using var read = factory.Services.CreateAsyncScope();
        Assert.Empty(await read.ServiceProvider.GetRequiredService<ApplicationDbContext>().Set<StepUpChallenge>().Where(x => x.ActorId == actor.UserId).ToListAsync());
    }

    private static async Task<StepUpAuthenticationRequiredResponse> DemandAsync(HttpClient client, Guid id)
    {
        using var response = await client.PostAsync(Route(id), null);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<StepUpAuthenticationRequiredResponse>(JsonOptions);
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.ChallengeId);
        return result;
    }

    private static async Task<ServiceAccount> SeedTargetAsync(WebApplicationFactory<Program> factory, Account owner, Guid? tenant)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var result = await scope.ServiceProvider.GetRequiredService<IServiceAccountService>().CreateServiceAccountAsync(
            "Synthetic secret rotation target", null, tenant, "reports:read", owner.UserId.ToString());
        return result.Account;
    }

    private static async Task<ServiceAccount> ReadTargetAsync(WebApplicationFactory<Program> factory, Guid id)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Set<ServiceAccount>().AsNoTracking().SingleAsync(x => x.Id == id);
    }

    private static async Task AssertUnconsumedAsync(WebApplicationFactory<Program> factory, Guid id)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        Assert.Null((await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Set<StepUpChallenge>().AsNoTracking().SingleAsync(x => x.Id == id)).ConsumedAt);
    }
    private WebApplicationFactory<Program> CreateFactory() => CreateFactory(null);

    private WebApplicationFactory<Program> CreateFactory(SecretCommitBoundary? boundary) => fixture.CreateFactory(builder => builder.ConfigureTestServices(services =>
    {
        services.PostConfigure<AuthenticationOptions>(options =>
        {
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        });
        services.PostConfigure<MfaOptions>(options => { options.Enabled = true; options.RequireMfaByDefault = false; });
        services.RemoveAll<MfaOptions>();
        services.AddSingleton(provider => provider.GetRequiredService<IOptions<MfaOptions>>().Value);
        if (boundary is not null)
        {
            services.AddDbContext<ApplicationDbContext>(options => options.AddInterceptors(boundary));
        }
    }));

    private static async Task<HttpClient> ClientAsync(WebApplicationFactory<Program> factory, Account account, Guid? session)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        var schemes = provider.GetRequiredService<IAuthenticationSchemeProvider>();
        Assert.Equal(typeof(JwtBearerHandler), (await schemes.GetSchemeAsync("Bearer"))!.HandlerType);
        Assert.Equal("Bearer", (await schemes.GetDefaultAuthenticateSchemeAsync())!.Name);
        var jwt = provider.GetRequiredService<IJwtTokenService>();
        var token = session.HasValue
            ? await jwt.GenerateAccessTokenAsync(account.UserId, account.Email, [account.Role], account.TenantId, account.TokenVersion, DateTimeOffset.UtcNow, session.Value)
            : await jwt.GenerateAccessTokenAsync(account.UserId, account.Email, [account.Role], account.TenantId, account.TokenVersion, DateTimeOffset.UtcNow);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("X-Tenant-Id", account.TenantId.ToString());
        return client;
    }

    private static async Task<Account> SeedAsync(WebApplicationFactory<Program> factory, bool enabled, bool complete, string role = "Admin")
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        var db = provider.GetRequiredService<ApplicationDbContext>();
        var marker = Guid.NewGuid().ToString("N");
        var user = User.Create(marker + "@example.test", "Synthetic step-up account");
        user.Username = marker;
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Step-up tenant " + marker, Slug = "step-up-" + marker, AdminEmail = user.Email, IsActive = true };
        var now = DateTime.UtcNow;
        var fingerprint = Guid.NewGuid().ToString("N");
        UserSession Session(string device) => new()
        {
            Id = Guid.NewGuid(), UserId = user.Id, DeviceFingerprint = device, DeviceInfo = "{}",
            RefreshToken = new RefreshTokenHasher().HashToken(Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))),
            IpAddress = "127.0.0.1", UserAgent = "Synthetic step-up HTTP acceptance", IsActive = true,
            CreatedAt = now.AddMinutes(-1), UpdatedAt = now, LastUsedAt = now, ExpiresAt = now.AddHours(1)
        };
        var session = Session(fingerprint);
        var other = Session(Guid.NewGuid().ToString("N"));
        foreach (var boundSession in new[] { session, other })
        {
            var deviceFingerprint = boundSession.DeviceFingerprint
                ?? throw new InvalidOperationException("A fixture session must have a device fingerprint");
            db.Add(new TrustedDevice
            {
                Id = Guid.NewGuid(), UserId = user.Id, DeviceFingerprint = deviceFingerprint,
                DeviceName = "Synthetic previously trusted device", DeviceInfo = "{}", IsActive = true,
                CreatedAt = now, UpdatedAt = now, TrustedAt = now, LastUsedAt = now, ExpiresAt = now.AddDays(1)
            });
        }
        db.AddRange(user, tenant, new TenantMember { Id = Guid.NewGuid(), UserId = user.Id, TenantId = tenant.Id, Role = role == "SystemAdmin" ? "Admin" : role, IsActive = true },
            session, other, new UserMfaConfiguration { Id = Guid.NewGuid(), UserId = user.Id, IsEnabled = enabled, IsSetupComplete = complete, CreatedAt = now, UpdatedAt = now });
        await db.SaveChangesAsync();
        var code = Convert.ToHexString(RandomNumberGenerator.GetBytes(12));
        await provider.GetRequiredService<IBackupCodeMfaService>().StoreBackupCodesForSetupAsync(user.Id, [code]);
        return new(user.Id, user.Email, tenant.Id, user.TokenVersion, session.Id, other.Id, fingerprint, code, role);
    }

    private static async Task<StepUpReceiptResponse> VerifyAsync(HttpClient client, Guid challenge, string code)
    {
        using var response = await client.PostAsJsonAsync($"/v1/auth/step-up/challenges/{challenge}:verify", new VerifyStepUpChallengeRequest(MfaMethod.BackupCode, code), JsonOptions);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var receipt = await response.Content.ReadFromJsonAsync<StepUpReceiptResponse>(JsonOptions);
        Assert.NotNull(receipt);
        Assert.False(string.IsNullOrWhiteSpace(receipt.Receipt));
        return receipt;
    }

    private sealed class SecretCommitBoundary(bool pauseUntilCancellation) : DbTransactionInterceptor
    {
        private Guid? _transaction;
        public Guid? ActorId { get; set; }
        public Guid StoredAccountId { get; private set; }
        public TaskCompletionSource Reached { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource RolledBack { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override async ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction,
            TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
        {
            if (ActorId is { } actor && eventData.Context is { } context)
            {
                var stored = context.ChangeTracker.Entries<ServiceAccount>()
                    .SingleOrDefault(row => row.Entity.Id == actor);
                if (stored is { State: EntityState.Unchanged })
                {
                    _transaction = eventData.TransactionId;
                    StoredAccountId = stored.Entity.Id;
                    Reached.TrySetResult();
                    if (pauseUntilCancellation)
                    {
                        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                    }
                    throw new InvalidOperationException("Synthetic failure after service-account secret storage before commit");
                }
            }
            return await base.TransactionCommittingAsync(transaction, eventData, result, cancellationToken);
        }

        public override Task TransactionRolledBackAsync(DbTransaction transaction, TransactionEndEventData eventData,
            CancellationToken cancellationToken = default)
        {
            if (_transaction == eventData.TransactionId)
            {
                RolledBack.TrySetResult();
            }
            return base.TransactionRolledBackAsync(transaction, eventData, cancellationToken);
        }
    }

    private sealed record Account(Guid UserId, string Email, Guid TenantId, int TokenVersion, Guid SessionId, Guid OtherSessionId, string Fingerprint, string BackupCode, string Role);
}
