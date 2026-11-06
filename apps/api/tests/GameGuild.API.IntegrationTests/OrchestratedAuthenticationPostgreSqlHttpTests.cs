using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GameGuild.Notifications;
using GameGuild.Notifications.Services.Email;
using GameGuild.API.Database;
using GameGuild.API.IntegrationTests.Infrastructure;
using GameGuild.Configuration.ApplicationLayer;
using GameGuild.Email;
using GameGuild.Identity.Authentication;
using GameGuild.Identity.Tenants;
using GameGuild.Identity.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Moq;
using Npgsql;

namespace GameGuild.API.IntegrationTests;

[Collection(ApiPostgreSqlCollection.Name)]
public sealed class OrchestratedAuthenticationPostgreSqlHttpTests(ApiPostgreSqlFixture fixture)
{
    private const string Root = "/v1/auth/orchestrated";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    [Fact]
    public async Task CompletionPersistsBoundCredentialsWhoseBearerWorksOnTheNextRequest()
    {
        using var factory = CreateFactory();
        var account = await SeedAsync(factory);
        using var client = factory.CreateClient();
        var flow = await InitiateAsync(client, account);
        Assert.Equal(new[] { AuthenticationStep.PrimaryCredential }, flow.RequiredSteps);
        using var response = await client.PostAsync(Root + "/" + flow.FlowId + ":complete", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var issued = await response.Content.ReadFromJsonAsync<SignInResponse>(Json);
        Assert.NotNull(issued);
        Assert.True(issued.Success);
        Assert.Equal(account.UserId, issued.UserId);
        Assert.Equal(account.TenantId, issued.TenantId);
        Assert.Equal(account.TenantId.ToString(), Assert.Single(
            new JwtSecurityTokenHandler().ReadJwtToken(issued.AccessToken).Claims, claim => claim.Type == "tenant_id").Value);
        Assert.NotEqual(Guid.Empty, issued.SessionId);
        // PostgreSQL timestamps persist microseconds; JWT/session capping may retain sub-microsecond CLR ticks.
        Assert.True(issued.AccessTokenExpiresAt <= issued.RefreshTokenExpiresAt.AddTicks(9));
        Assert.True(issued.ExpiresIn > 0);
        await AssertPersistedCredentialsAsync(factory, account.UserId, issued.SessionId, flow.FlowId);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", issued.AccessToken);
        using var stateResponse = await client.GetAsync("/v1/auth/session-state");
        Assert.Equal(HttpStatusCode.OK, stateResponse.StatusCode);
        var state = await stateResponse.Content.ReadFromJsonAsync<AuthSessionStateDto>(Json);
        Assert.NotNull(state);
        Assert.Equal(issued.SessionId, state.SessionId);
        Assert.True(state.SessionValid);
        using var replay = await client.PostAsync(Root + "/" + flow.FlowId + ":complete", null);
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
        await AssertPersistedCredentialsAsync(factory, account.UserId, issued.SessionId, flow.FlowId);
    }

    [Fact]
    public async Task ConcurrentHttpCompletionsIssueExactlyOnePersistedSessionAndRefreshRoot()
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        var barrier = new CompletionBarrier();
        using var factory = CreateFactory(barrier: barrier);
        var account = await SeedAsync(factory);
        using var client = factory.CreateClient();
        var flow = await InitiateAsync(client, account);
        var first = client.PostAsync(Root + "/" + flow.FlowId + ":complete", null, deadline.Token);
        Task<HttpResponseMessage>? second = null;
        try
        {
            var firstBackend = await barrier.FirstLocked.Task.WaitAsync(deadline.Token);
            second = client.PostAsync(Root + "/" + flow.FlowId + ":complete", null, deadline.Token);
            var secondBackend = await barrier.SecondEntered.Task.WaitAsync(deadline.Token);
            await WaitForDatabaseBlockAsync(secondBackend, firstBackend, deadline.Token);
            Assert.False(first.IsCompleted);
            Assert.False(second.IsCompleted);
        }
        finally { barrier.Release.TrySetResult(); }
        Assert.NotNull(second);
        var responses = await Task.WhenAll(first, second);
        try
        {
            Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
            Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Unauthorized);
            var issued = await responses.Single(response => response.StatusCode == HttpStatusCode.OK).Content.ReadFromJsonAsync<SignInResponse>(Json);
            Assert.NotNull(issued);
            await AssertPersistedCredentialsAsync(factory, account.UserId, issued.SessionId, flow.FlowId);
        }
        finally { foreach (var response in responses) { response.Dispose(); } }
    }

    [Fact]
    public async Task FailureAfterFinalizationSaveRollsBackFlowSessionAndRefreshTogether()
    {
        var fault = new FinalizationFault();
        using var factory = CreateFactory(fault: fault);
        var account = await SeedAsync(factory);
        using var client = factory.CreateClient();
        var flow = await InitiateAsync(client, account);
        using var response = await client.PostAsync(Root + "/" + flow.FlowId + ":complete", null);
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.True(fault.Persisted, "The injected failure must follow the actual finalization SaveChanges.");
        using var read = factory.Services.CreateScope();
        var db = read.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Empty(await db.Set<UserSession>().AsNoTracking().Where(row => row.UserId == account.UserId).ToListAsync());
        Assert.Empty(await db.Set<RefreshToken>().AsNoTracking().Where(row => row.UserId == account.UserId).ToListAsync());
        var storedFlow = await db.Set<AuthenticationFlowStateRecord>().AsNoTracking().SingleAsync(row => row.FlowId == flow.FlowId);
        Assert.Null(storedFlow.CompletedAt);
        Assert.Null(storedFlow.AbandonedAt);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("accessToken", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("refreshToken", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FiveInvalidRiskCodesCommitFailureCountsAndCannotIssueCredentials()
    {
        using var factory = CreateFactory(score: 90);
        var account = await SeedAsync(factory, enrolled: true);
        using var client = factory.CreateClient();
        var flow = await InitiateAsync(client, account);
        Assert.Contains(AuthenticationStep.MfaVerification, flow.RequiredSteps);
        Assert.Contains(AuthenticationStep.RiskChallenge, flow.RequiredSteps);
        using var mfa = await client.PostAsJsonAsync(Root + "/" + flow.FlowId + ":process-step", new
        {
            step = AuthenticationStep.MfaVerification.ToString(), backupCode = account.BackupCode
        });
        Assert.Equal(HttpStatusCode.OK, mfa.StatusCode);
        var code = await ReadRiskCodeAsync(factory, account.UserId);
        var wrong = code == "00000000" ? "11111111" : "00000000";
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            using var denial = await client.PostAsJsonAsync(Root + "/" + flow.FlowId + ":process-step", new
            {
                step = AuthenticationStep.RiskChallenge.ToString(), riskCode = wrong
            });
            Assert.Equal(HttpStatusCode.Unauthorized, denial.StatusCode);
            using var denialJson = JsonDocument.Parse(await denial.Content.ReadAsStringAsync());
            Assert.Equal(401, denialJson.RootElement.GetProperty("status").GetInt32());
            var denied = denialJson.RootElement.GetProperty("legacy").Deserialize<OrchestratedFlowStateResponse>(Json);
            Assert.NotNull(denied);
            Assert.False(denied.Success);
            Assert.Equal("InvalidRiskCode", denied.ErrorCode);
            using var read = factory.Services.CreateScope();
            var db = read.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var stored = await db.Set<AuthenticationFlowStateRecord>().AsNoTracking().SingleAsync(row => row.FlowId == flow.FlowId);
            using var json = JsonDocument.Parse(stored.StepDataJson!);
            Assert.Equal(attempt, json.RootElement.GetProperty("__riskChallenge").GetProperty("FailedAttempts").GetInt32());
        }
        using var correctAfterExhaustion = await client.PostAsJsonAsync(Root + "/" + flow.FlowId + ":process-step", new
        {
            step = AuthenticationStep.RiskChallenge.ToString(), riskCode = code
        });
        Assert.Equal(HttpStatusCode.Unauthorized, correctAfterExhaustion.StatusCode);
        using var finalRead = factory.Services.CreateScope();
        var context = finalRead.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Empty(await context.Set<UserSession>().Where(row => row.UserId == account.UserId).ToListAsync());
        Assert.Empty(await context.Set<RefreshToken>().Where(row => row.UserId == account.UserId).ToListAsync());
        Assert.NotNull((await context.Set<AuthenticationFlowStateRecord>().SingleAsync(row => row.FlowId == flow.FlowId)).AbandonedAt);
    }

    [Fact]
    public async Task HighRiskCompletionPersistsMfaEvidenceAfterTheDedicatedEmailProof()
    {
        using var factory = CreateFactory(score: 90);
        var account = await SeedAsync(factory, enrolled: true);
        using var client = factory.CreateClient();
        var flow = await InitiateAsync(client, account);
        using var mfa = await client.PostAsJsonAsync(Root + "/" + flow.FlowId + ":process-step", new
        {
            step = AuthenticationStep.MfaVerification.ToString(), backupCode = account.BackupCode
        });
        Assert.Equal(HttpStatusCode.OK, mfa.StatusCode);
        using var risk = await client.PostAsJsonAsync(Root + "/" + flow.FlowId + ":process-step", new
        {
            step = AuthenticationStep.RiskChallenge.ToString(), riskCode = await ReadRiskCodeAsync(factory, account.UserId)
        });
        Assert.Equal(HttpStatusCode.OK, risk.StatusCode);
        using var completion = await client.PostAsync(Root + "/" + flow.FlowId + ":complete", null);
        Assert.Equal(HttpStatusCode.OK, completion.StatusCode);
        var issued = await completion.Content.ReadFromJsonAsync<SignInResponse>(Json);
        Assert.NotNull(issued);
        Assert.Equal(account.TenantId.ToString(), Assert.Single(
            new JwtSecurityTokenHandler().ReadJwtToken(issued.AccessToken).Claims, claim => claim.Type == "tenant_id").Value);
        await AssertPersistedCredentialsAsync(factory, account.UserId, issued.SessionId, flow.FlowId);
        using var scope = factory.Services.CreateScope();
        var evidence = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Set<SessionMfaEvidence>()
            .AsNoTracking().SingleAsync(row => row.SessionId == issued.SessionId);
        Assert.Equal(account.UserId, evidence.SubjectId);
        Assert.Equal(account.TenantId, evidence.TenantId);
        Assert.Equal(MfaMethod.BackupCode, evidence.Method);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", issued.AccessToken);
        using var state = await client.GetAsync("/v1/auth/session-state");
        Assert.Equal(HttpStatusCode.OK, state.StatusCode);
        Assert.True((await state.Content.ReadFromJsonAsync<AuthSessionStateDto>(Json))!.MfaVerifiedInCurrentToken);
    }

    [Fact]
    public async Task RoleOnlyMfaWithoutAnEnrolledFactorIssuesComplianceMarkedCredentials()
    {
        using var factory = CreateFactory();
        var account = await SeedAsync(factory);
        await AssignElevatedRoleAsync(factory, account);
        using var client = factory.CreateClient();
        var flow = await InitiateAsync(client, account);
        Assert.DoesNotContain(AuthenticationStep.MfaVerification, flow.RequiredSteps);
        using var completion = await client.PostAsync(Root + "/" + flow.FlowId + ":complete", null);
        Assert.Equal(HttpStatusCode.OK, completion.StatusCode);
        var issued = await completion.Content.ReadFromJsonAsync<SignInResponse>(Json);
        Assert.NotNull(issued);
        Assert.True(issued.Success);
        Assert.True(issued.MfaCompliancePending);
        var claims = new JwtSecurityTokenHandler().ReadJwtToken(issued.AccessToken).Claims.ToList();
        Assert.Equal("true", Assert.Single(claims, claim => claim.Type == "mfa_compliance_pending").Value);
        Assert.Equal(account.TenantId.ToString(), Assert.Single(claims, claim => claim.Type == "tenant_id").Value);
        await AssertPersistedCredentialsAsync(factory, account.UserId, issued.SessionId, flow.FlowId);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", issued.AccessToken);
        using var state = await client.GetAsync("/v1/auth/session-state");
        Assert.Equal(HttpStatusCode.OK, state.StatusCode);
        Assert.False((await state.Content.ReadFromJsonAsync<AuthSessionStateDto>(Json))!.MfaVerifiedInCurrentToken);
    }

    [Fact]
    public async Task DefaultMfaRequirementStillRejectsAnUnenrolledElevatedSubject()
    {
        using var factory = CreateFactory();
        var account = await SeedAsync(factory);
        await AssignElevatedRoleAsync(factory, account);
        factory.Services.GetRequiredService<MfaOptions>().RequireMfaByDefault = true;
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync(Root + ":initiate", new
        {
            identifier = account.Email, password = account.Password,
            tenantId = account.TenantId, deviceFingerprint = account.Fingerprint
        });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await AssertNoCredentialsAsync(factory, account.UserId);
    }

    [Fact]
    public async Task EnrolledElevatedRoleStillRequiresMfaAtLowRisk()
    {
        using var factory = CreateFactory();
        var account = await SeedAsync(factory, enrolled: true);
        await AssignElevatedRoleAsync(factory, account);
        using var client = factory.CreateClient();
        var flow = await InitiateAsync(client, account);
        Assert.Contains(AuthenticationStep.MfaVerification, flow.RequiredSteps);
        Assert.DoesNotContain(AuthenticationStep.MfaVerification, flow.CompletedSteps);
        await AssertNoCredentialsAsync(factory, account.UserId);
    }

    [Fact]
    public async Task RiskStepUpStillRejectsAnUnenrolledElevatedSubject()
    {
        using var factory = CreateFactory(score: 90);
        var account = await SeedAsync(factory);
        await AssignElevatedRoleAsync(factory, account);
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync(Root + ":initiate", new
        {
            identifier = account.Email, password = account.Password,
            tenantId = account.TenantId, deviceFingerprint = account.Fingerprint
        });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await AssertNoCredentialsAsync(factory, account.UserId);
    }

    private static async Task AssignElevatedRoleAsync(WebApplicationFactory<Program> factory, Account account)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var membership = await db.Set<TenantMember>().SingleAsync(row =>
            row.UserId == account.UserId && row.TenantId == account.TenantId);
        membership.Role = "Admin";
        await db.SaveChangesAsync();
        var policy = await scope.ServiceProvider.GetRequiredService<IMfaSubjectRequirementPolicy>()
            .EvaluateAsync(account.UserId, account.TenantId, CancellationToken.None);
        Assert.True(policy.RequiresMfa);
        Assert.False(policy.RequiredByDefault);
    }

    private static async Task AssertNoCredentialsAsync(WebApplicationFactory<Program> factory, Guid userId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Empty(await db.Set<UserSession>().AsNoTracking().Where(row => row.UserId == userId).ToListAsync());
        Assert.Empty(await db.Set<RefreshToken>().AsNoTracking().Where(row => row.UserId == userId).ToListAsync());
    }

    [Theory]
    [InlineData("inactive-account")]
    [InlineData("token-version")]
    [InlineData("inactive-tenant")]
    [InlineData("revoked-membership")]
    [InlineData("new-mfa-enrollment")]
    [InlineData("changed-mfa-policy")]
    [InlineData("expired")]
    [InlineData("legacy-unbound")]
    public async Task CompletionRechecksPersistedAccountTenantPolicyAndFlowBinding(string change)
    {
        using var factory = CreateFactory();
        var account = await SeedAsync(factory);
        using var client = factory.CreateClient();
        var flow = await InitiateAsync(client, account);
        using (var mutation = factory.Services.CreateScope())
        {
            var db = mutation.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            switch (change)
            {
                case "inactive-account": (await db.Set<User>().SingleAsync(row => row.Id == account.UserId)).Deactivate(); break;
                case "token-version": (await db.Set<User>().SingleAsync(row => row.Id == account.UserId)).IncrementTokenVersion(); break;
                case "inactive-tenant": (await db.Set<Tenant>().SingleAsync(row => row.Id == account.TenantId)).IsActive = false; break;
                case "revoked-membership":
                    (await db.Set<TenantMember>().SingleAsync(row => row.UserId == account.UserId && row.TenantId == account.TenantId)).IsActive = false;
                    break;
                case "new-mfa-enrollment":
                    db.Add(new UserMfaConfiguration { Id = Guid.NewGuid(), UserId = account.UserId, IsEnabled = true, IsSetupComplete = true });
                    break;
                case "changed-mfa-policy": mutation.ServiceProvider.GetRequiredService<IOptions<MfaOptions>>().Value.RequireMfaByDefault = true; break;
                case "expired": (await db.Set<AuthenticationFlowStateRecord>().SingleAsync(row => row.FlowId == flow.FlowId)).ExpiresAt = DateTime.UtcNow.AddSeconds(-1); break;
                case "legacy-unbound": (await db.Set<AuthenticationFlowStateRecord>().SingleAsync(row => row.FlowId == flow.FlowId)).StepDataJson = "{}"; break;
                default: throw new ArgumentOutOfRangeException(nameof(change));
            }
            await db.SaveChangesAsync();
        }
        using var completion = await client.PostAsync(Root + "/" + flow.FlowId + ":complete", null);
        Assert.Equal(HttpStatusCode.Unauthorized, completion.StatusCode);
        using var read = factory.Services.CreateScope();
        var context = read.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Empty(await context.Set<UserSession>().Where(row => row.UserId == account.UserId).ToListAsync());
        Assert.Empty(await context.Set<RefreshToken>().Where(row => row.UserId == account.UserId).ToListAsync());
        Assert.Null((await context.Set<AuthenticationFlowStateRecord>().SingleAsync(row => row.FlowId == flow.FlowId)).CompletedAt);
    }

    [Fact]
    public async Task OpenApiDocumentsDedicatedRiskProofWithoutLeakingPrivateFlowState()
    {
        using var factory = fixture.CreateFactory(builder => builder.UseEnvironment("Development"));
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");
        var proof = Assert.Single(schemas.EnumerateObject(), schema => schema.Name.EndsWith("_" + nameof(ProcessOrchestratedAuthenticationStepRequestDto), StringComparison.Ordinal));
        Assert.True(proof.Value.GetProperty("properties").TryGetProperty("riskCode", out _));
        var state = Assert.Single(schemas.EnumerateObject(), schema => schema.Name.EndsWith("_" + nameof(OrchestratedFlowStateResponse), StringComparison.Ordinal));
        var properties = state.Value.GetProperty("properties");
        Assert.Equal("boolean", properties.GetProperty("success").GetProperty("type").GetString());
        Assert.True(properties.TryGetProperty("errorCode", out _));
        Assert.False(properties.TryGetProperty("stepData", out _));
        Assert.False(properties.TryGetProperty("__serverBinding", out _));
        var capture = Environment.GetEnvironmentVariable("GAMEGUILD_ORCHESTRATED_OPENAPI_CAPTURE");
        if (!string.IsNullOrWhiteSpace(capture))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(capture))!);
            await File.WriteAllTextAsync(capture, json);
        }
    }

    private async Task WaitForDatabaseBlockAsync(int waitingBackend, int holdingBackend, CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync(ct);
        await using var command = new NpgsqlCommand(
            "SELECT @holder = ANY(pg_blocking_pids(@waiter))", connection);
        command.Parameters.AddWithValue("holder", holdingBackend);
        command.Parameters.AddWithValue("waiter", waitingBackend);
        while (await command.ExecuteScalarAsync(ct) is not true)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(50), ct);
        }
    }

    private WebApplicationFactory<Program> CreateFactory() => CreateFactory(null, null, 0);
    private WebApplicationFactory<Program> CreateFactory(FinalizationFault fault) => CreateFactory(fault, null, 0);
    private WebApplicationFactory<Program> CreateFactory(CompletionBarrier barrier) => CreateFactory(null, barrier, 0);
    private WebApplicationFactory<Program> CreateFactory(int score) => CreateFactory(null, null, score);

    private WebApplicationFactory<Program> CreateFactory(FinalizationFault? fault, CompletionBarrier? barrier, int score) =>
        fixture.CreateFactory(builder => builder.ConfigureTestServices(services =>
        {
            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            });
            services.PostConfigure<EmailDeliveryOptions>(options => options.Enabled = false);
            services.PostConfigure<MfaOptions>(options => { options.Enabled = true; options.RequireMfaByDefault = false; });
            services.RemoveAll<MfaOptions>();
            services.AddSingleton(provider => provider.GetRequiredService<IOptions<MfaOptions>>().Value);
            var risk = new Mock<IAuthenticationAnomalyDetectionService>();
            risk.Setup(service => service.AnalyzeLoginAttemptAsync(It.IsAny<AuthenticationAttemptContext>()))
                .ReturnsAsync(new AuthenticationAnomalyResult { RiskScore = score, RiskLevel = score >= 70 ? RiskLevel.High : RiskLevel.Low });
            risk.Setup(service => service.AnalyzeBehavioralPatternsAsync(It.IsAny<Guid>(), It.IsAny<AuthenticationAttemptContext>()))
                .ReturnsAsync(new BehavioralAnalysisResult { MatchesTypicalPattern = true, RiskLevel = RiskLevel.Low });
            services.RemoveAll<IAuthenticationAnomalyDetectionService>();
            services.AddSingleton(risk.Object);
            if (fault is not null || barrier is not null)
            {
                services.RemoveAll<IAuthenticationFlowStateRepository>();
                services.AddScoped<IAuthenticationFlowStateRepository>(provider => new ObservedFlowRepository(
                    new AuthenticationFlowStateRepository(provider.GetRequiredService<IApplicationDbContext>()),
                    provider.GetRequiredService<IApplicationDbContext>(), fault, barrier));
            }
        }));

    private static Task<Account> SeedAsync(WebApplicationFactory<Program> factory) => SeedAsync(factory, false);

    private static async Task<Account> SeedAsync(WebApplicationFactory<Program> factory, bool enrolled)
    {
        using var scope = factory.Services.CreateScope();
        var provider = scope.ServiceProvider;
        var marker = Guid.NewGuid().ToString("N");
        var password = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
        var user = User.CreateWithPassword("orchestrated-" + marker + "@example.test", "Synthetic orchestrated account",
            provider.GetRequiredService<IPasswordHasher>().HashPassword(password), "orchestrated-" + marker);
        user.VerifyEmail();
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Orchestrated " + marker, Slug = "orchestrated-" + marker,
            AdminEmail = user.Email, IsActive = true };
        var fingerprint = Guid.NewGuid().ToString("N");
        var now = DateTime.UtcNow;
        var db = provider.GetRequiredService<ApplicationDbContext>();
        db.AddRange(user, tenant, new TenantMember { Id = Guid.NewGuid(), UserId = user.Id, TenantId = tenant.Id, Role = "Member", IsActive = true },
            new TrustedDevice { Id = Guid.NewGuid(), UserId = user.Id, DeviceFingerprint = fingerprint, DeviceName = "Synthetic known device",
                DeviceInfo = "{}", IsActive = true, CreatedAt = now, UpdatedAt = now, TrustedAt = now, LastUsedAt = now, ExpiresAt = now.AddDays(1) });
        var backup = enrolled ? Convert.ToHexString(RandomNumberGenerator.GetBytes(6)) : null;
        if (backup is not null)
        {
            db.Add(new UserMfaConfiguration { Id = Guid.NewGuid(), UserId = user.Id, IsEnabled = true, IsSetupComplete = true,
                BackupCodes = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(backup))) });
        }
        await db.SaveChangesAsync();
        return new(user.Id, user.Email, password, tenant.Id, fingerprint, backup);
    }

    private static async Task<string> ReadRiskCodeAsync(WebApplicationFactory<Program> factory, Guid userId)
    {
        using var scope = factory.Services.CreateScope();
        var notification = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Set<Notification>()
            .AsNoTracking().SingleAsync(row => row.RecipientId == userId && row.Type == NotificationType.EmailCode);
        Assert.StartsWith(NotificationMetadataProtector.ProtectedPrefix, notification.Metadata!);
        var metadata = scope.ServiceProvider.GetRequiredService<NotificationMetadataProtector>().GetForRendering(notification, NotificationType.EmailCode);
        using var json = JsonDocument.Parse(metadata!);
        return json.RootElement.GetProperty("code").GetString()!;
    }

    private static async Task<OrchestratedFlowStateResponse> InitiateAsync(HttpClient client, Account account)
    {
        using var response = await client.PostAsJsonAsync(Root + ":initiate", new
        {
            identifier = account.Email, password = account.Password, tenantId = account.TenantId, deviceFingerprint = account.Fingerprint
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var state = await response.Content.ReadFromJsonAsync<OrchestratedFlowStateResponse>(Json);
        Assert.NotNull(state);
        return state;
    }

    private static async Task AssertPersistedCredentialsAsync(WebApplicationFactory<Program> factory, Guid userId, Guid sessionId, Guid flowId)
    {
        using var read = factory.Services.CreateScope();
        var db = read.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var session = Assert.Single(await db.Set<UserSession>().AsNoTracking().Where(row => row.UserId == userId).ToListAsync());
        var refresh = Assert.Single(await db.Set<RefreshToken>().AsNoTracking().Where(row => row.UserId == userId).ToListAsync());
        Assert.Equal(sessionId, session.Id);
        Assert.Equal(sessionId, refresh.SessionId);
        Assert.Null(refresh.ParentTokenId);
        Assert.True(session.IsActive);
        Assert.True(refresh.IsActive);
        Assert.NotNull((await db.Set<AuthenticationFlowStateRecord>().AsNoTracking().SingleAsync(row => row.FlowId == flowId)).CompletedAt);
    }

    private sealed record Account(Guid UserId, string Email, string Password, Guid TenantId, string Fingerprint, string? BackupCode);
    private sealed class FinalizationFault { public bool Persisted { get; set; } }
    private sealed class CompletionBarrier
    {
        private int _entrants;
        public int NextEntrant() => Interlocked.Increment(ref _entrants);
        public TaskCompletionSource<int> FirstLocked { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<int> SecondEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
    private sealed class ObservedFlowRepository(IAuthenticationFlowStateRepository inner, IApplicationDbContext context, FinalizationFault? fault, CompletionBarrier? barrier)
        : IAuthenticationFlowStateRepository
    {
        public Task<AuthenticationFlowStateRecord> CreateAsync(AuthenticationFlowStateRecord row, CancellationToken ct) => inner.CreateAsync(row, ct);
        public Task<AuthenticationFlowStateRecord?> GetByFlowIdAsync(Guid id, CancellationToken ct) => inner.GetByFlowIdAsync(id, ct);
        public async Task<AuthenticationFlowStateRecord?> GetForUpdateAsync(Guid id, CancellationToken ct)
        {
            var ordinal = barrier is null ? 0 : barrier.NextEntrant();
            var backend = barrier is null ? 0 : await ((DbContext)context).Database
                .SqlQueryRaw<int>("SELECT pg_backend_pid() AS \"Value\"").SingleAsync(ct);
            if (ordinal == 2) { barrier!.SecondEntered.TrySetResult(backend); }
            var row = await inner.GetForUpdateAsync(id, ct);
            if (ordinal == 1)
            {
                barrier!.FirstLocked.TrySetResult(backend);
                await barrier.Release.Task.WaitAsync(ct);
            }
            return row;
        }
        public async Task<AuthenticationFlowStateRecord> UpdateAsync(AuthenticationFlowStateRecord row, CancellationToken ct)
        {
            var result = await inner.UpdateAsync(row, ct);
            if (fault is not null && row.CompletedAt is not null)
            {
                fault.Persisted = true;
                throw new InvalidOperationException("Synthetic failure after persisted flow finalization");
            }
            return result;
        }
        public Task AbandonAsync(Guid id, DateTime when, CancellationToken ct) => inner.AbandonAsync(id, when, ct);
        public Task DeleteExpiredAsync(DateTime before, CancellationToken ct) => inner.DeleteExpiredAsync(before, ct);
    }
}
