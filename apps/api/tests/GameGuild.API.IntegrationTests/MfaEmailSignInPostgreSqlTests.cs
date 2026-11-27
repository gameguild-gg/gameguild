using System.Buffers.Binary;
using System.Collections;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
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

/// <summary>
///     GAP-1 of the J2 email-MFA design (docs/design/j2-email-mfa-and-recovery-ui.md): a subject
///     whose only enrolled factor is Email must be able to complete sign-in. Mirrors the SMS
///     sign-in suite: enrolled-only completion, shared lockout budget on failed proofs, and
///     denial when Email is not among the enrolled methods.
/// </summary>
[Collection(ApiPostgreSqlCollection.Name)]
public sealed class MfaEmailSignInPostgreSqlTests(ApiPostgreSqlFixture fixture)
{
    [Fact]
    public async Task EmailOnlySubjectCompletesSignInWithAnEmailProofAndPreservesMfaAcrossRefresh()
    {
        var delivery = new EmailDelivery();
        using var factory = CreateFactory(delivery);
        var subject = await SeedAsync(factory, emailEnrolled: true);
        using var client = factory.CreateClient();
        using var firstFactor = await client.PostAsJsonAsync("/v1/auth/sign-in", new { subject.Email, subject.Password, subject.TenantId });
        Assert.Equal(HttpStatusCode.OK, firstFactor.StatusCode);
        using var pendingJson = JsonDocument.Parse(await firstFactor.Content.ReadAsStringAsync());
        var pending = pendingJson.RootElement;
        AssertPendingWithoutCredentials(pending);
        // The pending response carries the post-password-proof identifiers the web client needs
        // to drive the email challenge (design GAP-2 cites SignInMfaService.cs:335-336).
        Assert.Equal(subject.UserId, pending.GetProperty("userId").GetGuid());
        Assert.Equal(subject.Email, pending.GetProperty("email").GetString());
        Assert.Contains(pending.GetProperty("availableMethods").EnumerateArray(), method =>
            string.Equals(method.GetString(), "Email", StringComparison.OrdinalIgnoreCase));
        var bearer = pending.GetProperty("mfaToken").GetString();
        Assert.True(SignInMfaChallengeToken.TryHash(bearer, out _));

        using var sent = await client.PostAsJsonAsync("/v1/auth/mfa/email:challenge", new { userId = subject.UserId });
        Assert.Equal(HttpStatusCode.OK, sent.StatusCode);
        Assert.Equal(1, delivery.Count);
        Assert.Equal(subject.UserId, delivery.LastUserId);
        Assert.NotNull(delivery.LastCode);

        using var completed = await client.PostAsJsonAsync("/v1/auth/mfa/sign-in/complete", new
        {
            mfaToken = bearer, code = delivery.LastCode, method = "Email"
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

        using var replay = await client.PostAsJsonAsync("/v1/auth/mfa/sign-in/complete", new
        {
            mfaToken = bearer, code = delivery.LastCode, method = "Email"
        });
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);

        using var refreshed = await client.PostAsJsonAsync("/v1/auth/tokens:refresh", new
        {
            refreshToken = result.GetProperty("refreshToken").GetString(), subject.TenantId
        });
        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
        using var refreshJson = JsonDocument.Parse(await refreshed.Content.ReadAsStringAsync());
        Assert.True(refreshJson.RootElement.GetProperty("success").GetBoolean());
        var renewed = new JwtSecurityTokenHandler().ReadJwtToken(refreshJson.RootElement.GetProperty("accessToken").GetString());
        Assert.Contains(renewed.Claims, claim => claim.Type == "mfa_verified" && claim.Value == "true");

        using var verify = factory.Services.CreateScope();
        var database = verify.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(1, await database.Set<UserSession>().CountAsync(row => row.UserId == subject.UserId));
        var challenge = await database.Set<SignInMfaChallenge>().SingleAsync(row => row.SubjectId == subject.UserId);
        Assert.NotNull(challenge.ConsumedAt);
        Assert.Equal(MfaMethod.Email, challenge.VerificationMethod);
        Assert.Equal(1, delivery.Count);
    }

    [Fact]
    public async Task EmailEnrollmentCompletesOverHttpWithTheMailedCodeAlone()
    {
        // Settings enrollment (issue #1298 J2): email is added to an account that already
        // has a factor — the service needs an MFA configuration to enroll into — and its
        // completion contract carries the mailed code alone.
        var delivery = new EmailDelivery();
        using var factory = CreateFactory(delivery);
        var subject = await SeedAsync(factory, emailEnrolled: false);
        using var client = factory.CreateClient();

        // The account is protected by an authenticator, so sign-in pauses for its code.
        using var firstFactor = await client.PostAsJsonAsync("/v1/auth/sign-in", new { subject.Email, subject.Password, subject.TenantId });
        using var firstFactorBody = JsonDocument.Parse(await firstFactor.Content.ReadAsStringAsync());
        var pending = firstFactorBody.RootElement;
        AssertPendingWithoutCredentials(pending);
        var authenticatorCode = ComputeTotpCode(subject.TotpSecret!, factory.Services.GetRequiredService<MfaOptions>().TotpTimeStepSeconds);
        using var authenticated = await client.PostAsJsonAsync("/v1/auth/mfa/sign-in/complete", new
        {
            mfaToken = pending.GetProperty("mfaToken").GetString(), code = authenticatorCode, method = "Totp"
        });
        using var authenticatedBody = JsonDocument.Parse(await authenticated.Content.ReadAsStringAsync());
        var accessToken = authenticatedBody.RootElement.GetProperty("accessToken").GetString();
        Assert.False(string.IsNullOrEmpty(accessToken));
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        using var setup = await client.PostAsync("/v1/auth/mfa/email:setup", null);
        Assert.Equal(HttpStatusCode.OK, setup.StatusCode);
        Assert.Equal(subject.UserId, delivery.LastUserId);
        Assert.NotNull(delivery.LastCode);
        using var setupBody = JsonDocument.Parse(await setup.Content.ReadAsStringAsync());
        Assert.False(string.IsNullOrEmpty(setupBody.RootElement.GetProperty("emailMasked").GetString()));
        Assert.True(setupBody.RootElement.GetProperty("expiresInSeconds").GetInt32() > 0);

        using var completed = await client.PostAsJsonAsync("/v1/auth/mfa/email:complete", new { code = delivery.LastCode });
        Assert.Equal(HttpStatusCode.OK, completed.StatusCode);

        using var verify = factory.Services.CreateScope();
        var database = verify.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var configuration = await database.Set<UserMfaConfiguration>().AsNoTracking().SingleAsync(row => row.UserId == subject.UserId);
        Assert.Equal(subject.Email, configuration.EmailMfaAddress);
        Assert.True(configuration.IsEnabled);
        // An additional factor: the authenticator stays enrolled beside it.
        Assert.False(string.IsNullOrEmpty(configuration.TotpSecretKey));
    }

    [Fact]
    public async Task FailedEmailProofsPersistTheSharedAccountBudgetAndBlockFreshSignIn()
    {
        var delivery = new EmailDelivery();
        using var factory = CreateFactory(delivery);
        var subject = await SeedAsync(factory, emailEnrolled: true);
        using var client = factory.CreateClient();
        var bearer = await BeginPendingAsync(client, subject);
        using var sent = await client.PostAsJsonAsync("/v1/auth/mfa/email:challenge", new { userId = subject.UserId });
        Assert.Equal(HttpStatusCode.OK, sent.StatusCode);
        Assert.NotNull(delivery.LastCode);
        var wrong = (delivery.LastCode[0] == '0' ? '1' : '0') + delivery.LastCode[1..];
        for (var attempt = 0; attempt < 3; attempt++)
        {
            using var denied = await client.PostAsJsonAsync("/v1/auth/mfa/sign-in/complete", new { mfaToken = bearer, code = wrong, method = "Email" });
            Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
        }
        using var verify = factory.Services.CreateScope();
        var database = verify.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var configuration = await database.Set<UserMfaConfiguration>().AsNoTracking().SingleAsync(row => row.UserId == subject.UserId);
        Assert.Equal(3, configuration.FailedAttempts);
        Assert.True(configuration.LockedOutUntil > DateTime.UtcNow);
        Assert.Equal(3, await database.Set<MfaAttempt>().CountAsync(row => row.UserId == subject.UserId && !row.IsSuccessful && row.Method == MfaMethod.Email));

        using var freshPassword = await client.PostAsJsonAsync("/v1/auth/sign-in", new { subject.Email, subject.Password, subject.TenantId });
        // Local sign-in reports the service denial in its HTTP 200 response; the boundary is the
        // absence of credentials, not the status code.
        Assert.Equal(HttpStatusCode.OK, freshPassword.StatusCode);
        using var freshBody = JsonDocument.Parse(await freshPassword.Content.ReadAsStringAsync());
        var fresh = freshBody.RootElement;
        Assert.False(fresh.GetProperty("success").GetBoolean());
        Assert.False(fresh.GetProperty("requiresMfa").GetBoolean());
        Assert.True(string.IsNullOrEmpty(fresh.GetProperty("accessToken").GetString()));
        Assert.True(string.IsNullOrEmpty(fresh.GetProperty("mfaToken").GetString()));

        using var correctAfterLock = await client.PostAsJsonAsync("/v1/auth/mfa/sign-in/complete", new { mfaToken = bearer, code = delivery.LastCode, method = "Email" });
        Assert.Equal(HttpStatusCode.Unauthorized, correctAfterLock.StatusCode);
        Assert.Equal(0, await database.Set<UserSession>().CountAsync(row => row.UserId == subject.UserId));
        Assert.Equal(1, delivery.Count);
    }

    [Fact]
    public async Task EmailMethodIsDeniedWhenEmailIsNotAmongTheEnrolledMethods()
    {
        var delivery = new EmailDelivery();
        using var factory = CreateFactory(delivery);
        // A TOTP-enrolled subject keeps the sign-in challenge alive but has no email factor.
        var subject = await SeedAsync(factory, emailEnrolled: false);
        using var client = factory.CreateClient();
        var bearer = await BeginPendingAsync(client, subject);

        using var denied = await client.PostAsJsonAsync("/v1/auth/mfa/sign-in/complete", new { mfaToken = bearer, code = "000000", method = "Email" });
        Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);

        using var verify = factory.Services.CreateScope();
        var database = verify.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(0, await database.Set<UserSession>().CountAsync(row => row.UserId == subject.UserId));
        Assert.Equal(0, await database.Set<MfaAttempt>().CountAsync(row => row.UserId == subject.UserId && row.Method == MfaMethod.Email));
        Assert.Equal(0, delivery.Count);
    }

    private static async Task<string> BeginPendingAsync(HttpClient client, EmailSubject subject)
    {
        using var response = await client.PostAsJsonAsync("/v1/auth/sign-in", new { subject.Email, subject.Password, subject.TenantId });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        AssertPendingWithoutCredentials(body.RootElement);
        var bearer = body.RootElement.GetProperty("mfaToken").GetString();
        Assert.True(SignInMfaChallengeToken.TryHash(bearer, out _));
        return bearer!;
    }

    private WebApplicationFactory<Program> CreateFactory(EmailDelivery delivery)
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
            services.RemoveAll<IEmailMfaService>();
            services.AddScoped<IEmailMfaService>(provider => new EmailMfaService(
                provider.GetRequiredService<ILogger<EmailMfaService>>(),
                provider.GetRequiredService<IMemoryCache>(),
                provider.GetRequiredService<IUserMfaConfigurationRepository>(),
                provider.GetRequiredService<IUserRepository>(),
                delivery.Publisher,
                provider.GetRequiredService<IMfaAttemptTrackingService>()));
            services.RemoveAll<IAuthenticationAnomalyDetectionService>();
            services.AddSingleton(risk.Object);
        }));
    }

    private static async Task<EmailSubject> SeedAsync(WebApplicationFactory<Program> factory, bool emailEnrolled)
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
        var user = User.CreateWithPassword("email-sign-in-" + marker + "@example.test", "Email " + marker,
            provider.GetRequiredService<IPasswordHasher>().HashPassword(password), "email-sign-in-" + marker);
        user.VerifyEmail();
        var database = provider.GetRequiredService<ApplicationDbContext>();
        database.Set<User>().Add(user);
        database.Set<Tenant>().Add(new Tenant { Id = tenantId, Name = "Email " + marker, Slug = "email-" + marker,
            AdminEmail = user.Email, IsActive = true });
        database.Set<TenantMember>().Add(new TenantMember { Id = Guid.NewGuid(), UserId = user.Id, TenantId = tenantId,
            Role = "Member", IsActive = true });
        var secret = new string(Enumerable.Range(0, 32).Select(_ => "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567"[RandomNumberGenerator.GetInt32(32)]).ToArray());
        database.Set<UserMfaConfiguration>().Add(new UserMfaConfiguration { Id = Guid.NewGuid(), UserId = user.Id,
            IsEnabled = true, IsSetupComplete = true, PreferredMethod = MfaMethod.Email,
            EmailMfaAddress = emailEnrolled ? user.Email : null,
            TotpSecretKey = emailEnrolled ? null : provider.GetRequiredService<IEncryptionService>().Encrypt(secret) });
        await database.SaveChangesAsync();
        return new EmailSubject(user.Id, tenantId, user.Email, password, emailEnrolled ? null : secret);
    }

    private static string ComputeTotpCode(string base32Secret, int timeStepSeconds)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var normalized = base32Secret.ToUpperInvariant().Replace(" ", "").Replace("-", "");
        var bits = new BitArray(normalized.Length * 5);
        var bitIndex = 0;
        foreach (var character in normalized)
        {
            var value = alphabet.IndexOf(character);
            Assert.True(value >= 0, "The enrollment secret is not valid Base32.");
            for (var shift = 4; shift >= 0; shift--) { bits[bitIndex++] = (value >> shift & 1) == 1; }
        }
        var key = new byte[bitIndex / 8];
        for (var index = 0; index < key.Length; index++)
        {
            for (var bit = 0; bit < 8; bit++) { if (bits[index * 8 + bit]) { key[index] |= (byte)(1 << 7 - bit); } }
        }
        var message = new byte[8];
        BinaryPrimitives.WriteInt64BigEndian(message, DateTimeOffset.UtcNow.ToUnixTimeSeconds() / timeStepSeconds);
        var digest = HMACSHA1.HashData(key, message);
        var offset = digest[^1] & 0x0F;
        var truncated = BinaryPrimitives.ReadUInt32BigEndian(digest.AsSpan(offset, 4)) & 0x7FFFFFFF;
        return (truncated % 1000000).ToString("D6", CultureInfo.InvariantCulture);
    }

    private static void AssertPendingWithoutCredentials(JsonElement result)
    {
        Assert.False(result.GetProperty("success").GetBoolean());
        Assert.True(result.GetProperty("requiresMfa").GetBoolean());
        Assert.True(string.IsNullOrEmpty(result.GetProperty("accessToken").GetString()));
        Assert.True(string.IsNullOrEmpty(result.GetProperty("refreshToken").GetString()));
    }

    private sealed record EmailSubject(Guid UserId, Guid TenantId, string Email, string Password, string? TotpSecret);

    private sealed class EmailDelivery
    {
        public string? LastCode { get; private set; }
        public Guid LastUserId { get; private set; }
        public int Count { get; private set; }
        public IPublisher Publisher { get; }

        public EmailDelivery()
        {
            var publisher = new Mock<IPublisher>(MockBehavior.Strict);
            publisher.Setup(value => value.Publish(It.IsAny<EmailMfaCodeRequestedNotification>(), It.IsAny<CancellationToken>()))
                .Callback<EmailMfaCodeRequestedNotification, CancellationToken>((notice, _) =>
                {
                    LastCode = notice.Code;
                    LastUserId = notice.UserId;
                    Count++;
                }).Returns(Task.CompletedTask);
            Publisher = publisher.Object;
        }
    }
}
