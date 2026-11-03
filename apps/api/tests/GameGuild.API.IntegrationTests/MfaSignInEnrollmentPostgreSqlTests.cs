using System.Buffers.Binary;
using System.Collections;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using GameGuild.API.Database;
using GameGuild.API.IntegrationTests.Infrastructure;
using GameGuild.Configuration.ApplicationLayer;
using GameGuild.Identity.Authentication;
using GameGuild.Identity.Tenants;
using GameGuild.Identity.Users;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Moq;

namespace GameGuild.API.IntegrationTests;

/// <summary>
///     First-ever real-PostgreSQL coverage of <see cref="PostgreSqlSignInMfaChallengeStore" /> and the
///     <c>ck_sign_in_mfa_enrollment_binding</c> check constraint: required-MFA unenrolled subjects get a
///     bound EnrollFactor challenge (T10 of the sign-in enrollment deadlock fix).
/// </summary>
[Collection(ApiPostgreSqlCollection.Name)]
public sealed class MfaSignInEnrollmentPostgreSqlTests(ApiPostgreSqlFixture fixture)
{
    [Fact]
    public async Task RequiredUnenrolledSignInProvisionsBoundEnrollmentChallengeAndCompletesWithTotp()
    {
        using var factory = CreateFactory(requireMfaByDefault: true);
        var marker = Guid.NewGuid().ToString("N");
        var password = SyntheticPassword();
        var (userId, tenantId, email) = await SeedAsync(factory, marker, password);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var pending = await SignInPendingAsync(client, email, password, tenantId);

        Assert.False(pending.Success);
        Assert.True(pending.RequiresMfa);
        Assert.True(SignInMfaChallengeToken.TryHash(pending.Bearer, out _));
        Assert.Equal(43, pending.Bearer.Length);
        Assert.False(string.IsNullOrWhiteSpace(pending.SecretKey));
        Assert.StartsWith("otpauth://totp/", pending.QrCodeUri);

        using (var scope = factory.Services.CreateScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            // THE constraint regression assertion: the challenge insert itself proves
            // ck_sign_in_mfa_enrollment_binding accepts the fully bound EnrollFactor row.
            Assert.Equal(1, await CountBoundEnrollFactorChallengesAsync(database, userId));
            var challenge = await database.Set<SignInMfaChallenge>().AsNoTracking()
                .SingleAsync(row => row.SubjectId == userId);
            Assert.Equal(SignInMfaPurpose.EnrollFactor, challenge.Purpose);
            Assert.NotNull(challenge.EnrollmentConfigurationId);
            Assert.NotEqual(Guid.Empty, challenge.EnrollmentConfigurationId.Value);
            Assert.Matches("^[a-f0-9]{64}$", challenge.EnrollmentSecretFingerprint);
            Assert.NotNull(challenge.EnrollmentInitializedAt);
            Assert.InRange(challenge.EnrollmentInitializedAt!.Value, challenge.CreatedAt, challenge.ExpiresAt);
            // Sign-in itself provisions the pending configuration the challenge is bound to.
            var pendingConfiguration = await database.Set<UserMfaConfiguration>().SingleAsync(row => row.UserId == userId);
            Assert.False(pendingConfiguration.IsEnabled);
            Assert.False(pendingConfiguration.IsSetupComplete);
            Assert.Equal(challenge.EnrollmentConfigurationId, pendingConfiguration.Id);
            Assert.Equal(challenge.ExpiresAt.UtcDateTime, pendingConfiguration.SetupExpiresAt);
        }

        var code = ComputeTotpCode(pending.SecretKey, factory.Services.GetRequiredService<MfaOptions>().TotpTimeStepSeconds);
        var (completed, backupCodes) = await CompleteAsync(client, pending.Bearer, code);
        Assert.True(completed.GetProperty("success").GetBoolean());
        Assert.True(backupCodes.GetArrayLength() > 0);
        Assert.All(backupCodes.EnumerateArray(), value => Assert.False(string.IsNullOrWhiteSpace(value.GetString())));

        using (var scope = factory.Services.CreateScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var configuration = await database.Set<UserMfaConfiguration>().SingleAsync(row => row.UserId == userId);
            Assert.True(configuration.IsEnabled);
            Assert.True(configuration.IsSetupComplete);
            Assert.Null(configuration.SetupExpiresAt);
            Assert.NotNull(await database.Set<TotpReplayState>().SingleOrDefaultAsync(row => row.ConfigurationId == configuration.Id));
        }
    }

    [Fact]
    public async Task MidEnrollmentSignInResumesPendingConfigurationWithoutDuplicatingIt()
    {
        using var factory = CreateFactory(requireMfaByDefault: true);
        var marker = Guid.NewGuid().ToString("N");
        var password = SyntheticPassword();
        var seededConfigurationId = Guid.NewGuid();
        var seededSecret = SyntheticSecret();
        var (userId, tenantId, email) = await SeedAsync(factory, marker, password, encryption => new UserMfaConfiguration
        {
            Id = seededConfigurationId, IsEnabled = false, IsSetupComplete = false, PreferredMethod = MfaMethod.Totp,
            SetupExpiresAt = DateTimeOffset.UtcNow.AddMinutes(3).UtcDateTime,
            TotpSecretKey = encryption.Encrypt(seededSecret)
        });
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var pending = await SignInPendingAsync(client, email, password, tenantId);
        Assert.True(pending.RequiresMfa);

        // Resume, not duplicate: the unique index ix_user_mfa_configuration_user_id holds and the
        // challenge binds to the pre-existing pending row instead of provisioning a second one.
        string resumedSecret;
        using (var scope = factory.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            var database = services.GetRequiredService<ApplicationDbContext>();
            Assert.Equal(1, await database.Set<UserMfaConfiguration>().CountAsync(row => row.UserId == userId));
            var configuration = await database.Set<UserMfaConfiguration>().SingleAsync(row => row.UserId == userId);
            Assert.Equal(seededConfigurationId, configuration.Id);
            var challenge = await database.Set<SignInMfaChallenge>().AsNoTracking()
                .SingleAsync(row => row.SubjectId == userId);
            Assert.Equal(seededConfigurationId, challenge.EnrollmentConfigurationId);
            // Resume overwrites the pending secret; the offered secret is the one now stored encrypted.
            resumedSecret = services.GetRequiredService<IEncryptionService>().Decrypt(configuration.TotpSecretKey!);
            Assert.NotEqual(seededSecret, resumedSecret);
        }

        var code = ComputeTotpCode(pending.SecretKey, factory.Services.GetRequiredService<MfaOptions>().TotpTimeStepSeconds);
        var (completed, backupCodes) = await CompleteAsync(client, pending.Bearer, code);
        Assert.True(completed.GetProperty("success").GetBoolean());
        Assert.True(backupCodes.GetArrayLength() > 0);
        using (var scope = factory.Services.CreateScope())
        {
            var configuration = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>()
                .Set<UserMfaConfiguration>().SingleAsync(row => row.UserId == userId);
            Assert.True(configuration.IsEnabled);
            Assert.True(configuration.IsSetupComplete);
            Assert.Null(configuration.SetupExpiresAt);
        }
    }

    [Fact]
    public async Task ChallengeStorePersistsBoundEnrollFactorAndRejectsBareOrPartialBinding()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var store = new PostgreSqlSignInMfaChallengeStore(context);
        var subjectId = Guid.NewGuid();
        var configurationId = Guid.NewGuid();
        var fingerprint = Fingerprint("enrollment-secret-" + Guid.NewGuid().ToString("N"));
        var bound = ValidChallenge(subjectId, SignInMfaPurpose.EnrollFactor);
        bound.EnrollmentConfigurationId = configurationId;
        bound.EnrollmentSecretFingerprint = fingerprint;
        bound.EnrollmentInitializedAt = bound.CreatedAt;
        await store.AddAsync(bound, CancellationToken.None);
        Assert.Equal(1, await context.Set<SignInMfaChallenge>().CountAsync(row => row.SubjectId == subjectId));

        // Bare EnrollFactor is unpersistable (ValidateForPersistence fires before any DB round-trip).
        await Assert.ThrowsAsync<ArgumentException>(() => store.AddAsync(ValidChallenge(subjectId, SignInMfaPurpose.EnrollFactor), CancellationToken.None));
        var partial = ValidChallenge(subjectId, SignInMfaPurpose.EnrollFactor);
        partial.EnrollmentConfigurationId = configurationId;
        await Assert.ThrowsAsync<ArgumentException>(() => store.AddAsync(partial, CancellationToken.None));
        Assert.Equal(1, await context.Set<SignInMfaChallenge>().CountAsync(row => row.SubjectId == subjectId));
    }

    [Fact]
    public async Task InvalidTotpCodeDeniesCompletionWithoutBlockingTheCorrectRetry()
    {
        using var factory = CreateFactory(requireMfaByDefault: true);
        var marker = Guid.NewGuid().ToString("N");
        var password = SyntheticPassword();
        var (userId, tenantId, email) = await SeedAsync(factory, marker, password);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var pending = await SignInPendingAsync(client, email, password, tenantId);

        // A non-digit payload can never match a generated code.
        using var denied = await client.PostAsJsonAsync("/v1/auth/mfa/sign-in/complete",
            new { mfaToken = pending.Bearer, code = "99999X", method = "Totp" });
        Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);

        using (var scope = factory.Services.CreateScope())
        {
            var configuration = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>()
                .Set<UserMfaConfiguration>().SingleAsync(row => row.UserId == userId);
            Assert.False(configuration.IsEnabled);
            Assert.False(configuration.IsSetupComplete);
            Assert.Equal(1, configuration.FailedAttempts);
            Assert.Null(configuration.LockedOutUntil);
        }

        var code = ComputeTotpCode(pending.SecretKey, factory.Services.GetRequiredService<MfaOptions>().TotpTimeStepSeconds);
        var (completed, backupCodes) = await CompleteAsync(client, pending.Bearer, code);
        Assert.True(completed.GetProperty("success").GetBoolean());
        Assert.True(backupCodes.GetArrayLength() > 0);
        using (var scope = factory.Services.CreateScope())
        {
            var configuration = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>()
                .Set<UserMfaConfiguration>().SingleAsync(row => row.UserId == userId);
            Assert.True(configuration.IsEnabled);
            Assert.Null(configuration.LockedOutUntil);
        }
    }

    [Fact]
    public async Task OptionalPolicySignInIssuesCredentialsWithoutChallengeOrEnrollment()
    {
        using var factory = CreateFactory(requireMfaByDefault: false);
        var marker = Guid.NewGuid().ToString("N");
        var password = SyntheticPassword();
        var (userId, tenantId, email) = await SeedAsync(factory, marker, password);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var response = await client.PostAsJsonAsync("/v1/auth/sign-in", new { email, password, tenantId });
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK, got {response.StatusCode}: {body}");
        using var json = JsonDocument.Parse(body);
        Assert.True(json.RootElement.GetProperty("success").GetBoolean());
        Assert.False(json.RootElement.GetProperty("requiresMfa").GetBoolean());
        Assert.False(string.IsNullOrEmpty(json.RootElement.GetProperty("accessToken").GetString()));
        Assert.True(!json.RootElement.TryGetProperty("mfaToken", out var mfaToken) || mfaToken.ValueKind == JsonValueKind.Null);
        Assert.True(!json.RootElement.TryGetProperty("mfaEnrollment", out var enrollment) || enrollment.ValueKind == JsonValueKind.Null);
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(0, await database.Set<SignInMfaChallenge>().CountAsync(row => row.SubjectId == userId));
        Assert.Equal(0, await database.Set<UserMfaConfiguration>().CountAsync(row => row.UserId == userId));
    }

    private WebApplicationFactory<Program> CreateFactory(bool requireMfaByDefault) =>
        fixture.CreateFactory(builder => builder.ConfigureTestServices(services =>
        {
            var risk = new Mock<IAuthenticationAnomalyDetectionService>(MockBehavior.Strict);
            risk.Setup(service => service.AnalyzeLoginAttemptAsync(It.IsAny<AuthenticationAttemptContext>()))
                .ReturnsAsync(new AuthenticationAnomalyResult { RiskLevel = RiskLevel.Low });
            risk.Setup(service => service.AnalyzeBehavioralPatternsAsync(It.IsAny<Guid>(), It.IsAny<AuthenticationAttemptContext>()))
                .ReturnsAsync(new BehavioralAnalysisResult { MatchesTypicalPattern = true, RiskLevel = RiskLevel.Low });
            services.PostConfigure<MfaOptions>(options => { options.Enabled = true; options.RequireMfaByDefault = requireMfaByDefault; });
            services.RemoveAll<MfaOptions>();
            services.AddSingleton(provider => provider.GetRequiredService<IOptions<MfaOptions>>().Value);
            services.RemoveAll<IMfaAttemptTrackingService>();
            services.AddScoped<IMfaAttemptTrackingService>(provider => ActivatorUtilities.CreateInstance<MfaAttemptTrackingService>(provider,
                provider.GetRequiredService<IOptions<MfaOptions>>().Value));
            services.RemoveAll<IMfaService>();
            services.AddScoped<IMfaService, MfaService>();
            services.RemoveAll<IAuthenticationAnomalyDetectionService>();
            services.AddSingleton(risk.Object);
        }));

    private static async Task<(Guid UserId, Guid TenantId, string Email)> SeedAsync(WebApplicationFactory<Program> factory,
        string marker, string password, Func<IEncryptionService, UserMfaConfiguration?>? configurationFactory = null)
    {
        using var scope = factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        var email = "mfa-enroll-" + marker + "@example.test";
        var user = User.CreateWithPassword(email, "Synthetic MFA enrollment account",
            services.GetRequiredService<IPasswordHasher>().HashPassword(password), "mfa-enroll-" + marker);
        user.VerifyEmail();
        var tenantId = Guid.NewGuid();
        var database = services.GetRequiredService<ApplicationDbContext>();
        database.Set<User>().Add(user);
        database.Set<Tenant>().Add(new Tenant { Id = tenantId, Name = "MFA " + marker, Slug = "mfa-enroll-" + marker,
            AdminEmail = "admin-" + marker + "@example.test", IsActive = true });
        database.Set<TenantMember>().Add(new TenantMember { Id = Guid.NewGuid(), TenantId = tenantId, UserId = user.Id,
            Role = "Member", IsActive = true });
        if (configurationFactory?.Invoke(services.GetRequiredService<IEncryptionService>()) is { } configuration)
        {
            configuration.UserId = user.Id;
            database.Set<UserMfaConfiguration>().Add(configuration);
        }
        await database.SaveChangesAsync();
        return (user.Id, tenantId, email);
    }

    private static async Task<SignInPendingResult> SignInPendingAsync(HttpClient client, string email, string password,
        Guid tenantId)
    {
        using var response = await client.PostAsJsonAsync("/v1/auth/sign-in", new { email, password, tenantId });
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK, got {response.StatusCode}: {body}");
        using var json = JsonDocument.Parse(body);
        Assert.False(json.RootElement.GetProperty("success").GetBoolean());
        Assert.True(json.RootElement.GetProperty("requiresMfa").GetBoolean());
        Assert.True(string.IsNullOrEmpty(json.RootElement.GetProperty("accessToken").GetString()));
        var enrollment = json.RootElement.GetProperty("mfaEnrollment");
        return new SignInPendingResult(json.RootElement.GetProperty("success").GetBoolean(),
            json.RootElement.GetProperty("requiresMfa").GetBoolean(),
            json.RootElement.GetProperty("mfaToken").GetString()!,
            enrollment.GetProperty("secretKey").GetString()!, enrollment.GetProperty("qrCodeUri").GetString()!);
    }

    private static async Task<(JsonElement Body, JsonElement BackupCodes)> CompleteAsync(HttpClient client, string bearer,
        string code)
    {
        using var response = await client.PostAsJsonAsync("/v1/auth/mfa/sign-in/complete",
            new { mfaToken = bearer, code, method = "Totp" });
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK, got {response.StatusCode}: {body}");
        var json = JsonDocument.Parse(body);
        var backupCodes = json.RootElement.GetProperty("mfaEnrollmentBackupCodes").Clone();
        return (json.RootElement.Clone(), backupCodes);
    }

    private static Task<long> CountBoundEnrollFactorChallengesAsync(ApplicationDbContext database, Guid subjectId) =>
        database.Set<SignInMfaChallenge>().LongCountAsync(row =>
            row.SubjectId == subjectId && row.Purpose == SignInMfaPurpose.EnrollFactor &&
            row.EnrollmentConfigurationId != null && row.EnrollmentConfigurationId != Guid.Empty &&
            row.EnrollmentSecretFingerprint != null && Regex.IsMatch(row.EnrollmentSecretFingerprint, "^[a-f0-9]{64}$") &&
            row.EnrollmentInitializedAt != null && row.EnrollmentInitializedAt >= row.CreatedAt &&
            row.EnrollmentInitializedAt < row.ExpiresAt);

    private static string SyntheticSecret() => new(Enumerable.Range(0, 32)
        .Select(_ => "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567"[RandomNumberGenerator.GetInt32(32)]).ToArray());

    private static SignInMfaChallenge ValidChallenge(Guid subjectId, SignInMfaPurpose purpose)
    {
        Assert.True(SignInMfaChallengeToken.TryHash(SignInMfaChallengeToken.Create(), out var tokenHash));
        var now = DateTimeOffset.UtcNow;
        return new SignInMfaChallenge
        {
            Id = Guid.NewGuid(), SubjectId = subjectId, TenantId = Guid.NewGuid(), SubjectTokenVersion = 1,
            TokenHash = tokenHash, PolicyFingerprint = Fingerprint("policy-" + Guid.NewGuid().ToString("N")),
            Purpose = purpose, FirstFactor = SignInFirstFactor.Password, CreatedAt = now, ExpiresAt = now.AddMinutes(5)
        };
    }

    /// <summary>RFC 6238 HMAC-SHA1 six-digit code for a Base32 secret at the configured time step.</summary>
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

    private static string Fingerprint(string seed) =>
        Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes(seed))).ToLowerInvariant();

    private static string SyntheticPassword() => $"Synthetic1!{Guid.NewGuid():N}";

    private sealed record SignInPendingResult(bool Success, bool RequiresMfa, string Bearer, string SecretKey,
        string QrCodeUri);
}
