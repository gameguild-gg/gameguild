using System.Buffers.Binary;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using GameGuild.API.Database;
using GameGuild.API.IntegrationTests.Infrastructure;
using GameGuild.Configuration.ApplicationLayer;
using GameGuild.Email;
using GameGuild.Identity.Authentication;
using GameGuild.Identity.Tenants;
using GameGuild.Identity.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Moq;

namespace GameGuild.API.IntegrationTests;

[Collection(ApiPostgreSqlCollection.Name)]
public sealed class MfaSettingsEnrollmentPostgreSqlTests(ApiPostgreSqlFixture fixture)
{
    private const string SetupPath = "/v1/auth/mfa/totp:setup";

    [Fact]
    public async Task SetupReturnsEveryGeneratedBackupCodeAndCodesRecoverOnlyOnceAfterConfirmation()
    {
        using var factory = CreateFactory();
        var account = await SeedAsync(factory);
        using var client = CreateOwnerClient(factory, account.UserId, account.TenantId);
        using var setup = await client.PostAsync(SetupPath, null);
        Assert.Equal(HttpStatusCode.OK, setup.StatusCode);
        using var body = JsonDocument.Parse(await setup.Content.ReadAsStringAsync());
        var codes = body.RootElement.GetProperty("backupCodes").EnumerateArray()
            .Select(value => value.GetString()!).ToArray();
        Assert.Equal(10, codes.Length);
        Assert.Equal(10, codes.Distinct(StringComparer.Ordinal).Count());
        Assert.All(codes, code => Assert.False(string.IsNullOrWhiteSpace(code)));
        var secret = body.RootElement.GetProperty("secretKey").GetString()!;
        Assert.False(string.IsNullOrWhiteSpace(secret));
        Assert.Contains(Uri.EscapeDataString(account.Email), body.RootElement.GetProperty("qrCodeUri").GetString());

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var stored = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>()
                .Set<UserMfaConfiguration>().SingleAsync(row => row.UserId == account.UserId);
            Assert.False(stored.IsEnabled);
            Assert.False(stored.IsSetupComplete);
            Assert.NotEqual(secret, stored.TotpSecretKey);
            Assert.False(string.IsNullOrEmpty(stored.BackupCodes));
            Assert.All(codes, code => Assert.DoesNotContain(code, stored.BackupCodes));
        }

        var options = factory.Services.GetRequiredService<MfaOptions>();
        using var confirmed = await client.PostAsJsonAsync("/v1/auth/mfa/totp:complete",
            new { code = Totp(secret, options.TotpTimeStepSeconds), secretKey = secret });
        Assert.Equal(HttpStatusCode.OK, confirmed.StatusCode);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var stored = await database.Set<UserMfaConfiguration>().SingleAsync(row => row.UserId == account.UserId);
            Assert.True(stored.IsEnabled);
            Assert.True(stored.IsSetupComplete);
            await using var transaction = await database.Database.BeginTransactionAsync();
            var backup = scope.ServiceProvider.GetRequiredService<IBackupCodeMfaService>();
            Assert.True(await backup.VerifyBackupCodeAsync(account.UserId, codes[0]));
            Assert.False(await backup.VerifyBackupCodeAsync(account.UserId, codes[0]));
            await transaction.CommitAsync();
        }
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task ExpectedSetupDenialsReturnBadRequestWithoutChangingExistingFactor(
        bool platformEnabled, bool alreadyEnrolled)
    {
        using var factory = CreateFactory(platformEnabled);
        var account = await SeedAsync(factory, alreadyEnrolled);
        using var client = CreateOwnerClient(factory, account.UserId, account.TenantId);
        using var denied = await client.PostAsync(SetupPath, null);
        Assert.Equal(HttpStatusCode.BadRequest, denied.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var stored = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>()
            .Set<UserMfaConfiguration>().SingleOrDefaultAsync(row => row.UserId == account.UserId);
        if (alreadyEnrolled)
        {
            Assert.NotNull(stored);
            Assert.True(stored.IsEnabled);
            Assert.True(stored.IsSetupComplete);
            Assert.Equal(account.EncryptedSecret, stored.TotpSecretKey);
        }
        else
        {
            Assert.Null(stored);
        }
    }

    [Fact]
    public async Task UnexpectedSetupInfrastructureFailureRemainsServerError()
    {
        using var factory = CreateFactory(configure: services =>
        {
            var broken = new Mock<ITotpMfaService>(MockBehavior.Strict);
            broken.Setup(service => service.SetupTotpAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Synthetic setup storage unavailable"));
            services.RemoveAll<ITotpMfaService>();
            services.AddScoped(_ => broken.Object);
        });
        var account = await SeedAsync(factory);
        using var client = CreateOwnerClient(factory, account.UserId, account.TenantId);
        using var response = await client.PostAsync(SetupPath, null);
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        Assert.False(await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>()
            .Set<UserMfaConfiguration>().AnyAsync(row => row.UserId == account.UserId));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RegenerationWithoutEnabledFactorReturnsBadRequestAfterValidPassword(bool platformEnabled)
    {
        using var factory = CreateFactory(platformEnabled);
        var password = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
        var account = await SeedAsync(factory, password: password);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var stored = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>()
                .Set<User>().SingleAsync(user => user.Id == account.UserId);
            Assert.True(scope.ServiceProvider.GetRequiredService<IPasswordHasher>()
                .VerifyPassword(stored.PasswordHash!, password));
        }
        using var client = CreateOwnerClient(factory, account.UserId, account.TenantId);
        using var response = await client.PostAsJsonAsync("/v1/auth/mfa/backup-codes:regenerate", new { password });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await using var verification = factory.Services.CreateAsyncScope();
        Assert.False(await verification.ServiceProvider.GetRequiredService<ApplicationDbContext>()
            .Set<UserMfaConfiguration>().AnyAsync(row => row.UserId == account.UserId));
    }

    [Fact]
    public async Task AnonymousCallerCannotStartEnrollment()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        using var response = await client.PostAsync(SetupPath, null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private WebApplicationFactory<Program> CreateFactory(bool enabled = true,
        Action<IServiceCollection>? configure = null) => fixture.CreateFactory(builder =>
        builder.ConfigureTestServices(services =>
        {
            services.PostConfigure<MfaOptions>(options =>
            {
                options.Enabled = enabled;
                options.RequireMfaByDefault = false;
                options.BackupCodesCount = 10;
            });
            services.PostConfigure<EmailDeliveryOptions>(options => options.Enabled = false);
            services.RemoveAll<MfaOptions>();
            services.AddSingleton(provider => provider.GetRequiredService<IOptions<MfaOptions>>().Value);
            services.RemoveAll<IMfaAttemptTrackingService>();
            services.AddScoped<IMfaAttemptTrackingService>(provider => ActivatorUtilities.CreateInstance<MfaAttemptTrackingService>(
                provider, provider.GetRequiredService<IOptions<MfaOptions>>().Value));
            services.RemoveAll<IMfaService>();
            services.AddScoped<IMfaService, MfaService>();
            services.AddScoped<IClaimsTransformation, SeededOwnerEmailClaims>();
            configure?.Invoke(services);
        }));

    private static HttpClient CreateOwnerClient(WebApplicationFactory<Program> factory, Guid userId, Guid tenantId)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ApiPostgreSqlTestAuthHandler.SchemeName, "authenticated");
        client.DefaultRequestHeaders.Add(ApiPostgreSqlTestAuthHandler.UserIdHeader, userId.ToString());
        client.DefaultRequestHeaders.Add(ApiPostgreSqlTestAuthHandler.TenantIdHeader, tenantId.ToString());
        client.DefaultRequestHeaders.Add("X-Tenant-Id", tenantId.ToString());
        return client;
    }

    private static async Task<Account> SeedAsync(WebApplicationFactory<Program> factory, bool enrolled = false,
        string? password = null)
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var email = "mfa-settings-" + userId.ToString("N") + "@example.test";
        await using var scope = factory.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = User.CreateOAuthUser(email, "MFA settings owner " + userId.ToString("N"));
        user.Id = userId;
        if (password is not null)
        {
            user.PasswordHash = scope.ServiceProvider.GetRequiredService<IPasswordHasher>().HashPassword(password);
        }
        database.Set<User>().Add(user);
        database.Set<Tenant>().Add(new Tenant { Id = tenantId, Name = "MFA settings " + tenantId.ToString("N"), Slug = "mfa-settings-" + tenantId.ToString("N"), AdminEmail = email, IsActive = true });
        database.Set<TenantMember>().Add(new TenantMember { Id = Guid.NewGuid(), TenantId = tenantId, UserId = userId, Role = "Member", IsActive = true });
        string? encryptedSecret = null;
        if (enrolled)
        {
            var secret = new string(Enumerable.Range(0, 32).Select(_ => "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567"[RandomNumberGenerator.GetInt32(32)]).ToArray());
            encryptedSecret = scope.ServiceProvider.GetRequiredService<IEncryptionService>().Encrypt(secret);
            database.Set<UserMfaConfiguration>().Add(new UserMfaConfiguration
            {
                Id = Guid.NewGuid(), UserId = userId, IsEnabled = true, IsSetupComplete = true,
                PreferredMethod = MfaMethod.Totp, TotpSecretKey = encryptedSecret
            });
        }
        await database.SaveChangesAsync();
        return new Account(userId, tenantId, email, encryptedSecret);
    }

    private static string Totp(string secret, int stepSeconds)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var key = new byte[secret.Length * 5 / 8];
        var buffer = 0;
        var bits = 0;
        var offset = 0;
        foreach (var character in secret)
        {
            var value = alphabet.IndexOf(character);
            Assert.InRange(value, 0, 31);
            buffer = buffer << 5 | value;
            bits += 5;
            if (bits >= 8)
            {
                bits -= 8;
                key[offset++] = (byte)(buffer >> bits);
                buffer &= (1 << bits) - 1;
            }
        }
        Span<byte> counter = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(counter, DateTimeOffset.UtcNow.ToUnixTimeSeconds() / stepSeconds);
        var hash = HMACSHA1.HashData(key, counter);
        var index = hash[^1] & 15;
        var binary = BinaryPrimitives.ReadUInt32BigEndian(hash.AsSpan(index, 4)) & 0x7fffffff;
        return (binary % 1_000_000).ToString("D6", System.Globalization.CultureInfo.InvariantCulture);
    }

    private sealed record Account(Guid UserId, Guid TenantId, string Email, string? EncryptedSecret);

    // Authentication itself is the existing fixture's explicit synthetic owner. Add its
    // seeded email for the controller prerequisite; all enrollment services/DB writes are real.
    private sealed class SeededOwnerEmailClaims(ApplicationDbContext database) : IClaimsTransformation
    {
        public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            if (principal.Identity is ClaimsIdentity { IsAuthenticated: true } identity &&
                Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) &&
                !principal.HasClaim(claim => claim.Type == ClaimTypes.Email))
            {
                var email = await database.Set<User>().IgnoreQueryFilters().Where(user => user.Id == id)
                    .Select(user => user.Email).SingleAsync();
                identity.AddClaim(new Claim(ClaimTypes.Email, email));
            }
            return principal;
        }
    }
}
