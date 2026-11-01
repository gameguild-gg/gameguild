using System.Security.Claims;
using System.Text.Encodings.Web;
using FluentAssertions;
using GameGuild.Configuration.PresentationLayer.Authorization;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

/// <summary>
///     API key scoped cross-tenant access (issue #1132): a key minted for tenant A must
///     only ever act inside tenant A. The chain under test is the real production chain:
///     the real <see cref="ApiKeyAuthenticationHandler" /> authenticates the key against a
///     real <see cref="ApiKeyRepository" /> (in-memory) and stamps the key's OWN tenant into
///     the <c>tenant_id</c> claim; the real <see cref="AuthorizationTenantResolver" />
///     resolves the acting tenant from that principal exactly as the actor-context
///     middleware does; and
///     <see cref="TenantValidationExtensions.ValidateTenantAccess" /> must then deny any
///     operation that names tenant B. The key holder cannot switch tenants by asserting
///     another id in the request — the tenant is a property of the key, not the request.
/// </summary>
public sealed class ApiKeyCrossTenantAccessTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _tenantA = Guid.NewGuid();
    private readonly Guid _tenantB = Guid.NewGuid();

    private static async Task<AuthenticateResult> AuthenticateInMemoryAsync(ApiKey apiKey, string plaintext, Mock<IAuthenticationAuditEventSink>? auditSink = null)
    {
        await using var context = CreateContext();
        var repository = new ApiKeyRepository(context);
        await repository.AddAsync(apiKey);

        var options = new Mock<IOptionsMonitor<ApiKeyAuthenticationOptions>>();
        options.Setup(x => x.Get(It.IsAny<string>())).Returns(new ApiKeyAuthenticationOptions());
        options.SetupGet(x => x.CurrentValue).Returns(new ApiKeyAuthenticationOptions());

        var handler = auditSink is null
            ? new ApiKeyAuthenticationHandler(
                options.Object, NullLoggerFactory.Instance, UrlEncoder.Default, repository)
            : new ApiKeyAuthenticationHandler(
                options.Object, NullLoggerFactory.Instance, UrlEncoder.Default, repository, auditSink.Object);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-API-Key"] = plaintext;
        await handler.InitializeAsync(
            new AuthenticationScheme(
                ApiKeyAuthenticationOptions.SchemeName,
                ApiKeyAuthenticationOptions.SchemeName,
                typeof(ApiKeyAuthenticationHandler)),
            httpContext);
        return await handler.AuthenticateAsync();
    }

    private static TestApiKeyDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestApiKeyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new TestApiKeyDbContext(options);
    }

    /// <summary>Resolves the acting tenant the way ActorContextMiddleware does for an API-key request.</summary>
    private static async Task<Guid?> ResolveActingTenantAsync(ClaimsPrincipal principal)
    {
        var tenancy = new TenancyOptions();
        // Request-based resolution off: the tenant must come from the authenticated
        // principal (the API key's own claim), not from anything the caller asserts.
        tenancy.Resolution.EnableHeader = false;
        tenancy.Resolution.EnableSubdomain = false;
        tenancy.Resolution.EnableQueryString = false;
        var resolver = new AuthorizationTenantResolver(
            Options.Create(tenancy),
            Options.Create(new AuthorizationTokenOptions()));

        var httpContext = new DefaultHttpContext { User = principal };
        var resolved = await resolver.ResolveTenantIdAsync(httpContext);
        return Guid.TryParse(resolved, out var tenantId) ? tenantId : null;
    }

    private static ActorContext ActorFor(Guid userId, Guid? tenantId) =>
        ActorContextBuilder.ForUser(userId)
            .WithTenantId(tenantId)
            .WithAuthScheme(ApiKeyAuthenticationOptions.SchemeName)
            .Build();

    private static IActorContextAccessor AccessorFor(ActorContext context)
    {
        var accessor = new Mock<IActorContextAccessor>();
        accessor.SetupGet(x => x.ActorContext).Returns(context);
        return accessor.Object;
    }

    [Fact]
    public async Task KeyForTenantA_AuthenticatesWithTenantAStampedIntoThePrincipal()
    {
        var (apiKey, plaintext) = ApiKey.Create(_userId, _tenantA, "tenant-a-key", ["reports:read"]);

        var result = await AuthenticateInMemoryAsync(apiKey, plaintext);

        result.Succeeded.Should().BeTrue();
        result.Principal!.FindFirst("tenant_id")!.Value.Should().Be(_tenantA.ToString(),
            "the tenant claim is a property of the key, minted at authentication time");
    }

    [Fact]
    public async Task KeyForTenantA_ResolvesActingTenantA_EvenWhenTenantBExists()
    {
        var (apiKey, plaintext) = ApiKey.Create(_userId, _tenantA, "tenant-a-key", ["reports:read"]);
        var authentication = await AuthenticateInMemoryAsync(apiKey, plaintext);

        var actingTenant = await ResolveActingTenantAsync(authentication.Principal!);

        actingTenant.Should().Be(_tenantA,
            "tenant resolution for an API-key request reads the key's own tenant claim");
    }

    [Fact]
    public async Task KeyForTenantA_OperationAgainstTenantB_IsDeniedAsCrossTenant()
    {
        var (apiKey, plaintext) = ApiKey.Create(_userId, _tenantA, "tenant-a-key", ["reports:read"]);
        var authentication = await AuthenticateInMemoryAsync(apiKey, plaintext);
        var actingTenant = await ResolveActingTenantAsync(authentication.Principal!);
        var accessor = AccessorFor(ActorFor(_userId, actingTenant));

        var denial = accessor.ValidateTenantAccess(_tenantB, "read tenant-B reports");

        denial.IsValid.Should().BeFalse("a tenant-A key must never operate on tenant B");
        denial.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        denial.ErrorDetails.Should().NotBeNull();
        var code = denial.ErrorDetails!.GetType().GetProperty("code")!.GetValue(denial.ErrorDetails);
        code.Should().Be("TENANT_MISMATCH");
    }

    [Fact]
    public async Task KeyForTenantA_OperationAgainstTenantA_IsAllowed()
    {
        var (apiKey, plaintext) = ApiKey.Create(_userId, _tenantA, "tenant-a-key", ["reports:read"]);
        var authentication = await AuthenticateInMemoryAsync(apiKey, plaintext);
        var actingTenant = await ResolveActingTenantAsync(authentication.Principal!);
        var accessor = AccessorFor(ActorFor(_userId, actingTenant));

        var allowed = accessor.ValidateTenantAccess(_tenantA, "read tenant-A reports");

        allowed.IsValid.Should().BeTrue("the key's own tenant is exactly what it is scoped to");
        allowed.ToActionResult().Should().BeNull();
    }

    [Fact]
    public async Task KeyForTenantA_TenantBOperation_ReturnsForbiddenActionResult()
    {
        var (apiKey, plaintext) = ApiKey.Create(_userId, _tenantA, "tenant-a-key", ["reports:read"]);
        var authentication = await AuthenticateInMemoryAsync(apiKey, plaintext);
        var actingTenant = await ResolveActingTenantAsync(authentication.Principal!);
        var accessor = AccessorFor(ActorFor(_userId, actingTenant));

        var actionResult = accessor.ValidateTenantAccessAsActionResult(_tenantB, "export tenant-B data");

        actionResult.Should().NotBeNull().And.BeOfType<ObjectResult>();
        ((ObjectResult)actionResult!).StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task KeyForTenantA_TamperedPlaintext_DoesNotAuthenticateAtAll()
    {
        // Mutation of the key material cannot land on tenant B either: any altered key is
        // simply unknown (hash lookup misses), so there is no principal to act with.
        var (apiKey, _) = ApiKey.Create(_userId, _tenantA, "tenant-a-key", ["reports:read"]);

        var result = await AuthenticateInMemoryAsync(apiKey, "gg_live_tampered-key-material-that-hashes-to-nothing");

        result.Succeeded.Should().BeFalse("an altered key never matches any stored hash");
        result.Principal.Should().BeNull("no principal means no tenant context to abuse");
    }

    [Fact]
    public async Task KeyForTenantA_SuccessfulUse_IsAuditedAgainstTenantA()
    {
        var (apiKey, plaintext) = ApiKey.Create(_userId, _tenantA, "tenant-a-key", ["reports:read"]);
        AuthenticationAuditEvent? audited = null;
        var auditSink = new Mock<IAuthenticationAuditEventSink>();
        auditSink
            .Setup(sink => sink.RecordAsync(It.IsAny<AuthenticationAuditEvent>(), It.IsAny<CancellationToken>()))
            .Callback((AuthenticationAuditEvent auditEvent, CancellationToken _) => audited = auditEvent)
            .Returns(Task.CompletedTask);

        var result = await AuthenticateInMemoryAsync(apiKey, plaintext, auditSink);

        result.Succeeded.Should().BeTrue();
        audited.Should().NotBeNull();
        audited!.TenantId.Should().Be(_tenantA,
            "usage telemetry must attribute the key to its own tenant, never to the requested one");
    }

    private sealed class TestApiKeyDbContext(DbContextOptions<TestApiKeyDbContext> options)
        : DbContext(options), IApplicationDbContext
    {
        public DbSet<ApiKey> ApiKeys => Set<ApiKey>();

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
