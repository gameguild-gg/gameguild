using System.Data.Common;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GameGuild.API.Database;
using GameGuild.API.IntegrationTests.Infrastructure;
using GameGuild.Compliance.Audit;
using GameGuild.Configuration.ApplicationLayer;
using GameGuild.Email;
using GameGuild.Identity.Authentication;
using GameGuild.Identity.Tenants;
using GameGuild.Identity.Users;
using GameGuild.Notifications;
using GameGuild.Notifications.Services.Email;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace GameGuild.API.IntegrationTests;

[Collection(ApiPostgreSqlCollection.Name)]
public sealed class MfaRecoveryLifecyclePostgreSqlTests(ApiPostgreSqlFixture fixture)
{
    private const string CancelPath = "/v1/auth/mfa/recovery:cancel";

    [Fact]
    public async Task EverySuccessfulRecoveryStageHasDurableSecurityAuditWithoutRawTokens()
    {
        using var factory = CreateFactory();
        var owner = await SeedAsync(factory);
        using var client = factory.CreateClient();
        using var requested = await client.PostAsJsonAsync("/v1/auth/mfa/recovery:request", new { owner.Email });
        Assert.Equal(HttpStatusCode.OK, requested.StatusCode);
        var requestToken = await ReadNotificationTokenAsync(factory, owner.UserId, NotificationType.MfaRecovery);
        using (var elapsed = factory.Services.CreateScope())
        {
            var database = elapsed.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var row = await database.Set<MfaRecoveryRequest>().SingleAsync(value => value.UserId == owner.UserId.ToString());
            row.RequestedAt = DateTimeOffset.UtcNow.AddDays(-8);
            row.CooldownEndsAt = DateTimeOffset.UtcNow.AddDays(-1);
            await database.SaveChangesAsync();
        }
        using var continued = await client.PostAsJsonAsync("/v1/auth/mfa/recovery:continue", new { requestToken });
        Assert.Equal(HttpStatusCode.OK, continued.StatusCode);
        var confirmationToken = await ReadNotificationTokenAsync(factory, owner.UserId, NotificationType.MfaRecoveryConfirmation);
        using var confirmed = await client.PostAsJsonAsync("/v1/auth/mfa/recovery:confirm", new { confirmationToken });
        Assert.Equal(HttpStatusCode.OK, confirmed.StatusCode);
        using var result = JsonDocument.Parse(await confirmed.Content.ReadAsStringAsync());
        Assert.True(result.RootElement.GetProperty("success").GetBoolean());
        using var verify = factory.Services.CreateScope();
        var stored = verify.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.False((await stored.Set<UserMfaConfiguration>().SingleAsync(value => value.UserId == owner.UserId)).IsEnabled);
        var audits = await stored.Set<AuditLog>().IgnoreQueryFilters().Where(value => value.UserId == owner.UserId &&
            value.ActionType.StartsWith("Authentication.MfaRecovery")).ToListAsync();
        Assert.Equal(3, audits.Count);
        Assert.Contains(audits, value => value.ActionType == "Authentication.MfaRecoveryRequested" && value.Success);
        Assert.Contains(audits, value => value.ActionType == "Authentication.MfaRecoveryContinued" && value.Success);
        Assert.Contains(audits, value => value.ActionType == "Authentication.MfaRecoveryConfirmed" && value.Success);
        var auditJson = JsonSerializer.Serialize(audits);
        Assert.DoesNotContain(requestToken, auditJson);
        Assert.DoesNotContain(confirmationToken, auditJson);
    }

    [Fact]
    public async Task OwnerCancellationStopsBothTokensAndKeepsTheFactorEnabled()
    {
        using var factory = CreateFactory();
        var owner = await SeedAsync(factory, awaiting: true);
        using var client = OwnerClient(factory, owner);
        using var cancelled = await client.PostAsync(CancelPath, null);
        Assert.Equal(HttpStatusCode.OK, cancelled.StatusCode);
        client.DefaultRequestHeaders.Authorization = null;
        using var continued = await client.PostAsJsonAsync("/v1/auth/mfa/recovery:continue", new { requestToken = owner.RequestToken });
        using var confirmed = await client.PostAsJsonAsync("/v1/auth/mfa/recovery:confirm", new { confirmationToken = owner.ConfirmationToken });
        Assert.Equal(HttpStatusCode.OK, continued.StatusCode);
        Assert.Equal(HttpStatusCode.OK, confirmed.StatusCode);
        using var continuedJson = JsonDocument.Parse(await continued.Content.ReadAsStringAsync());
        using var confirmedJson = JsonDocument.Parse(await confirmed.Content.ReadAsStringAsync());
        Assert.False(continuedJson.RootElement.GetProperty("success").GetBoolean());
        Assert.False(confirmedJson.RootElement.GetProperty("success").GetBoolean());
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(MfaRecoveryRequestStatus.Cancelled, (await database.Set<MfaRecoveryRequest>()
            .SingleAsync(row => row.UserId == owner.UserId.ToString())).Status);
        Assert.True((await database.Set<UserMfaConfiguration>().SingleAsync(row => row.UserId == owner.UserId)).IsEnabled);
        Assert.Equal(1, await database.Set<AuditLog>().IgnoreQueryFilters().CountAsync(row => row.UserId == owner.UserId &&
            row.ActionType == "Authentication.MfaRecoveryCancelled" && row.Success));
    }

    [Fact]
    public async Task AnonymousCancellationIsUnauthorized()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        using var response = await client.PostAsync(CancelPath, null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SubmittedForeignUserIdCannotRedirectOwnerCancellation()
    {
        using var factory = CreateFactory();
        var owner = await SeedAsync(factory);
        var foreign = await SeedAsync(factory, awaiting: true);
        using var client = OwnerClient(factory, owner);
        using var response = await client.PostAsJsonAsync(CancelPath, new { userId = foreign.UserId });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(MfaRecoveryRequestStatus.AwaitingConfirmation, (await database.Set<MfaRecoveryRequest>()
            .SingleAsync(row => row.UserId == foreign.UserId.ToString())).Status);
        Assert.True((await database.Set<UserMfaConfiguration>().SingleAsync(row => row.UserId == foreign.UserId)).IsEnabled);
    }

    [Fact]
    public async Task CancellationCommittedBeforeConfirmationWriteCannotBeResurrected()
    {
        var barrier = new PauseRecoveryWrite();
        using var factory = CreateFactory(barrier: barrier);
        var owner = await SeedAsync(factory, awaiting: true);
        using (var installation = factory.Services.CreateScope())
        {
            var options = installation.ServiceProvider.GetRequiredService<ApplicationDbContext>().GetService<IDbContextOptions>();
            if (options.FindExtension<CoreOptionsExtension>()?.Interceptors?.Contains(barrier) != true)
            {
                throw new InvalidOperationException("The deterministic recovery-write barrier is not installed.");
            }
        }
        using var client = factory.CreateClient();
        var confirming = client.PostAsJsonAsync("/v1/auth/mfa/recovery:confirm", new { confirmationToken = owner.ConfirmationToken });
        try
        {
            await barrier.Entered.Task.WaitAsync(TimeSpan.FromSeconds(30));
            using var cancellation = factory.Services.CreateScope();
            var database = cancellation.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await using var transaction = await database.Database.BeginTransactionAsync();
            var cancelled = await cancellation.ServiceProvider.GetRequiredService<IMfaRecoveryRequestStore>()
                .CancelLiveByUserAsync(owner.UserId.ToString(), CancellationToken.None);
            Assert.NotNull(cancelled);
            await transaction.CommitAsync();
        }
        finally { barrier.Release.TrySetResult(); }
        using var response = await confirming.WaitAsync(TimeSpan.FromSeconds(30));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        using var verify = factory.Services.CreateScope();
        var stored = verify.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var status = (await stored.Set<MfaRecoveryRequest>().SingleAsync(row => row.UserId == owner.UserId.ToString())).Status;
        var enabled = (await stored.Set<UserMfaConfiguration>().SingleAsync(row => row.UserId == owner.UserId)).IsEnabled;
        Console.WriteLine($"Recovery race: status={status}; factorEnabled={enabled}; responseSuccess={body.RootElement.GetProperty("success").GetBoolean()}");
        Assert.Equal(MfaRecoveryRequestStatus.Cancelled, status);
        Assert.True(enabled);
        Assert.False(body.RootElement.GetProperty("success").GetBoolean());
    }

    [Theory]
    [InlineData("request")]
    [InlineData("continue")]
    [InlineData("confirm")]
    [InlineData("cancel")]
    public async Task RequiredStageAuditFailureRollsBackTheStateAndQueuedMessages(string stage)
    {
        using var factory = CreateFactory(auditFails: true);
        var owner = await SeedAsync(factory, awaiting: stage is "confirm" or "cancel", pending: stage == "continue");
        using var client = stage == "cancel" ? OwnerClient(factory, owner) : factory.CreateClient();
        object payload = stage switch
        {
            "request" => new { owner.Email },
            "continue" => new { requestToken = owner.RequestToken },
            _ => new { confirmationToken = owner.ConfirmationToken }
        };
        using var response = await client.PostAsJsonAsync("/v1/auth/mfa/recovery:" + stage, payload);
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var action = stage switch
        {
            "request" => "Authentication.MfaRecoveryRequested",
            "continue" => "Authentication.MfaRecoveryContinued",
            "confirm" => "Authentication.MfaRecoveryConfirmed",
            _ => "Authentication.MfaRecoveryCancelled"
        };
        Mock.Get(factory.Services.GetRequiredService<IAuthenticationAuditEventSink>()).Verify(value => value.RecordAsync(
            It.Is<AuthenticationAuditEvent>(audit => audit.ActionType == action), It.IsAny<CancellationToken>()), Times.Once);
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var row = await database.Set<MfaRecoveryRequest>().SingleOrDefaultAsync(value => value.UserId == owner.UserId.ToString());
        if (stage == "request") { Assert.Null(row); }
        else { Assert.Equal(stage == "continue" ? MfaRecoveryRequestStatus.Pending : MfaRecoveryRequestStatus.AwaitingConfirmation, row!.Status); }
        Assert.True((await database.Set<UserMfaConfiguration>().SingleAsync(value => value.UserId == owner.UserId)).IsEnabled);
        Assert.Equal(owner.TokenVersion, (await database.Set<User>().SingleAsync(value => value.Id == owner.UserId)).TokenVersion);
        Assert.Equal(0, await database.Set<Notification>().IgnoreQueryFilters().CountAsync(value => value.RecipientId == owner.UserId &&
            (value.Type == NotificationType.MfaRecovery || value.Type == NotificationType.MfaRecoveryConfirmation)));
    }

    private WebApplicationFactory<Program> CreateFactory(bool auditFails = false, PauseRecoveryWrite? barrier = null) =>
        fixture.CreateFactory(builder => builder.ConfigureTestServices(services =>
        {
            services.PostConfigure<MfaOptions>(options => options.Enabled = true);
            services.PostConfigure<EmailDeliveryOptions>(options => options.Enabled = false);
            services.RemoveAll<MfaOptions>();
            services.AddSingleton(provider => provider.GetRequiredService<IOptions<MfaOptions>>().Value);
            services.RemoveAll<IMfaAttemptTrackingService>();
            services.AddScoped<IMfaAttemptTrackingService>(provider => ActivatorUtilities.CreateInstance<MfaAttemptTrackingService>(
                provider, provider.GetRequiredService<IOptions<MfaOptions>>().Value));
            if (barrier is not null)
            {
                services.AddDbContext<ApplicationDbContext>((_, options) => options.AddInterceptors(barrier));
            }
            if (auditFails)
            {
                var sink = new Mock<IAuthenticationAuditEventSink>(MockBehavior.Strict);
                sink.Setup(value => value.RecordAsync(It.IsAny<AuthenticationAuditEvent>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
                sink.Setup(value => value.RecordAsync(It.Is<AuthenticationAuditEvent>(audit => audit.ActionType.StartsWith("Authentication.MfaRecovery")),
                    It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("Synthetic required recovery audit outage"));
                services.RemoveAll<IAuthenticationAuditEventSink>();
                services.AddSingleton(sink.Object);
            }
        }));

    private static async Task<RecoveryOwner> SeedAsync(WebApplicationFactory<Program> factory, bool awaiting = false, bool pending = false)
    {
        var marker = Guid.NewGuid().ToString("N");
        var tenantId = Guid.NewGuid();
        var requestToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var confirmationToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        using var scope = factory.Services.CreateScope();
        var provider = scope.ServiceProvider;
        var database = provider.GetRequiredService<ApplicationDbContext>();
        var user = User.CreateOAuthUser("recovery-state-" + marker + "@example.test", "Recovery " + marker);
        user.VerifyEmail();
        database.Set<User>().Add(user);
        database.Set<Tenant>().Add(new Tenant { Id = tenantId, Name = "Recovery " + marker, Slug = "recovery-" + marker,
            AdminEmail = user.Email, IsActive = true });
        database.Set<TenantMember>().Add(new TenantMember { Id = Guid.NewGuid(), UserId = user.Id, TenantId = tenantId,
            Role = "Member", IsActive = true });
        database.Set<UserMfaConfiguration>().Add(new UserMfaConfiguration { Id = Guid.NewGuid(), UserId = user.Id,
            IsEnabled = true, IsSetupComplete = true, PreferredMethod = MfaMethod.Totp,
            TotpSecretKey = provider.GetRequiredService<IEncryptionService>().Encrypt(Convert.ToHexString(RandomNumberGenerator.GetBytes(20))) });
        if (awaiting || pending)
        {
            database.Set<MfaRecoveryRequest>().Add(new MfaRecoveryRequest { Id = Guid.NewGuid(), UserId = user.Id.ToString(),
                TenantId = tenantId.ToString(), RequestedAt = DateTimeOffset.UtcNow.AddDays(-8), CooldownEndsAt = DateTimeOffset.UtcNow.AddDays(-1),
                Status = awaiting ? MfaRecoveryRequestStatus.AwaitingConfirmation : MfaRecoveryRequestStatus.Pending,
                RequestTokenHash = Hash(requestToken), ConfirmationTokenHash = awaiting ? Hash(confirmationToken) : null,
                ConfirmationTokenExpiresAt = awaiting ? DateTimeOffset.UtcNow.AddHours(1) : null });
        }
        await database.SaveChangesAsync();
        return new RecoveryOwner(user.Id, tenantId, user.Email, requestToken, confirmationToken, user.TokenVersion);
    }

    private static HttpClient OwnerClient(WebApplicationFactory<Program> factory, RecoveryOwner owner)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("ApiPostgreSqlTest", "authenticated");
        client.DefaultRequestHeaders.Add("X-Test-User-Id", owner.UserId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Tenant-Id", owner.TenantId.ToString());
        client.DefaultRequestHeaders.Add("X-Tenant-Id", owner.TenantId.ToString());
        return client;
    }

    private static async Task<string> ReadNotificationTokenAsync(WebApplicationFactory<Program> factory, Guid userId, NotificationType type)
    {
        using var scope = factory.Services.CreateScope();
        var row = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Set<Notification>().IgnoreQueryFilters()
            .AsNoTracking().SingleAsync(value => value.RecipientId == userId && value.Type == type);
        Assert.StartsWith(NotificationMetadataProtector.ProtectedPrefix, row.Metadata);
        var plaintext = scope.ServiceProvider.GetRequiredService<NotificationMetadataProtector>().GetForRendering(row, type);
        using var metadata = JsonDocument.Parse(plaintext!);
        return metadata.RootElement.GetProperty("token").GetString()!;
    }

    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
    private sealed record RecoveryOwner(Guid UserId, Guid TenantId, string Email, string RequestToken, string ConfirmationToken, int TokenVersion);

    private sealed class PauseRecoveryWrite : DbCommandInterceptor
    {
        private int entered;
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private async Task PauseAsync(DbCommand command, CancellationToken cancellationToken)
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(command.CommandText, @"\bUPDATE\s+[^;\r\n]*mfa_recovery_requests\b",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase) &&
                Interlocked.CompareExchange(ref entered, 1, 0) == 0)
            {
                Entered.TrySetResult();
                await Release.Task.WaitAsync(cancellationToken);
            }
        }
        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            await PauseAsync(command, cancellationToken);
            return await base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
        public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            await PauseAsync(command, cancellationToken);
            return await base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}
