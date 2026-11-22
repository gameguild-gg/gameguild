using System.Net;
using System.Net.Http.Headers;
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

[Collection(ApiPostgreSqlCollection.Name)]
public sealed class MfaSmsEnrollmentReverificationPostgreSqlTests(ApiPostgreSqlFixture fixture)
{
    [Theory]
    [InlineData("missing")]
    [InlineData("wrong")]
    public async Task StartingSmsEnrollmentRequiresReverifiedPrimarySecret(string fault)
    {
        var delivery = new SmsDelivery();
        using var factory = CreateFactory(delivery);
        var subject = await SeedAsync(factory, smsAlreadyVerified: false);
        using var client = factory.CreateClient();
        await AuthenticateOrdinarySessionAsync(client, subject);
        var request = new Dictionary<string, string> { ["phoneNumber"] = "+15555550123" };
        if (fault == "wrong") { request["password"] = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)); }
        using var response = await client.PostAsJsonAsync("/v1/auth/mfa/sms:setup", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, delivery.Count);
    }

    [Fact]
    public async Task CorrectPrimarySecretStillStartsTheOwnersSmsEnrollment()
    {
        var delivery = new SmsDelivery();
        using var factory = CreateFactory(delivery);
        var subject = await SeedAsync(factory, smsAlreadyVerified: false);
        using var client = factory.CreateClient();
        await AuthenticateOrdinarySessionAsync(client, subject);
        using var response = await client.PostAsJsonAsync("/v1/auth/mfa/sms:setup", new { phoneNumber = "+15555550123", password = subject.Password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, delivery.Count);
        Assert.Equal(subject.UserId, delivery.LastUserId);
    }

    [Fact]
    public async Task FreshSmsEnrollmentDoesNotRequireAPreexistingTotpConfiguration()
    {
        var delivery = new SmsDelivery();
        using var factory = CreateFactory(delivery);
        var subject = await SeedAsync(factory, smsAlreadyVerified: false);
        using (var removeFixtureConfiguration = factory.Services.CreateScope())
        {
            var initialDatabase = removeFixtureConfiguration.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Equal(1, await initialDatabase.Set<UserMfaConfiguration>().Where(row => row.UserId == subject.UserId).ExecuteDeleteAsync());
            Assert.Equal(0, await initialDatabase.Set<UserMfaConfiguration>().CountAsync(row => row.UserId == subject.UserId));
        }
        using var client = factory.CreateClient();
        await AuthenticateOrdinarySessionAsync(client, subject);
        using var setup = await client.PostAsJsonAsync("/v1/auth/mfa/sms:setup", new { phoneNumber = "+15555550123", password = subject.Password });
        Assert.Equal(HttpStatusCode.OK, setup.StatusCode);
        Assert.NotNull(delivery.LastCode);
        using var completed = await client.PostAsJsonAsync("/v1/auth/mfa/sms:complete", new { code = delivery.LastCode });
        Assert.Equal(HttpStatusCode.OK, completed.StatusCode);
        using var verify = factory.Services.CreateScope();
        var stored = await verify.ServiceProvider.GetRequiredService<ApplicationDbContext>().Set<UserMfaConfiguration>().AsNoTracking().SingleAsync(row => row.UserId == subject.UserId);
        Assert.True(stored.IsEnabled);
        Assert.True(stored.IsSetupComplete);
        Assert.Equal(MfaMethod.Sms, stored.PreferredMethod);
        Assert.Null(stored.TotpSecretKey);
        Assert.Equal("+15555550123", stored.SmsPhoneNumber);
        var database = verify.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var sessionsBefore = await database.Set<UserSession>().CountAsync(row => row.UserId == subject.UserId);
        client.DefaultRequestHeaders.Authorization = null;
        using var pending = await client.PostAsJsonAsync("/v1/auth/sign-in", new { subject.Email, subject.Password, subject.TenantId });
        Assert.Equal(HttpStatusCode.OK, pending.StatusCode);
        using var pendingJson = JsonDocument.Parse(await pending.Content.ReadAsStringAsync());
        Assert.True(pendingJson.RootElement.GetProperty("requiresMfa").GetBoolean());
        Assert.False(pendingJson.RootElement.GetProperty("success").GetBoolean());
        Assert.True(string.IsNullOrEmpty(pendingJson.RootElement.GetProperty("accessToken").GetString()));
        Assert.True(string.IsNullOrEmpty(pendingJson.RootElement.GetProperty("refreshToken").GetString()));
        Assert.Contains(pendingJson.RootElement.GetProperty("availableMethods").EnumerateArray(), item => string.Equals(item.GetString(), "Sms", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(sessionsBefore, await database.Set<UserSession>().CountAsync(row => row.UserId == subject.UserId));
    }

    private static async Task AuthenticateOrdinarySessionAsync(HttpClient client, SmsSubject subject)
    {
        using var response = await client.PostAsJsonAsync("/v1/auth/sign-in", new { subject.Email, subject.Password, subject.TenantId });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(body.RootElement.GetProperty("success").GetBoolean());
        var access = body.RootElement.GetProperty("accessToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(access));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access);
        using var protectedState = await client.GetAsync("/v1/auth/session-state");
        Assert.Equal(HttpStatusCode.OK, protectedState.StatusCode);
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
            services.PostConfigure<MfaOptions>(options => { options.Enabled = true; options.RequireMfaByDefault = false; });
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
