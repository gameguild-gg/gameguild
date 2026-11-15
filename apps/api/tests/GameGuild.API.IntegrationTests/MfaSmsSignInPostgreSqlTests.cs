using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using GameGuild.API.Database;
using GameGuild.API.IntegrationTests.Infrastructure;
using GameGuild.Configuration.ApplicationLayer;
using GameGuild.CQRS;
using GameGuild.Email;
using GameGuild.Identity.Authentication;
using GameGuild.Identity.Tenants;
using GameGuild.Identity.Users;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace GameGuild.API.IntegrationTests;

[Collection(ApiPostgreSqlCollection.Name)]
public sealed class MfaSmsSignInPostgreSqlTests(ApiPostgreSqlFixture fixture)
{
    [Fact]
    public async Task VerifiedSmsEnrollmentIsRecognizedBeforePasswordCredentialsCanBeIssued()
    {
        var delivery = new SmsDelivery();
        using var factory = CreateFactory(delivery);
        var subject = await SeedAsync(factory, smsAlreadyVerified: false);
        using (var enrollmentScope = factory.Services.CreateScope())
        {
            var sms = enrollmentScope.ServiceProvider.GetRequiredService<ISmsMfaService>();
            Assert.True((await sms.StartEnrollmentAsync(subject.UserId, "+15555550123")).Success);
            Assert.NotNull(delivery.LastCode);
            Assert.True((await sms.CompleteEnrollmentAsync(subject.UserId, delivery.LastCode)).Success);
            var database = enrollmentScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var stored = await database.Set<UserMfaConfiguration>().AsNoTracking().SingleAsync(row => row.UserId == subject.UserId);
            Assert.True(stored.IsEnabled);
            Assert.True(stored.IsSetupComplete);
            var signInSubject = await enrollmentScope.ServiceProvider.GetRequiredService<ISignInMfaSubjectReader>()
                .ReadCurrentAsync(subject.UserId, CancellationToken.None);
            Assert.NotNull(signInSubject);
            Assert.True(signInSubject.HasEnrolledMfa);
        }
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/v1/auth/sign-in", new { subject.Email, subject.Password, subject.TenantId });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        AssertPendingWithoutCredentials(body.RootElement);
        using var verify = factory.Services.CreateScope();
        Assert.Equal(0, await verify.ServiceProvider.GetRequiredService<ApplicationDbContext>()
            .Set<UserSession>().CountAsync(row => row.UserId == subject.UserId));
    }

    [Fact]
    public async Task UserIdAloneCannotEnumerateSmsEnrollmentOrTriggerDelivery()
    {
        var delivery = new SmsDelivery();
        using var factory = CreateFactory(delivery);
        var subject = await SeedAsync(factory, smsAlreadyVerified: true);
        using var client = factory.CreateClient();
        using var unknown = await client.PostAsJsonAsync("/v1/auth/mfa/sms:challenge", new { userId = Guid.NewGuid() });
        using var enrolled = await client.PostAsJsonAsync("/v1/auth/mfa/sms:challenge", new { userId = subject.UserId });
        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
        Assert.Equal(unknown.StatusCode, enrolled.StatusCode);
        var unknownPayload = JsonNode.Parse(await unknown.Content.ReadAsStringAsync());
        var enrolledPayload = JsonNode.Parse(await enrolled.Content.ReadAsStringAsync());
        Assert.IsType<JsonObject>(unknownPayload);
        Assert.IsType<JsonObject>(enrolledPayload);
        static void RemoveRequestIds(JsonNode? payload)
        {
            if (payload is not JsonObject obj) { return; }
            obj.Remove("traceId");
            obj.Remove("correlationId");
            if (obj["extensions"] is JsonObject extensions)
            {
                extensions.Remove("traceId");
                extensions.Remove("correlationId");
            }
        }
        RemoveRequestIds(unknownPayload);
        RemoveRequestIds(enrolledPayload);
        Assert.True(JsonNode.DeepEquals(unknownPayload, enrolledPayload), "Generic denial payloads differ beyond request metadata.");
        Assert.Equal(0, delivery.Count);
    }

    [Fact]
    public async Task SmsFirstFactorBoundChallengeCompletesOnceAndPreservesMfaAcrossRefresh()
    {
        var delivery = new SmsDelivery();
        using var factory = CreateFactory(delivery);
        var subject = await SeedAsync(factory, smsAlreadyVerified: true);
        using var client = factory.CreateClient();
        using var anonymous = await client.GetAsync("/v1/auth/session-state");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        using var wrongPassword = await client.PostAsJsonAsync("/v1/auth/sign-in", new
        {
            subject.Email, password = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)), subject.TenantId
        });
        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        using var firstFactor = await client.PostAsJsonAsync("/v1/auth/sign-in", new { subject.Email, subject.Password, subject.TenantId });
        Assert.Equal(HttpStatusCode.OK, firstFactor.StatusCode);
        using var pendingJson = JsonDocument.Parse(await firstFactor.Content.ReadAsStringAsync());
        var pending = pendingJson.RootElement;
        AssertPendingWithoutCredentials(pending);
        Assert.Contains(pending.GetProperty("availableMethods").EnumerateArray(), method =>
            string.Equals(method.GetString(), "Sms", StringComparison.OrdinalIgnoreCase));
        var bearer = pending.GetProperty("mfaToken").GetString();
        Assert.True(SignInMfaChallengeToken.TryHash(bearer, out _));

        // The account and tenant are resolved from the first-factor continuation.
        // Extra legacy client identifiers cannot redirect delivery to another user.
        using var requested = await client.PostAsJsonAsync("/v1/auth/mfa/sms:challenge", new
        {
            mfaToken = bearer, userId = Guid.NewGuid(), tenantId = Guid.NewGuid()
        });
        Assert.Equal(HttpStatusCode.OK, requested.StatusCode);
        Assert.Equal(subject.UserId, delivery.LastUserId);
        Assert.Equal(1, delivery.Count);
        Assert.NotNull(delivery.LastCode);
        using var completed = await client.PostAsJsonAsync("/v1/auth/mfa/sign-in/complete", new
        {
            mfaToken = bearer, code = delivery.LastCode, method = "Sms"
        });
        Assert.Equal(HttpStatusCode.OK, completed.StatusCode);
        using var successJson = JsonDocument.Parse(await completed.Content.ReadAsStringAsync());
        var result = successJson.RootElement;
        Assert.True(result.GetProperty("success").GetBoolean());
        Assert.False(result.GetProperty("requiresMfa").GetBoolean());
        Assert.Equal(subject.UserId, result.GetProperty("userId").GetGuid());
        Assert.Equal(subject.TenantId, result.GetProperty("tenantId").GetGuid());
        var access = result.GetProperty("accessToken").GetString();
        var token = new JwtSecurityTokenHandler().ReadJwtToken(access);
        Assert.Contains(token.Claims, claim => claim.Type == "amr" && claim.Value == "mfa");
        Assert.Contains(token.Claims, claim => claim.Type == "mfa_verified" && claim.Value == "true");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access);
        using var protectedState = await client.GetAsync("/v1/auth/session-state");
        Assert.Equal(HttpStatusCode.OK, protectedState.StatusCode);
        using var stateJson = JsonDocument.Parse(await protectedState.Content.ReadAsStringAsync());
        Assert.True(stateJson.RootElement.GetProperty("mfaVerifiedInCurrentToken").GetBoolean());
        client.DefaultRequestHeaders.Authorization = null;
        using var replay = await client.PostAsJsonAsync("/v1/auth/mfa/sign-in/complete", new
        {
            mfaToken = bearer, code = delivery.LastCode, method = "Sms"
        });
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
        using var resendAfterConsumption = await client.PostAsJsonAsync("/v1/auth/mfa/sms:challenge", new { mfaToken = bearer });
        Assert.Equal(HttpStatusCode.BadRequest, resendAfterConsumption.StatusCode);
        Assert.Equal(1, delivery.Count);
        using var refreshed = await client.PostAsJsonAsync("/v1/auth/tokens:refresh", new
        {
            refreshToken = result.GetProperty("refreshToken").GetString(), subject.TenantId
        });
        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
        using var refreshJson = JsonDocument.Parse(await refreshed.Content.ReadAsStringAsync());
        var refresh = refreshJson.RootElement;
        Assert.True(refresh.GetProperty("success").GetBoolean());
        var renewed = new JwtSecurityTokenHandler().ReadJwtToken(refresh.GetProperty("accessToken").GetString());
        Assert.Contains(renewed.Claims, claim => claim.Type == "mfa_verified" && claim.Value == "true");
        Assert.Equal(result.GetProperty("sessionId").GetGuid(), refresh.GetProperty("sessionId").GetGuid());
        using var final = factory.Services.CreateScope();
        var database = final.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(1, await database.Set<UserSession>().CountAsync(row => row.UserId == subject.UserId));
        var challenge = await database.Set<SignInMfaChallenge>().SingleAsync(row => row.SubjectId == subject.UserId);
        Assert.NotNull(challenge.ConsumedAt);
        Assert.Equal(MfaMethod.Sms, challenge.VerificationMethod);
    }

    [Fact]
    public async Task FailedBoundProofsPersistTheSharedAccountBudgetAndBlockFreshSignIn()
    {
        var delivery = new SmsDelivery();
        using var factory = CreateFactory(delivery);
        var subject = await SeedAsync(factory, smsAlreadyVerified: true);
        using var client = factory.CreateClient();
        var bearer = await BeginSmsAsync(client, subject);
        using var sent = await client.PostAsJsonAsync("/v1/auth/mfa/sms:challenge", new { mfaToken = bearer });
        Assert.Equal(HttpStatusCode.OK, sent.StatusCode);
        Assert.NotNull(delivery.LastCode);
        var wrong = (delivery.LastCode[0] == '0' ? "1" : "0") + delivery.LastCode[1..];
        for (var attempt = 0; attempt < 3; attempt++)
        {
            using var denied = await client.PostAsJsonAsync("/v1/auth/mfa/sign-in/complete", new { mfaToken = bearer, code = wrong, method = "Sms" });
            Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
        }
        using var verify = factory.Services.CreateScope();
        var database = verify.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var configuration = await database.Set<UserMfaConfiguration>().AsNoTracking().SingleAsync(row => row.UserId == subject.UserId);
        Assert.Equal(3, configuration.FailedAttempts);
        Assert.True(configuration.LockedOutUntil > DateTime.UtcNow);
        Assert.Equal(3, await database.Set<MfaAttempt>().CountAsync(row => row.UserId == subject.UserId && !row.IsSuccessful && row.Method == MfaMethod.Sms));
        using var freshPassword = await client.PostAsJsonAsync("/v1/auth/sign-in", new { subject.Email, subject.Password, subject.TenantId });
        // The existing LocalSignIn mapper returns the service's denial DTO as HTTP200;
        // enforce the actual security boundary instead of inferring an authentication success.
        Assert.Equal(HttpStatusCode.OK, freshPassword.StatusCode);
        using var freshPasswordBody = JsonDocument.Parse(await freshPassword.Content.ReadAsStringAsync());
        var freshResult = freshPasswordBody.RootElement;
        Assert.False(freshResult.GetProperty("success").GetBoolean());
        Assert.False(freshResult.GetProperty("requiresMfa").GetBoolean());
        static bool HasNoCredential(JsonElement result, string name) =>
            !result.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null ||
            value.ValueKind == JsonValueKind.String && string.IsNullOrEmpty(value.GetString());
        Assert.True(HasNoCredential(freshResult, "accessToken"));
        Assert.True(HasNoCredential(freshResult, "refreshToken"));
        Assert.True(HasNoCredential(freshResult, "mfaToken"));
        Assert.False(freshResult.GetProperty("requiresStepUp").GetBoolean());
        Assert.True(HasNoCredential(freshResult, "tempToken"));
        Assert.True(HasNoCredential(freshResult, "stepUpToken"));
        Assert.Equal(Guid.Empty, freshResult.GetProperty("sessionId").GetGuid());
        using var correctAfterLock = await client.PostAsJsonAsync("/v1/auth/mfa/sign-in/complete", new { mfaToken = bearer, code = delivery.LastCode, method = "Sms" });
        Assert.Equal(HttpStatusCode.Unauthorized, correctAfterLock.StatusCode);
        Assert.Equal(0, await database.Set<UserSession>().CountAsync(row => row.UserId == subject.UserId));
        Assert.Equal(1, delivery.Count);
    }

    [Fact]
    public async Task ASecondContinuationCannotUseTheFirstContinuationsCode()
    {
        var delivery = new SmsDelivery();
        using var factory = CreateFactory(delivery);
        var subject = await SeedAsync(factory, smsAlreadyVerified: true);
        using var client = factory.CreateClient();
        var first = await BeginSmsAsync(client, subject);
        var second = await BeginSmsAsync(client, subject);
        Assert.NotEqual(first, second);
        using var sent = await client.PostAsJsonAsync("/v1/auth/mfa/sms:challenge", new { mfaToken = first });
        Assert.Equal(HttpStatusCode.OK, sent.StatusCode);
        Assert.NotNull(delivery.LastCode);
        using var wrongBinding = await client.PostAsJsonAsync("/v1/auth/mfa/sign-in/complete", new { mfaToken = second, code = delivery.LastCode, method = "Sms" });
        Assert.Equal(HttpStatusCode.Unauthorized, wrongBinding.StatusCode);
        using var original = await client.PostAsJsonAsync("/v1/auth/mfa/sign-in/complete", new { mfaToken = first, code = delivery.LastCode, method = "Sms" });
        Assert.Equal(HttpStatusCode.OK, original.StatusCode);
        using var result = JsonDocument.Parse(await original.Content.ReadAsStringAsync());
        Assert.True(result.RootElement.GetProperty("success").GetBoolean());
        using var verify = factory.Services.CreateScope();
        var database = verify.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(1, await database.Set<UserSession>().CountAsync(row => row.UserId == subject.UserId));
        Assert.Equal(1, await database.Set<SignInMfaChallenge>().CountAsync(row => row.SubjectId == subject.UserId && row.ConsumedAt != null));
        Assert.Equal(1, delivery.Count);
    }

    [Fact]
    public async Task ConcurrentSmsCompletionCreatesExactlyOneSession()
    {
        var delivery = new SmsDelivery();
        using var factory = CreateFactory(delivery);
        var subject = await SeedAsync(factory, smsAlreadyVerified: true);
        using var client = factory.CreateClient();
        var bearer = await BeginSmsAsync(client, subject);
        using var sent = await client.PostAsJsonAsync("/v1/auth/mfa/sms:challenge", new { mfaToken = bearer });
        Assert.Equal(HttpStatusCode.OK, sent.StatusCode);
        Assert.NotNull(delivery.LastCode);
        var responses = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => client.PostAsJsonAsync("/v1/auth/mfa/sign-in/complete",
            new { mfaToken = bearer, code = delivery.LastCode, method = "Sms" })));
        try
        {
            Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
            Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Unauthorized);
            using var verify = factory.Services.CreateScope();
            var database = verify.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Equal(1, await database.Set<UserSession>().CountAsync(row => row.UserId == subject.UserId));
            Assert.Equal(1, await database.Set<SignInMfaChallenge>().CountAsync(row => row.SubjectId == subject.UserId && row.ConsumedAt != null));
        }
        finally
        {
            foreach (var response in responses) { response.Dispose(); }
        }
    }

    [Theory]
    [InlineData("version")]
    [InlineData("expired")]
    public async Task StaleContinuationCannotSendToTheEnrolledPhone(string fault)
    {
        var delivery = new SmsDelivery();
        using var factory = CreateFactory(delivery);
        var subject = await SeedAsync(factory, smsAlreadyVerified: true);
        using var client = factory.CreateClient();
        var bearer = await BeginSmsAsync(client, subject);
        using (var change = factory.Services.CreateScope())
        {
            var database = change.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            if (fault == "version")
            {
                var user = await database.Set<User>().SingleAsync(row => row.Id == subject.UserId);
                user.TokenVersion++;
            }
            else
            {
                var challenge = await database.Set<SignInMfaChallenge>().SingleAsync(row => row.SubjectId == subject.UserId);
                // Keep the persisted lifetime valid even on a fast host; only freshness fails.
                var expiredAt = DateTimeOffset.UtcNow.AddSeconds(-1);
                challenge.CreatedAt = expiredAt.AddMinutes(-1);
                challenge.ExpiresAt = expiredAt;
            }
            await database.SaveChangesAsync();
        }
        using var denied = await client.PostAsJsonAsync("/v1/auth/mfa/sms:challenge", new { mfaToken = bearer });
        Assert.Equal(HttpStatusCode.BadRequest, denied.StatusCode);
        Assert.Equal(0, delivery.Count);
        using var verify = factory.Services.CreateScope();
        Assert.Equal(0, await verify.ServiceProvider.GetRequiredService<ApplicationDbContext>().Set<UserSession>().CountAsync(row => row.UserId == subject.UserId));
    }

    private static async Task<string> BeginSmsAsync(HttpClient client, SmsSubject subject)
    {
        using var response = await client.PostAsJsonAsync("/v1/auth/sign-in", new { subject.Email, subject.Password, subject.TenantId });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        AssertPendingWithoutCredentials(body.RootElement);
        var bearer = body.RootElement.GetProperty("mfaToken").GetString();
        Assert.True(SignInMfaChallengeToken.TryHash(bearer, out _));
        return bearer!;
    }

    private WebApplicationFactory<Program> CreateFactory(SmsDelivery delivery)
    {
        var risk = new Mock<IAuthenticationAnomalyDetectionService>(MockBehavior.Strict);
        risk.Setup(service => service.AnalyzeLoginAttemptAsync(It.IsAny<AuthenticationAttemptContext>()))
            .ReturnsAsync(new AuthenticationAnomalyResult { RiskLevel = RiskLevel.Low });
        risk.Setup(service => service.AnalyzeBehavioralPatternsAsync(It.IsAny<Guid>(), It.IsAny<AuthenticationAttemptContext>()))
            .ReturnsAsync(new BehavioralAnalysisResult { MatchesTypicalPattern = true, RiskLevel = RiskLevel.Low });
        return fixture.CreateFactory(builder => builder.ConfigureTestServices(services =>
        {
            services.PostConfigure<Microsoft.AspNetCore.Authentication.AuthenticationOptions>(options =>
            {
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            });
            services.PostConfigure<MfaOptions>(options => { options.Enabled = true; options.RequireMfaByDefault = false; options.MaxFailedAttempts = 3; });
            services.PostConfigure<EmailDeliveryOptions>(options => options.Enabled = false);
            services.RemoveAll<MfaOptions>();
            services.AddSingleton(provider => provider.GetRequiredService<IOptions<MfaOptions>>().Value);
            services.RemoveAll<IMfaAttemptTrackingService>();
            services.AddScoped<IMfaAttemptTrackingService>(provider => ActivatorUtilities.CreateInstance<MfaAttemptTrackingService>(
                provider, provider.GetRequiredService<IOptions<MfaOptions>>().Value));
            services.RemoveAll<IMfaService>();
            services.AddScoped<IMfaService, MfaService>();
            services.RemoveAll<ISmsMfaService>();
            services.AddScoped<ISmsMfaService>(provider => new SmsMfaService(provider.GetRequiredService<ILogger<SmsMfaService>>(),
                provider.GetRequiredService<IMemoryCache>(), provider.GetRequiredService<IUserMfaConfigurationRepository>(), delivery.Publisher,
                provider.GetRequiredService<IMfaAttemptTrackingService>()));
            services.RemoveAll<IAuthenticationAnomalyDetectionService>();
            services.AddSingleton(risk.Object);
        }));
    }

    private static async Task<SmsSubject> SeedAsync(WebApplicationFactory<Program> factory, bool smsAlreadyVerified)
    {
        var marker = Guid.NewGuid().ToString("N");
        var requiredCharacters = new[]
        {
            (char)RandomNumberGenerator.GetInt32('A', 'Z' + 1),
            (char)RandomNumberGenerator.GetInt32('a', 'z' + 1),
            (char)RandomNumberGenerator.GetInt32('0', '9' + 1),
            (char)RandomNumberGenerator.GetInt32('!', '/' + 1)
        };
        var password = new string(requiredCharacters) + Convert.ToHexString(RandomNumberGenerator.GetBytes(20));
        var tenantId = Guid.NewGuid();
        using var scope = factory.Services.CreateScope();
        var provider = scope.ServiceProvider;
        var user = User.CreateWithPassword("sms-sign-in-" + marker + "@example.test", "SMS " + marker,
            provider.GetRequiredService<IPasswordHasher>().HashPassword(password), "sms-sign-in-" + marker);
        user.VerifyEmail();
        var database = provider.GetRequiredService<ApplicationDbContext>();
        database.Set<User>().Add(user);
        database.Set<Tenant>().Add(new Tenant { Id = tenantId, Name = "SMS " + marker, Slug = "sms-" + marker,
            AdminEmail = user.Email, IsActive = true });
        database.Set<TenantMember>().Add(new TenantMember { Id = Guid.NewGuid(), UserId = user.Id, TenantId = tenantId,
            Role = "Member", IsActive = true });
        var secret = new string(Enumerable.Range(0, 32).Select(_ => "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567"[RandomNumberGenerator.GetInt32(32)]).ToArray());
        database.Set<UserMfaConfiguration>().Add(new UserMfaConfiguration { Id = Guid.NewGuid(), UserId = user.Id,
            IsEnabled = smsAlreadyVerified, IsSetupComplete = smsAlreadyVerified, PreferredMethod = MfaMethod.Sms,
            SmsPhoneNumber = smsAlreadyVerified ? "+15555550123" : null,
            TotpSecretKey = provider.GetRequiredService<IEncryptionService>().Encrypt(secret),
            SetupExpiresAt = DateTime.UtcNow.AddMinutes(10) });
        await database.SaveChangesAsync();
        return new SmsSubject(user.Id, tenantId, user.Email, password);
    }

    private static void AssertPendingWithoutCredentials(JsonElement result)
    {
        Assert.False(result.GetProperty("success").GetBoolean());
        Assert.True(result.GetProperty("requiresMfa").GetBoolean());
        Assert.True(string.IsNullOrEmpty(result.GetProperty("accessToken").GetString()));
        Assert.True(string.IsNullOrEmpty(result.GetProperty("refreshToken").GetString()));
    }

    private sealed record SmsSubject(Guid UserId, Guid TenantId, string Email, string Password);

    private sealed class SmsDelivery
    {
        public string? LastCode { get; private set; }
        public Guid LastUserId { get; private set; }
        public int Count { get; private set; }
        public IPublisher Publisher { get; }

        public SmsDelivery()
        {
            var publisher = new Mock<IPublisher>(MockBehavior.Strict);
            publisher.Setup(value => value.Publish(It.IsAny<SmsMfaCodeRequestedNotification>(), It.IsAny<CancellationToken>()))
                .Callback<SmsMfaCodeRequestedNotification, CancellationToken>((notice, _) =>
                {
                    LastCode = notice.Code;
                    LastUserId = notice.UserId;
                    Count++;
                }).Returns(Task.CompletedTask);
            Publisher = publisher.Object;
        }
    }
}
