using System.Net;
using System.Security.Claims;
using FluentAssertions;
using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

/// <summary>
///     Issue #1192 hardening: IPv6-mapped whitelist normalization, per-source brute-force
///     lockout on key validation, and the creator linkage that lets revoking a leaked
///     api_keys:manage key cascade to the keys it minted.
/// </summary>
public sealed class ApiKeyHardeningTests
{
    [Fact]
    public async Task WhitelistEntry_MatchesAnIpv6MappedRemoteAddress()
    {
        var userId = Guid.NewGuid();
        await using var context = CreateContext();
        var repository = new ApiKeyRepository(context);
        var (apiKey, plaintext) = ApiKey.Create(userId, Guid.NewGuid(), "integration", ["read"], ipWhitelist: "203.0.113.7");
        await repository.AddAsync(apiKey);

        // Kestrel reports IPv4 callers on dual-stack sockets as ::ffff:203.0.113.7.
        var result = await AuthenticateAsync(repository, plaintext, remoteIp: IPAddress.Parse("::ffff:203.0.113.7"));

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task WhitelistEntryInMappedForm_MatchesPlainIpv4RemoteAddress()
    {
        var userId = Guid.NewGuid();
        await using var context = CreateContext();
        var repository = new ApiKeyRepository(context);
        var (apiKey, plaintext) = ApiKey.Create(userId, Guid.NewGuid(), "integration", ["read"], ipWhitelist: "::ffff:203.0.113.7");
        await repository.AddAsync(apiKey);

        var result = await AuthenticateAsync(repository, plaintext, remoteIp: IPAddress.Parse("203.0.113.7"));

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task WhitelistStillDeniesOtherAddresses_AfterNormalization()
    {
        var userId = Guid.NewGuid();
        await using var context = CreateContext();
        var repository = new ApiKeyRepository(context);
        var (apiKey, plaintext) = ApiKey.Create(userId, Guid.NewGuid(), "integration", ["read"], ipWhitelist: "203.0.113.7");
        await repository.AddAsync(apiKey);

        var result = await AuthenticateAsync(repository, plaintext, remoteIp: IPAddress.Parse("198.51.100.9"));

        result.Succeeded.Should().BeFalse();
        result.Failure!.Message.Should().Contain("not authorized from this IP address");
    }

    [Fact]
    public async Task RepeatedUnknownKeys_LockTheSourceBeforeTheCredentialLookup()
    {
        var userId = Guid.NewGuid();
        await using var context = CreateContext();
        var repository = new ApiKeyRepository(context);
        var (apiKey, plaintext) = ApiKey.Create(userId, Guid.NewGuid(), "integration", ["read"]);
        await repository.AddAsync(apiKey);
        var remote = IPAddress.Parse("203.0.113.77");
        var cache = NewDistributedCache();

        for (var attempt = 0; attempt < ApiKeyAuthenticationHandler.LockoutThreshold; attempt++)
        {
            var rejected = await AuthenticateAsync(repository, "gg_live_invalid", remote, cache);
            rejected.Succeeded.Should().BeFalse();
        }

        // The source is locked: even a VALID key is refused before the store is touched.
        var locked = await AuthenticateAsync(repository, plaintext, remote, cache);
        locked.Succeeded.Should().BeFalse();
        locked.Failure!.Message.Should().Contain("Too many invalid API key attempts");

        // The lock is per source: another address is unaffected.
        var otherSource = await AuthenticateAsync(repository, plaintext, IPAddress.Parse("198.51.100.1"), cache);
        otherSource.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task AValidKey_ClearsTheFailureBudgetForItsSource()
    {
        var userId = Guid.NewGuid();
        await using var context = CreateContext();
        var repository = new ApiKeyRepository(context);
        var (apiKey, plaintext) = ApiKey.Create(userId, Guid.NewGuid(), "integration", ["read"]);
        await repository.AddAsync(apiKey);
        var remote = IPAddress.Parse("203.0.113.78");
        var cache = NewDistributedCache();

        for (var attempt = 0; attempt < ApiKeyAuthenticationHandler.LockoutThreshold - 1; attempt++)
        {
            (await AuthenticateAsync(repository, "gg_live_invalid", remote, cache)).Succeeded.Should().BeFalse();
        }

        // A successful validation resets the counter, so the budget is not cumulative forever.
        (await AuthenticateAsync(repository, plaintext, remote, cache)).Succeeded.Should().BeTrue();
        for (var attempt = 0; attempt < ApiKeyAuthenticationHandler.LockoutThreshold - 1; attempt++)
        {
            (await AuthenticateAsync(repository, "gg_live_invalid", remote, cache)).Succeeded.Should().BeFalse();
        }
        // Threshold - 1 failures after the reset, then a valid key still succeeds.
        (await AuthenticateAsync(repository, plaintext, remote, cache)).Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task RevokingAKey_CascadesToKeysItMinted_Transitively()
    {
        await using var context = CreateContext();
        var repository = new ApiKeyRepository(context);
        var userId = Guid.NewGuid();

        var (leaked, leakedPlaintext) = ApiKey.Create(userId, Guid.NewGuid(), "leaked", ["api_keys:manage"]);
        var (minted, _) = ApiKey.Create(userId, Guid.NewGuid(), "minted", ["*"]);
        minted.CreatedByApiKeyId = leaked.Id;
        var (grandChild, _) = ApiKey.Create(userId, Guid.NewGuid(), "minted-by-minted", ["*"]);
        grandChild.CreatedByApiKeyId = minted.Id;
        var (independent, _) = ApiKey.Create(userId, Guid.NewGuid(), "independent", ["read"]);
        await repository.AddAsync(leaked);
        await repository.AddAsync(minted);
        await repository.AddAsync(grandChild);
        await repository.AddAsync(independent);

        // The RevokeApiKeyHandler sequence: revoke the compromised key itself, then cascade.
        await repository.RevokeAsync(leaked.Id, userId, "User revoked");
        await repository.RevokeCreatedByCascadeAsync(leaked.Id, "Cascade: creator revoked");

        (await repository.GetByKeyHashAsync(leaked.KeyHash))!.IsActive.Should().BeFalse();
        (await repository.GetByKeyHashAsync(minted.KeyHash))!.IsActive.Should().BeFalse("keys minted by the revoked key die with it");
        (await repository.GetByKeyHashAsync(grandChild.KeyHash))!.IsActive.Should().BeFalse("the cascade is transitive");
        (await repository.GetByKeyHashAsync(independent.KeyHash))!.IsActive.Should().BeTrue("unrelated keys are untouched");
        (await repository.GetByKeyHashAsync(minted.KeyHash))!.RevocationReason.Should().Contain("Cascade");

        _ = leakedPlaintext;
    }

    [Fact]
    public async Task CreateApiKeyHandler_RecordsTheCreatorWhenAuthenticatedByAnApiKey()
    {
        var creatorKeyId = Guid.NewGuid();
        var repository = new Mock<IApiKeyRepository>();
        repository.Setup(r => r.AddAsync(It.IsAny<ApiKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApiKey key, CancellationToken _) => key);
        var actor = new ActorContextAccessor();
        actor.SetActorContext(ActorContextBuilder.ForUser(Guid.NewGuid()).WithTenantId(Guid.NewGuid())
            .WithAttribute("session_id", Guid.NewGuid().ToString()).Build());
        var receipts = new Mock<IStepUpReceiptService>();
        receipts.Setup(r => r.ConsumeAsync(It.IsAny<StepUpOperationBinding>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var http = new Mock<IHttpContextAccessor>();
        http.Setup(h => h.HttpContext).Returns(new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("api_key_id", creatorKeyId.ToString())
            ], ApiKeyAuthenticationOptions.SchemeName))
        });

        var handler = new CreateApiKeyHandler(
            repository.Object,
            actor,
            receipts.Object,
            NullLogger<CreateApiKeyHandler>.Instance,
            httpContextAccessor: http.Object);

        var result = await handler.Handle(
            new CreateApiKeyCommand { Name = "minted", Scopes = ["read"], StepUpReceipt = "test-receipt" },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        repository.Verify(r => r.AddAsync(It.Is<ApiKey>(key => key.CreatedByApiKeyId == creatorKeyId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateApiKeyHandler_InteractiveSignIn_LeavesNoCreatorLinkage()
    {
        var repository = new Mock<IApiKeyRepository>();
        repository.Setup(r => r.AddAsync(It.IsAny<ApiKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApiKey key, CancellationToken _) => key);
        var actor = new ActorContextAccessor();
        actor.SetActorContext(ActorContextBuilder.ForUser(Guid.NewGuid()).WithTenantId(Guid.NewGuid())
            .WithAttribute("session_id", Guid.NewGuid().ToString()).Build());
        var receipts = new Mock<IStepUpReceiptService>();
        receipts.Setup(r => r.ConsumeAsync(It.IsAny<StepUpOperationBinding>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        // A Bearer principal carrying an api_key_id claim must NOT create linkage: the claim is
        // only honored on the ApiKey scheme.
        var http = new Mock<IHttpContextAccessor>();
        http.Setup(h => h.HttpContext).Returns(new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("api_key_id", Guid.NewGuid().ToString())
            ], "Bearer"))
        });

        var handler = new CreateApiKeyHandler(
            repository.Object,
            actor,
            receipts.Object,
            NullLogger<CreateApiKeyHandler>.Instance,
            httpContextAccessor: http.Object);

        var result = await handler.Handle(
            new CreateApiKeyCommand { Name = "interactive", Scopes = ["read"], StepUpReceipt = "test-receipt" },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        repository.Verify(r => r.AddAsync(It.Is<ApiKey>(key => key.CreatedByApiKeyId == null), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static async Task<AuthenticateResult> AuthenticateAsync(
        IApiKeyRepository repository,
        string plaintext,
        IPAddress? remoteIp = null,
        IDistributedCache? cache = null)
    {
        var options = new Mock<IOptionsMonitor<ApiKeyAuthenticationOptions>>();
        options.Setup(x => x.Get(It.IsAny<string>())).Returns(new ApiKeyAuthenticationOptions());
        options.SetupGet(x => x.CurrentValue).Returns(new ApiKeyAuthenticationOptions());
        var handler = new ApiKeyAuthenticationHandler(
            options.Object,
            NullLoggerFactory.Instance,
            System.Text.Encodings.Web.UrlEncoder.Default,
            repository,
            auditEventSink: null,
            distributedCache: cache);
        var context = new DefaultHttpContext();
        context.Request.Headers["X-API-Key"] = plaintext;
        context.Connection.RemoteIpAddress = remoteIp;
        await handler.InitializeAsync(
            new AuthenticationScheme(
                ApiKeyAuthenticationOptions.SchemeName,
                ApiKeyAuthenticationOptions.SchemeName,
                typeof(ApiKeyAuthenticationHandler)),
            context);
        return await handler.AuthenticateAsync();
    }

    private static MemoryDistributedCache NewDistributedCache() =>
        new(Microsoft.Extensions.Options.Options.Create(new MemoryDistributedCacheOptions()));

    private static TestApiKeyDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestApiKeyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new TestApiKeyDbContext(options);
    }

    private sealed class TestApiKeyDbContext(DbContextOptions<TestApiKeyDbContext> options)
        : DbContext(options), IApplicationDbContext
    {
        public DbSet<ApiKey> ApiKeys => Set<ApiKey>();

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
