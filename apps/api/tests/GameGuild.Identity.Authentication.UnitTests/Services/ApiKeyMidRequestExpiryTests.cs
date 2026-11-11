using System.Security.Claims;
using FluentAssertions;
using GameGuild.Configuration.ApplicationLayer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

/// <summary>
///     API key expiry mid-request (issue #1122): a request that authenticated with a valid
///     key at the door must complete even when the key expires while the handler is still
///     running — the credential decision is made once, at the authentication middleware, and
///     the resulting principal (key id, scopes) is frozen for the request. Only NEW requests
///     re-evaluate the key against the clock and get the 401. These tests pin the entry-check
///     contract: the handler validates the store exactly once per request (framework result
///     caching), mid-request authorization consumes the frozen claims, and a fresh request
///     after expiry fails closed.
/// </summary>
public sealed class ApiKeyMidRequestExpiryTests
{
    private sealed class CountingApiKeyRepository(IApiKeyRepository inner) : IApiKeyRepository
    {
        public int KeyLookups;

        public Task<ApiKey?> GetByKeyHashAsync(string keyHash, CancellationToken cancellationToken = default)
        {
            KeyLookups++;
            return inner.GetByKeyHashAsync(keyHash, cancellationToken);
        }

        public Task<ApiKey?> GetByIdForUserAsync(Guid keyId, Guid userId, CancellationToken cancellationToken = default) =>
            inner.GetByIdForUserAsync(keyId, userId, cancellationToken);

        public Task<List<ApiKey>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
            inner.GetByUserIdAsync(userId, cancellationToken);

        public Task<List<ApiKey>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
            inner.GetActiveByUserIdAsync(userId, cancellationToken);

        public Task<ApiKey> AddAsync(ApiKey apiKey, CancellationToken cancellationToken = default) =>
            inner.AddAsync(apiKey, cancellationToken);

        public Task<ApiKey?> RevokeAsync(Guid keyId, Guid userId, string reason, CancellationToken cancellationToken = default) =>
            inner.RevokeAsync(keyId, userId, reason, cancellationToken);

        public Task<List<ApiKey>> RevokeCreatedByCascadeAsync(Guid creatorKeyId, string reason, CancellationToken cancellationToken = default) =>
            inner.RevokeCreatedByCascadeAsync(creatorKeyId, reason, cancellationToken);

        public Task RecordUsageAsync(ApiKey apiKey, CancellationToken cancellationToken = default) =>
            inner.RecordUsageAsync(apiKey, cancellationToken);

        public Task RotateAsync(ApiKey oldKey, ApiKey newKey, DateTime? graceEndsAt, CancellationToken cancellationToken = default) =>
            inner.RotateAsync(oldKey, newKey, graceEndsAt, cancellationToken);

        public Task FinalizeRotationRevocationAsync(ApiKey apiKey, CancellationToken cancellationToken = default) =>
            inner.FinalizeRotationRevocationAsync(apiKey, cancellationToken);

        public Task<int> FinalizeExpiredRotationRevocationsAsync(int batchSize) =>
            inner.FinalizeExpiredRotationRevocationsAsync(batchSize);

        public Task<int> FinalizeExpiredRotationRevocationsAsync(int batchSize, CancellationToken cancellationToken) =>
            inner.FinalizeExpiredRotationRevocationsAsync(batchSize, cancellationToken);
    }

    private sealed class TestApiKeyDbContext(DbContextOptions<TestApiKeyDbContext> options)
        : DbContext(options), IApplicationDbContext
    {
        public DbSet<ApiKey> ApiKeys => Set<ApiKey>();

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private static (ApiKeyRepository Repository, TestApiKeyDbContext Context) CreateRepository()
    {
        var options = new DbContextOptionsBuilder<TestApiKeyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var context = new TestApiKeyDbContext(options);
        return (new ApiKeyRepository(context), context);
    }

    private static async Task<ApiKeyAuthenticationHandler> CreateInitializedHandlerAsync(
        IApiKeyRepository repository, DefaultHttpContext context, string plaintext)
    {
        var options = new Mock<IOptionsMonitor<ApiKeyAuthenticationOptions>>();
        options.Setup(monitor => monitor.Get(It.IsAny<string>())).Returns(new ApiKeyAuthenticationOptions());
        options.SetupGet(monitor => monitor.CurrentValue).Returns(new ApiKeyAuthenticationOptions());
        var handler = new ApiKeyAuthenticationHandler(
            options.Object,
            NullLoggerFactory.Instance,
            System.Text.Encodings.Web.UrlEncoder.Default,
            repository);
        context.Request.Headers["X-API-Key"] = plaintext;
        await handler.InitializeAsync(
            new AuthenticationScheme(
                ApiKeyAuthenticationOptions.SchemeName,
                ApiKeyAuthenticationOptions.SchemeName,
                typeof(ApiKeyAuthenticationHandler)),
            context);
        return handler;
    }

    /// <summary>
    ///     Runs the mid-request scope authorization exactly as the authorization middleware
    ///     would for an in-flight endpoint: the requirement is evaluated against the
    ///     principal FROZEN at entry, never against a re-read credential store.
    /// </summary>
    private static async Task<bool> MidRequestScopeAuthorizesAsync(ClaimsPrincipal principal, string requiredScope)
    {
        var requirement = new ApiKeyScopeRequirement(requiredScope);
        var context = new AuthorizationHandlerContext([requirement], principal, resource: null);
        var handler = new ApiKeyScopeHandler(NullLogger<ApiKeyScopeHandler>.Instance);
        await handler.HandleAsync(context);
        return context.HasSucceeded;
    }

    [Fact]
    public async Task KeyExpiringDuringRequest_InFlightRequestCompletes_NewRequestIsRejected()
    {
        var userId = Guid.NewGuid();
        var (repository, context) = CreateRepository();
        var counting = new CountingApiKeyRepository(repository);
        var (apiKey, plaintext) = ApiKey.Create(userId, Guid.NewGuid(), "service", ["reports:read"]);
        await repository.AddAsync(apiKey);

        // ── Entry: the middleware authenticates the request with a valid key. ──────────
        var requestContext = new DefaultHttpContext();
        var handler = await CreateInitializedHandlerAsync(counting, requestContext, plaintext);
        var entry = await handler.AuthenticateAsync();

        entry.Succeeded.Should().BeTrue("the request started with a valid key");
        var frozenPrincipal = entry.Principal!;
        frozenPrincipal.FindFirst("scope")!.Value.Should().Be("reports:read");

        // ── Mid-flight: the key expires WHILE the handler is processing. ───────────────
        var tracked = await repository.GetByKeyHashAsync(apiKey.KeyHash);
        tracked!.ExpiresAt = SystemClock.UtcNow.AddMinutes(-1);
        await context.SaveChangesAsync();

        // The long-running operation keeps executing with the frozen principal: mid-request
        // authorization re-consumes the entry claims (never the credential store), so the
        // operation finishes on its own terms — no 401 mid-handler.
        (await MidRequestScopeAuthorizesAsync(frozenPrincipal, "reports:read"))
            .Should().BeTrue("mid-request authorization uses the frozen entry principal");

        // A re-authentication inside the SAME request is served from the per-request cache:
        // the handler never re-reads the (now expired) key — the mechanism that makes
        // expiry invisible to in-flight work.
        var reAuthenticated = await handler.AuthenticateAsync();
        reAuthenticated.Succeeded.Should().BeTrue("the in-flight request keeps its entry-time ticket");
        reAuthenticated.Ticket.Should().BeSameAs(entry.Ticket, "the framework returns the cached entry result");
        counting.KeyLookups.Should().Be(1, "the credential store is consulted exactly once per request");

        // ── After the request: only NEW requests re-evaluate the clock. ────────────────
        var newRequestContext = new DefaultHttpContext();
        var newHandler = await CreateInitializedHandlerAsync(counting, newRequestContext, plaintext);
        var newEntry = await newHandler.AuthenticateAsync();

        newEntry.Succeeded.Should().BeFalse("an expired key must fail closed for new requests");
        newEntry.Failure!.Message.Should().Contain("inactive or expired");
        counting.KeyLookups.Should().Be(2, "the new request performed its own single lookup");
    }

    [Fact]
    public async Task KeyValidThroughout_RepeatedInRequestChecksNeverReReadTheStore()
    {
        var (repository, _) = CreateRepository();
        var counting = new CountingApiKeyRepository(repository);
        var (apiKey, plaintext) = ApiKey.Create(Guid.NewGuid(), Guid.NewGuid(), "service", ["reports:read"]);
        await repository.AddAsync(apiKey);

        var requestContext = new DefaultHttpContext();
        var handler = await CreateInitializedHandlerAsync(counting, requestContext, plaintext);

        // The middleware, the authorization layer and a handler middleware all "check"
        // authentication during the request — the handler answers each from its cache.
        var first = await handler.AuthenticateAsync();
        var second = await handler.AuthenticateAsync();
        var third = await handler.AuthenticateAsync();

        first.Succeeded.Should().BeTrue();
        second.Succeeded.Should().BeTrue();
        third.Succeeded.Should().BeTrue();
        second.Ticket.Should().BeSameAs(first.Ticket);
        third.Ticket.Should().BeSameAs(first.Ticket);
        counting.KeyLookups.Should().Be(1, "however many in-flight checks run, the store is read once");
    }

    [Fact]
    public async Task ExpiredFromTheStart_NeverAuthenticates()
    {
        var (repository, _) = CreateRepository();
        var counting = new CountingApiKeyRepository(repository);
        var (apiKey, plaintext) = ApiKey.Create(
            Guid.NewGuid(), Guid.NewGuid(), "service", ["reports:read"],
            expiresAt: SystemClock.UtcNow.AddMinutes(-5));
        await repository.AddAsync(apiKey);

        var requestContext = new DefaultHttpContext();
        var handler = await CreateInitializedHandlerAsync(counting, requestContext, plaintext);
        var entry = await handler.AuthenticateAsync();

        entry.Succeeded.Should().BeFalse("a key that was already expired when the request started is rejected at the door");
        entry.Failure!.Message.Should().Contain("inactive or expired");
    }
}
