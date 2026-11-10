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

// Real host, JWT/actor middleware, PostgreSQL, factor verification, and command transactions.
[Collection(ApiPostgreSqlCollection.Name)]
public sealed class ApiKeyCreationStepUpPostgreSqlHttpTests(ApiPostgreSqlFixture fixture)
{
    private const string Route = "/v1/auth/api-keys";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    { Converters = { new JsonStringEnumConverter() } };

    [Fact]
    public async Task AutomaticDemandCanIssueExactlyOneKeyAfterRealBackupVerification()
    {
        using var factory = CreateFactory();
        var account = await SeedAsync(factory, true, true);
        using var client = await ClientAsync(factory, account, account.SessionId);
        var demand = await DemandAsync(client);
        Assert.Equal(new[] { "BackupCode" }, demand.AvailableMethods);
        Assert.Equal("create-api-key", demand.Operation!.OperationType);
        Assert.Equal("current-user", demand.Operation.TargetReference);
        await using (var read = factory.Services.CreateAsyncScope())
        {
            var db = read.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var row = await db.Set<StepUpChallenge>().AsNoTracking().SingleAsync(x => x.Id == demand.ChallengeId);
            Assert.Equal(account.UserId, row.ActorId);
            Assert.Equal(account.TenantId, row.TenantId);
            Assert.Equal(account.SessionId, row.SessionId);
            Assert.Null(row.ConsumedAt);
            Assert.Empty(await db.Set<ApiKey>().Where(x => x.UserId == account.UserId).ToListAsync());
        }
        using var fabricated = await client.PostAsJsonAsync(Route, Intent(demand.StepUpToken));
        Assert.Equal(HttpStatusCode.BadRequest, fabricated.StatusCode);
        var receipt = await VerifyAsync(client, demand.ChallengeId, account.BackupCode);
        using var created = await client.PostAsJsonAsync(Route, Intent(receipt.Receipt));
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        var result = await created.Content.ReadFromJsonAsync<CreateApiKeyResponse>();
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.ApiKey));
        using var replay = await client.PostAsJsonAsync(Route, Intent(receipt.Receipt));
        Assert.Equal(HttpStatusCode.BadRequest, replay.StatusCode);
        await using var final = factory.Services.CreateAsyncScope();
        var context = final.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var key = Assert.Single(await context.Set<ApiKey>().Where(x => x.UserId == account.UserId).ToListAsync());
        Assert.Equal(account.TenantId, key.TenantId);
        Assert.Equal(result.Id, key.Id);
        Assert.NotEqual(result.ApiKey, key.KeyHash);
        Assert.NotNull((await context.Set<StepUpChallenge>().SingleAsync(x => x.Id == demand.ChallengeId)).ConsumedAt);
    }

    [Theory]
    [InlineData("name")]
    [InlineData("scopes")]
    [InlineData("expiry")]
    [InlineData("ip")]
    [InlineData("session")]
    [InlineData("owner")]
    public async Task VerifiedReceiptCannotMintAChangedIntentOrDifferentSubject(string substitution)
    {
        using var factory = CreateFactory();
        var account = await SeedAsync(factory, true, true);
        using var original = await ClientAsync(factory, account, account.SessionId);
        var demand = await DemandAsync(original);
        var receipt = await VerifyAsync(original, demand.ChallengeId, account.BackupCode);
        var other = substitution == "owner" ? await SeedAsync(factory, true, true) : account;
        using var caller = await ClientAsync(factory, other, substitution == "session" ? account.OtherSessionId : other.SessionId);
        var intent = substitution switch
        {
            "name" => Intent(receipt.Receipt) with { Name = "changed-key" },
            "scopes" => Intent(receipt.Receipt) with { Scopes = ["reports:write"] },
            "expiry" => Intent(receipt.Receipt) with { ExpiresAt = DateTime.UtcNow.AddDays(1) },
            "ip" => Intent(receipt.Receipt) with { IpWhitelist = "192.0.2.1" },
            _ => Intent(receipt.Receipt)
        };
        using var denied = await caller.PostAsJsonAsync(Route, intent);
        Assert.Equal(HttpStatusCode.BadRequest, denied.StatusCode);
        await using (var read = factory.Services.CreateAsyncScope())
        {
            var db = read.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Null((await db.Set<StepUpChallenge>().AsNoTracking().SingleAsync(x => x.Id == demand.ChallengeId)).ConsumedAt);
            Assert.Empty(await db.Set<ApiKey>().Where(x => x.UserId == account.UserId || x.UserId == other.UserId).ToListAsync());
        }
        using var legitimate = await original.PostAsJsonAsync(Route, Intent(receipt.Receipt));
        Assert.Equal(HttpStatusCode.OK, legitimate.StatusCode);
    }

    [Theory]
    [InlineData("name")]
    [InlineData("scopes")]
    [InlineData("null-scopes")]
    [InlineData("expiry")]
    public async Task InvalidIntentCreatesNeitherChallengeNorCredential(string invalid)
    {
        using var factory = CreateFactory();
        var account = await SeedAsync(factory, true, true);
        using var client = await ClientAsync(factory, account, account.SessionId);
        var intent = invalid switch
        {
            "name" => Intent() with { Name = "" },
            "scopes" => Intent() with { Scopes = [] },
            "null-scopes" => Intent() with { Scopes = null! },
            _ => Intent() with { ExpiresAt = DateTime.UtcNow.AddDays(-1) }
        };
        using var response = await client.PostAsJsonAsync(Route, intent);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await using var read = factory.Services.CreateAsyncScope();
        var db = read.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Empty(await db.Set<StepUpChallenge>().Where(x => x.ActorId == account.UserId).ToListAsync());
        Assert.Empty(await db.Set<ApiKey>().Where(x => x.UserId == account.UserId).ToListAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MissingIntentBodyCreatesNeitherChallengeNorCredential(bool jsonContentType)
    {
        using var factory = CreateFactory();
        var account = await SeedAsync(factory, true, true);
        using var client = await ClientAsync(factory, account, account.SessionId);
        using var request = new HttpRequestMessage(HttpMethod.Post, Route);
        if (jsonContentType)
        {
            request.Content = new StringContent(string.Empty, System.Text.Encoding.UTF8, "application/json");
        }
        using var response = await client.SendAsync(request);
        Assert.Equal(jsonContentType ? HttpStatusCode.BadRequest : HttpStatusCode.UnsupportedMediaType, response.StatusCode);
        await using var read = factory.Services.CreateAsyncScope();
        var db = read.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Empty(await db.Set<StepUpChallenge>().Where(x => x.ActorId == account.UserId).ToListAsync());
        Assert.Empty(await db.Set<ApiKey>().Where(x => x.UserId == account.UserId).ToListAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailureOrCancellationAfterKeyStorageRollsBackKeyAndReceipt(bool cancel)
    {
        var boundary = new KeyCommitBoundary(cancel);
        using var factory = CreateFactory(boundary);
        var account = await SeedAsync(factory, true, true);
        using var client = await ClientAsync(factory, account, account.SessionId);
        var demand = await DemandAsync(client);
        var receipt = await VerifyAsync(client, demand.ChallengeId, account.BackupCode);
        boundary.ActorId = account.UserId;
        using var cancellation = new CancellationTokenSource();
        var operation = client.PostAsJsonAsync(Route, Intent(receipt.Receipt), cancellation.Token);
        await boundary.Reached.Task.WaitAsync(TimeSpan.FromSeconds(30));
        Assert.NotEqual(Guid.Empty, boundary.StoredKeyId);
        if (cancel)
        {
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await operation);
        }
        else
        {
            using var response = await operation;
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }
        await boundary.RolledBack.Task.WaitAsync(TimeSpan.FromSeconds(30));
        await using (var read = factory.Services.CreateAsyncScope())
        {
            var db = read.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Empty(await db.Set<ApiKey>().Where(x => x.UserId == account.UserId).ToListAsync());
            Assert.Null((await db.Set<StepUpChallenge>().AsNoTracking().SingleAsync(x => x.Id == demand.ChallengeId)).ConsumedAt);
        }
        boundary.ActorId = null;
        using var retry = await client.PostAsJsonAsync(Route, Intent(receipt.Receipt));
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
    }

    [Fact]
    public async Task ConcurrentRetriesCannotMintTwoKeysFromOneReceipt()
    {
        using var factory = CreateFactory();
        var actor = await SeedAsync(factory, true, true);
        using var client = await ClientAsync(factory, actor, actor.SessionId);
        var demand = await DemandAsync(client);
        var receipt = await VerifyAsync(client, demand.ChallengeId, actor.BackupCode);
        var responses = await Task.WhenAll(client.PostAsJsonAsync(Route, Intent(receipt.Receipt)),
            client.PostAsJsonAsync(Route, Intent(receipt.Receipt)));
        try
        {
            Assert.Equal(1, responses.Count(x => x.StatusCode == HttpStatusCode.OK));
            Assert.Equal(1, responses.Count(x => x.StatusCode == HttpStatusCode.BadRequest));
            await using var read = factory.Services.CreateAsyncScope();
            Assert.Single(await read.ServiceProvider.GetRequiredService<ApplicationDbContext>().Set<ApiKey>().Where(x => x.UserId == actor.UserId).ToListAsync());
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
    public async Task ActualSwaggerExposesReceiptInputAndStructuredForbiddenDemand()
    {
        using var factory = fixture.CreateFactory(builder => builder.UseEnvironment("Development"));
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var raw = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(raw);
        var post = document.RootElement.GetProperty("paths").GetProperty(Route).GetProperty("post");
        Assert.True(post.GetProperty("requestBody").GetProperty("required").GetBoolean());
        var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");
        var bodyRef = post.GetProperty("requestBody").GetProperty("content").GetProperty("application/json").GetProperty("schema").GetProperty("$ref").GetString()!;
        Assert.True(schemas.GetProperty(bodyRef.Split('/')[^1]).GetProperty("properties").TryGetProperty("stepUpReceipt", out _));
        var demandRef = post.GetProperty("responses").GetProperty("403").GetProperty("content").GetProperty("application/json").GetProperty("schema").GetProperty("$ref").GetString()!;
        Assert.True(schemas.GetProperty(demandRef.Split('/')[^1]).GetProperty("properties").TryGetProperty("challengeId", out _));
        Assert.Contains("HTTP 403", post.GetProperty("description").GetString());
        var secretPost = document.RootElement.GetProperty("paths").GetProperty("/v1/auth/service-accounts/{serviceAccountId}:rotate-secret").GetProperty("post");
        var secretBody = secretPost.GetProperty("requestBody");
        Assert.False(secretBody.TryGetProperty("required", out var required) && required.GetBoolean());
        var secretBodyRef = secretBody.GetProperty("content").GetProperty("application/json").GetProperty("schema").GetProperty("$ref").GetString()!;
        Assert.True(schemas.GetProperty(secretBodyRef.Split('/')[^1]).GetProperty("properties").TryGetProperty("stepUpReceipt", out _));
        var secretDemandRef = secretPost.GetProperty("responses").GetProperty("403").GetProperty("content").GetProperty("application/json").GetProperty("schema").GetProperty("$ref").GetString();
        Assert.Equal(demandRef, secretDemandRef);

        var capture = Environment.GetEnvironmentVariable("GAMEGUILD_CREDENTIAL_STEPUP_OPENAPI_CAPTURE");
        if (!string.IsNullOrWhiteSpace(capture))
        {
            await File.WriteAllTextAsync(capture, raw);
        }
    }

    private static CreateApiKeyCommand Intent(string? receipt = null) => new()
    { Name = "synthetic-key-1191", Scopes = ["reports:read"], StepUpReceipt = receipt };
    private WebApplicationFactory<Program> CreateFactory() => CreateFactory(null);

    private WebApplicationFactory<Program> CreateFactory(KeyCommitBoundary? boundary) => fixture.CreateFactory(builder => builder.ConfigureTestServices(services =>
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
        db.AddRange(user, tenant, new TenantMember { Id = Guid.NewGuid(), UserId = user.Id, TenantId = tenant.Id, Role = "Member", IsActive = true },
            session, other, new UserMfaConfiguration { Id = Guid.NewGuid(), UserId = user.Id, IsEnabled = enabled, IsSetupComplete = complete, CreatedAt = now, UpdatedAt = now });
        await db.SaveChangesAsync();
        var code = Convert.ToHexString(RandomNumberGenerator.GetBytes(12));
        await provider.GetRequiredService<IBackupCodeMfaService>().StoreBackupCodesForSetupAsync(user.Id, [code]);
        return new(user.Id, user.Email, tenant.Id, user.TokenVersion, session.Id, other.Id, fingerprint, code);
    }

    private static async Task<StepUpAuthenticationRequiredResponse> DemandAsync(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync(Route, Intent());
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var demand = await response.Content.ReadFromJsonAsync<StepUpAuthenticationRequiredResponse>(JsonOptions);
        Assert.NotNull(demand);
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

    private sealed class KeyCommitBoundary(bool pauseUntilCancellation) : DbTransactionInterceptor
    {
        private Guid? _transaction;
        public Guid? ActorId { get; set; }
        public Guid StoredKeyId { get; private set; }
        public TaskCompletionSource Reached { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource RolledBack { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override async ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction,
            TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
        {
            if (ActorId is { } actor && eventData.Context is { } context)
            {
                var stored = context.ChangeTracker.Entries<ApiKey>()
                    .SingleOrDefault(row => row.Entity.UserId == actor);
                if (stored is { State: EntityState.Unchanged })
                {
                    _transaction = eventData.TransactionId;
                    StoredKeyId = stored.Entity.Id;
                    Reached.TrySetResult();
                    if (pauseUntilCancellation)
                    {
                        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                    }
                    throw new InvalidOperationException("Synthetic failure after API-key storage before commit");
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
