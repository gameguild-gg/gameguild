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

// Actual host DI, JWT validation, actor middleware, PostgreSQL, salted backup verification,
// command transactions and device trust. No factor, handler or repository replacements.
[Collection(ApiPostgreSqlCollection.Name)]
public sealed class TrustedDeviceStepUpPostgreSqlHttpTests(ApiPostgreSqlFixture fixture)
{
    private const string Route = "/v1/auth/trusted-devices";
    private const string DeviceName = "  Synthetic step-up device  ";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task AutomaticDemandPersistsExactSubjectBindingAndOnlyVerifiedReceiptCanGrantTrustOnce()
    {
        using var factory = CreateFactory();
        var account = await SeedAsync(factory, true, true);
        using var client = await ClientAsync(factory, account, account.SessionId);
        var demand = await DemandAsync(client);
        Assert.Equal(["BackupCode"], demand.AvailableMethods);
        Assert.Equal(SessionMutationCommandHandler.TrustDeviceStepUpBinding(DeviceName), demand.Operation);
        Assert.Equal(demand.ChallengeId.ToString("D"), demand.StepUpToken);
        await using (var read = factory.Services.CreateAsyncScope())
        {
            var db = read.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var challenge = await db.Set<StepUpChallenge>().AsNoTracking().SingleAsync(row => row.Id == demand.ChallengeId);
            Assert.Equal(account.UserId, challenge.ActorId);
            Assert.Equal(account.TenantId, challenge.TenantId);
            Assert.Equal(account.SessionId, challenge.SessionId);
            Assert.Equal(demand.ExpiresAt, challenge.ExpiresAt.UtcDateTime);
            Assert.Equal(demand.Operation!.PayloadHash, challenge.PayloadHash);
            Assert.Null(challenge.VerifiedAt);
            Assert.Null(challenge.ConsumedAt);
            Assert.Empty(await db.Set<TrustedDevice>().Where(row => row.UserId == account.UserId).ToListAsync());
        }
        using var fabricated = await client.PostAsJsonAsync(Route, new TrustDeviceRequest { DeviceName = DeviceName, StepUpReceipt = demand.StepUpToken });
        Assert.Equal(HttpStatusCode.BadRequest, fabricated.StatusCode);
        var receipt = await VerifyAsync(client, demand.ChallengeId, account.BackupCode);
        using var granted = await client.PostAsJsonAsync(Route, new TrustDeviceRequest { DeviceName = DeviceName, StepUpReceipt = receipt.Receipt });
        Assert.Equal(HttpStatusCode.OK, granted.StatusCode);
        using var replay = await client.PostAsJsonAsync(Route, new TrustDeviceRequest { DeviceName = DeviceName, StepUpReceipt = receipt.Receipt });
        Assert.Equal(HttpStatusCode.BadRequest, replay.StatusCode);
        await using var final = factory.Services.CreateAsyncScope();
        var context = final.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var trusted = Assert.Single(await context.Set<TrustedDevice>().Where(row => row.UserId == account.UserId).ToListAsync());
        Assert.Equal(account.Fingerprint, trusted.DeviceFingerprint);
        Assert.Equal(DeviceName, trusted.DeviceName);
        Assert.True(trusted.IsActive);
        var completed = await context.Set<StepUpChallenge>().SingleAsync(row => row.Id == demand.ChallengeId);
        Assert.NotNull(completed.VerifiedAt);
        Assert.NotNull(completed.ConsumedAt);
        Assert.Equal(MfaMethod.BackupCode, completed.VerificationMethod);
        Assert.Equal(1, completed.VerificationAttempts);
    }

    [Theory]
    [InlineData("different-payload")]
    [InlineData("different-session")]
    [InlineData("different-owner")]
    public async Task ReceiptCannotAuthorizeAnotherPayloadSessionOrOwner(string substitution)
    {
        using var factory = CreateFactory();
        var account = await SeedAsync(factory, true, true);
        using var original = await ClientAsync(factory, account, account.SessionId);
        var demand = await DemandAsync(original);
        var receipt = await VerifyAsync(original, demand.ChallengeId, account.BackupCode);
        var other = substitution == "different-owner" ? await SeedAsync(factory, true, true) : account;
        using var attacker = await ClientAsync(factory, other, substitution == "different-session" ? account.OtherSessionId : other.SessionId);
        using var denied = await attacker.PostAsJsonAsync(Route, new TrustDeviceRequest
        { DeviceName = substitution == "different-payload" ? "Different synthetic payload" : DeviceName, StepUpReceipt = receipt.Receipt });
        Assert.Equal(HttpStatusCode.BadRequest, denied.StatusCode);
        await using (var read = factory.Services.CreateAsyncScope())
        {
            var db = read.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Null((await db.Set<StepUpChallenge>().AsNoTracking().SingleAsync(row => row.Id == demand.ChallengeId)).ConsumedAt);
            Assert.Empty(await db.Set<TrustedDevice>().Where(row => row.UserId == account.UserId || row.UserId == other.UserId).ToListAsync());
        }
        using var legitimate = await original.PostAsJsonAsync(Route, new TrustDeviceRequest { DeviceName = DeviceName, StepUpReceipt = receipt.Receipt });
        Assert.Equal(HttpStatusCode.OK, legitimate.StatusCode);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task IncompleteOrDisabledEnrollmentIsNotAdvertisedAndCannotConsumeBackupEvidence(bool enabled, bool complete)
    {
        using var factory = CreateFactory();
        var account = await SeedAsync(factory, enabled, complete);
        using var client = await ClientAsync(factory, account, account.SessionId);
        var demand = await DemandAsync(client);
        Assert.Empty(demand.AvailableMethods);
        string before;
        await using (var read = factory.Services.CreateAsyncScope())
        {
            before = (await read.ServiceProvider.GetRequiredService<ApplicationDbContext>().Set<UserMfaConfiguration>()
                .AsNoTracking().SingleAsync(row => row.UserId == account.UserId)).BackupCodes!;
        }
        using var denied = await client.PostAsJsonAsync($"/v1/auth/step-up/challenges/{demand.ChallengeId}:verify",
            new VerifyStepUpChallengeRequest(MfaMethod.BackupCode, account.BackupCode), JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, denied.StatusCode);
        await using var final = factory.Services.CreateAsyncScope();
        var db = final.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var configuration = await db.Set<UserMfaConfiguration>().AsNoTracking().SingleAsync(row => row.UserId == account.UserId);
        Assert.Equal(before, configuration.BackupCodes);
        Assert.Equal(enabled, configuration.IsEnabled);
        Assert.Equal(complete, configuration.IsSetupComplete);
        var challenge = await db.Set<StepUpChallenge>().AsNoTracking().SingleAsync(row => row.Id == demand.ChallengeId);
        Assert.Equal(1, challenge.VerificationAttempts);
        Assert.Null(challenge.ReceiptHash);
        Assert.Null(challenge.VerifiedAt);
        Assert.Empty(await db.Set<TrustedDevice>().Where(row => row.UserId == account.UserId).ToListAsync());
    }

    [Fact]
    public async Task ExpiredAutomaticChallengeCannotConsumeTheEnrolledBackupCode()
    {
        using var factory = CreateFactory();
        var account = await SeedAsync(factory, true, true);
        using var client = await ClientAsync(factory, account, account.SessionId);
        var demand = await DemandAsync(client);
        string before;
        await using (var mutation = factory.Services.CreateAsyncScope())
        {
            var db = mutation.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var challenge = await db.Set<StepUpChallenge>().SingleAsync(row => row.Id == demand.ChallengeId);
            challenge.CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-10);
            challenge.ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(-1);
            before = (await db.Set<UserMfaConfiguration>().SingleAsync(row => row.UserId == account.UserId)).BackupCodes!;
            await db.SaveChangesAsync();
        }
        using var denied = await client.PostAsJsonAsync($"/v1/auth/step-up/challenges/{demand.ChallengeId}:verify",
            new VerifyStepUpChallengeRequest(MfaMethod.BackupCode, account.BackupCode), JsonOptions);
        Assert.Equal(HttpStatusCode.Conflict, denied.StatusCode);
        await using var final = factory.Services.CreateAsyncScope();
        var context = final.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(before, (await context.Set<UserMfaConfiguration>().AsNoTracking().SingleAsync(row => row.UserId == account.UserId)).BackupCodes);
        Assert.Equal(0, (await context.Set<StepUpChallenge>().AsNoTracking().SingleAsync(row => row.Id == demand.ChallengeId)).VerificationAttempts);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MissingBearerOrBoundSessionCannotIssueAnAutomaticChallenge(bool sessionlessBearer)
    {
        using var factory = CreateFactory();
        var account = await SeedAsync(factory, true, true);
        using var client = sessionlessBearer ? await ClientAsync(factory, account, null) : factory.CreateClient();
        using var response = await client.PostAsJsonAsync(Route, new TrustDeviceRequest { DeviceName = DeviceName });
        Assert.Equal(sessionlessBearer ? HttpStatusCode.BadRequest : HttpStatusCode.Unauthorized, response.StatusCode);
        await using var read = factory.Services.CreateAsyncScope();
        Assert.Empty(await read.ServiceProvider.GetRequiredService<ApplicationDbContext>().Set<StepUpChallenge>()
            .Where(row => row.ActorId == account.UserId).ToListAsync());
    }

    [Fact]
    public async Task ActualOpenApiDocumentsTheStructuredDemandAndOperationBinding()
    {
        using var factory = fixture.CreateFactory(builder => builder.UseEnvironment("Development"));
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var raw = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(raw);
        var post = document.RootElement.GetProperty("paths").GetProperty(Route).GetProperty("post");
        var reference = post.GetProperty("responses").GetProperty("403").GetProperty("content")
            .GetProperty("application/json").GetProperty("schema").GetProperty("$ref").GetString()!;
        var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");
        var properties = schemas.GetProperty(reference.Split('/')[^1]).GetProperty("properties");
        Assert.Equal("uuid", properties.GetProperty("challengeId").GetProperty("format").GetString());
        Assert.True(properties.TryGetProperty("operation", out _));
        Assert.True(properties.TryGetProperty("availableMethods", out _));
        Assert.Contains("HTTP 403", post.GetProperty("description").GetString());
        var capture = Environment.GetEnvironmentVariable("GAMEGUILD_STEPUP_OPENAPI_CAPTURE");
        if (!string.IsNullOrWhiteSpace(capture))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(capture))!);
            await File.WriteAllTextAsync(capture, raw);
        }
    }

    [Fact]
    public async Task FailureAfterChallengeStorageRollsBackTheAutomaticDemandAndNeverGrantsTrust()
    {
        var boundary = new StepUpCommitBoundary(false);
        using var factory = CreateFactory(boundary);
        var account = await SeedAsync(factory, true, true);
        boundary.ActorId = account.UserId;
        using var client = await ClientAsync(factory, account, account.SessionId);
        using var response = await client.PostAsJsonAsync(Route, new TrustDeviceRequest { DeviceName = DeviceName });
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.True(boundary.Reached.Task.IsCompletedSuccessfully);
        Assert.NotEqual(Guid.Empty, boundary.StoredChallengeId);
        await boundary.RolledBack.Task.WaitAsync(TimeSpan.FromSeconds(30));
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.False(body.RootElement.TryGetProperty("challengeId", out _));
        await AssertNoDemandOrTrustAsync(factory, account.UserId);
    }

    [Fact]
    public async Task RequestCancellationAfterStorageWaitsForActualRollbackBeforeCheckingForOrphanedDemands()
    {
        var boundary = new StepUpCommitBoundary(true);
        using var factory = CreateFactory(boundary);
        var account = await SeedAsync(factory, true, true);
        boundary.ActorId = account.UserId;
        using var client = await ClientAsync(factory, account, account.SessionId);
        using var cancellation = new CancellationTokenSource();
        var request = client.PostAsJsonAsync(Route, new TrustDeviceRequest { DeviceName = DeviceName }, cancellation.Token);
        try
        {
            await boundary.Reached.Task.WaitAsync(TimeSpan.FromSeconds(30));
            Assert.NotEqual(Guid.Empty, boundary.StoredChallengeId);
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await request);
            await boundary.RolledBack.Task.WaitAsync(TimeSpan.FromSeconds(30));
            await AssertNoDemandOrTrustAsync(factory, account.UserId);
        }
        finally
        {
            cancellation.Cancel();
        }
    }

    private static async Task AssertNoDemandOrTrustAsync(WebApplicationFactory<Program> factory, Guid actor)
    {
        await using var read = factory.Services.CreateAsyncScope();
        var db = read.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Empty(await db.Set<StepUpChallenge>().Where(row => row.ActorId == actor).ToListAsync());
        Assert.Empty(await db.Set<TrustedDevice>().Where(row => row.UserId == actor).ToListAsync());
    }

    private WebApplicationFactory<Program> CreateFactory() => CreateFactory(null);

    private WebApplicationFactory<Program> CreateFactory(StepUpCommitBoundary? boundary) => fixture.CreateFactory(builder => builder.ConfigureTestServices(services =>
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
            ? await jwt.GenerateAccessTokenAsync(account.UserId, account.Email, [], account.TenantId, account.TokenVersion, DateTimeOffset.UtcNow, session.Value)
            : await jwt.GenerateAccessTokenAsync(account.UserId, account.Email, [], account.TenantId, account.TokenVersion, DateTimeOffset.UtcNow);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("X-Tenant-Id", account.TenantId.ToString());
        return client;
    }

    private static async Task<Account> SeedAsync(WebApplicationFactory<Program> factory, bool enabled, bool complete)
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
        db.AddRange(user, tenant, new TenantMember { Id = Guid.NewGuid(), UserId = user.Id, TenantId = tenant.Id, Role = "Member", IsActive = true },
            session, other, new UserMfaConfiguration { Id = Guid.NewGuid(), UserId = user.Id, IsEnabled = enabled, IsSetupComplete = complete, CreatedAt = now, UpdatedAt = now });
        await db.SaveChangesAsync();
        var code = Convert.ToHexString(RandomNumberGenerator.GetBytes(12));
        await provider.GetRequiredService<IBackupCodeMfaService>().StoreBackupCodesForSetupAsync(user.Id, [code]);
        return new(user.Id, user.Email, tenant.Id, user.TokenVersion, session.Id, other.Id, fingerprint, code);
    }

    private static async Task<StepUpAuthenticationRequiredResponse> DemandAsync(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync(Route, new TrustDeviceRequest { DeviceName = DeviceName });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var demand = await response.Content.ReadFromJsonAsync<StepUpAuthenticationRequiredResponse>(JsonOptions);
        Assert.NotNull(demand);
        Assert.Equal(403, demand.Status);
        Assert.False(demand.Extensions.ContainsKey("legacy"));
        Assert.NotEqual(Guid.Empty, demand.ChallengeId);
        return demand;
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

    // The real store has saved the challenge and the command is about to commit. Only the
    // database boundary is instrumented; authentication, handlers and persistence stay real.
    private sealed class StepUpCommitBoundary(bool pauseUntilCancellation) : DbTransactionInterceptor
    {
        private Guid? _transaction;
        public Guid? ActorId { get; set; }
        public Guid StoredChallengeId { get; private set; }
        public TaskCompletionSource Reached { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource RolledBack { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override async ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction,
            TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
        {
            if (ActorId is { } actor && eventData.Context is { } context)
            {
                var stored = context.ChangeTracker.Entries<StepUpChallenge>()
                    .SingleOrDefault(row => row.Entity.ActorId == actor && row.Entity.VerifiedAt == null);
                if (stored is { State: EntityState.Unchanged })
                {
                    _transaction = eventData.TransactionId;
                    StoredChallengeId = stored.Entity.Id;
                    Reached.TrySetResult();
                    if (pauseUntilCancellation)
                    {
                        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                    }
                    throw new InvalidOperationException("Synthetic failure after step-up challenge storage");
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

    private sealed record Account(Guid UserId, string Email, Guid TenantId, int TokenVersion, Guid SessionId, Guid OtherSessionId, string Fingerprint, string BackupCode);
}
