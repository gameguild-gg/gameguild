using FluentAssertions;
using GameGuild;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Payments.UnitTests.Security;

/// <summary>
///     Encrypted-at-rest gateway credentials (issue #413): the store encrypts plaintext with
///     DataProtection (AES-256 under the key ring) before persisting, never stores or returns
///     plaintext from provisioning, rotates the active row atomically, fails closed when a
///     stored payload cannot be decrypted, and the provisioning command rejects providers
///     without a registered adapter.
/// </summary>
public class GatewayCredentialStoreTests
{
    private sealed class CredentialTestDbContext(DbContextOptions<CredentialTestDbContext> options)
        : DbContext(options), IApplicationDbContext
    {
        public DbContextOptions<CredentialTestDbContext> ContextOptions { get; } = options;

        public DbSet<GatewayCredential> Credentials { get; set; } = null!;

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
            => Database.BeginTransactionAsync(cancellationToken);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GatewayCredential>(builder =>
            {
                builder.Property(credential => credential.ProviderKey).HasMaxLength(50);
                builder.Property(credential => credential.SecretName).HasMaxLength(100);
                builder.Property(credential => credential.Fingerprint).HasMaxLength(64);
            });
        }
    }

    private static async Task<CredentialTestDbContext> CreateContextAsync()
    {
        var context = new CredentialTestDbContext(
            new DbContextOptionsBuilder<CredentialTestDbContext>()
                .UseInMemoryDatabase($"gateway-credentials-{Guid.NewGuid():N}")
                .Options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    private static (IGatewayCredentialStore Store, IDataProtectionProvider Protection) CreateStore(
        CredentialTestDbContext context)
    {
        var protection = new EphemeralDataProtectionProvider();
        var scopeFactory = new ServiceCollection()
            .AddScoped<IApplicationDbContext>(_ => new CredentialTestDbContext(context.ContextOptions))
            .BuildServiceProvider()
            .GetRequiredService<IServiceScopeFactory>();

        return (
            new GatewayCredentialStore(scopeFactory, protection, NullLogger<GatewayCredentialStore>.Instance),
            protection);
    }

    [Fact]
    public async Task Provision_EncryptsPlaintext_BeforePersisting()
    {
        await using var context = await CreateContextAsync();
        var (store, _) = CreateStore(context);

        await store.ProvisionAsync("stripe", "api_key", "sk_live_super_secret", provisionedByUserId: null);

        var row = await context.Credentials.SingleAsync();
        row.EncryptedValue.Should().NotBeNullOrWhiteSpace();
        row.EncryptedValue.Should().NotContain("sk_live_super_secret",
            "the row must never contain the plaintext credential");
        row.IsActive.Should().BeTrue();
        row.ProviderKey.Should().Be("stripe");
        row.SecretName.Should().Be("api_key");
    }

    [Fact]
    public async Task Provision_ThenGet_RoundTripsThePlaintext_AndComputesAMatchingFingerprint()
    {
        await using var context = await CreateContextAsync();
        var (store, _) = CreateStore(context);

        var provisioned = await store.ProvisionAsync("paypal", "client_secret", "secret-value-1", Guid.NewGuid());
        var resolved = await store.GetActiveAsync("paypal", "client_secret");

        resolved.Should().NotBeNull();
        resolved!.Plaintext.Should().Be("secret-value-1");
        resolved.Fingerprint.Should().Be(provisioned.Fingerprint);
        resolved.Fingerprint.Should().Be(GatewayCredentialStore.ComputeFingerprint("secret-value-1"));
    }

    [Fact]
    public async Task Provision_RotatesTheActiveRow_AtMostOneActivePerSlot()
    {
        await using var context = await CreateContextAsync();
        var (store, _) = CreateStore(context);

        var first = await store.ProvisionAsync("stripe", "api_key", "first-key", null);
        var second = await store.ProvisionAsync("stripe", "api_key", "second-key", null);

        second.DeactivatedPrevious.Should().BeTrue();
        first.Fingerprint.Should().NotBe(second.Fingerprint);

        var rows = await context.Credentials.ToListAsync();
        rows.Count(row => row.IsActive).Should().Be(1, "at most one active credential per slot");
        rows.Count(row => !row.IsActive).Should().Be(1, "the previous row is retained for audit");

        var resolved = await store.GetActiveAsync("stripe", "api_key");
        resolved!.Plaintext.Should().Be("second-key");
    }

    [Fact]
    public async Task GetActive_ReturnsNull_WhenNoCredentialExists()
    {
        await using var context = await CreateContextAsync();
        var (store, _) = CreateStore(context);

        var resolved = await store.GetActiveAsync("adyen", "api_key");

        resolved.Should().BeNull();
    }

    [Fact]
    public async Task GetActive_FailsClosed_WhenThePayloadCannotBeDecrypted()
    {
        await using var context = await CreateContextAsync();
        // Provision with one key ring, then read with a different one (simulated key-ring loss).
        var (store, _) = CreateStore(context);
        await store.ProvisionAsync("stripe", "api_key", "sk_live_super_secret", null);

        var otherProtection = new EphemeralDataProtectionProvider();
        var scopeFactory = new ServiceCollection()
            .AddScoped<IApplicationDbContext>(_ => new CredentialTestDbContext(context.ContextOptions))
            .BuildServiceProvider()
            .GetRequiredService<IServiceScopeFactory>();
        var otherKeyRingStore = new GatewayCredentialStore(
            scopeFactory,
            otherProtection,
            NullLogger<GatewayCredentialStore>.Instance);

        var resolved = await otherKeyRingStore.GetActiveAsync("stripe", "api_key");

        resolved.Should().BeNull("an undecryptable credential must surface as absent, never as a guess");
    }

    [Fact]
    public async Task Deactivate_ReturnsTrueOnce_ThenFalseWhenNothingIsActive()
    {
        await using var context = await CreateContextAsync();
        var (store, _) = CreateStore(context);
        await store.ProvisionAsync("braintree", "private_key", "pk-value", null);

        (await store.DeactivateAsync("braintree", "private_key", null)).Should().BeTrue();
        (await store.DeactivateAsync("braintree", "private_key", null)).Should().BeFalse("deactivation is idempotent");
        (await store.GetActiveAsync("braintree", "private_key")).Should().BeNull();
    }

    [Fact]
    public async Task Resolver_PrefersTheEncryptedStore_OverTheConfigurationFallback()
    {
        await using var context = await CreateContextAsync();
        var (store, _) = CreateStore(context);
        await store.ProvisionAsync("adyen", "api_key", "stored-key", null);
        var resolver = new GatewayCredentialResolver(
            store,
            NullLogger<GatewayCredentialResolver>.Instance);

        (await resolver.ResolveAsync("adyen", "api_key", "config-key")).Should().Be("stored-key");
        (await resolver.ResolveAsync("stripe", "api_key", "config-key")).Should().Be("config-key",
            "slots without a stored credential fall back to configuration");
        (await resolver.ResolveAsync("stripe", "api_key")).Should().BeNull(
            "no store record and no fallback resolves to null so callers fail closed");
    }

    [Fact]
    public async Task ProvisionCommand_FailsClosed_ForProvidersWithoutARegisteredAdapter()
    {
        var store = new Mock<IGatewayCredentialStore>();
        var registry = new FakeGatewayRegistryForCredentials(new FakeAdapterGateway("stripe"));
        var handler = new ProvisionGatewayCredentialCommandHandler(
            store.Object,
            registry,
            ActorAccessor(),
            NullLogger<ProvisionGatewayCredentialCommandHandler>.Instance);

        var act = () => handler.Handle(
            new ProvisionGatewayCredentialCommand("not-a-gateway", "api_key", "value"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not a registered payment gateway*");
        store.Verify(never => never.ProvisionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProvisionCommand_AcceptsRegisteredProviders()
    {
        var store = new Mock<IGatewayCredentialStore>();
        store
            .Setup(client => client.ProvisionAsync("adyen", "api_key", "value", It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GatewayCredentialProvisionResult("adyen", "api_key", "fingerprint", SystemClock.UtcNow, DeactivatedPrevious: false));

        var handler = new ProvisionGatewayCredentialCommandHandler(
            store.Object,
            new FakeGatewayRegistryForCredentials(new FakeAdapterGateway("adyen")),
            ActorAccessor(),
            NullLogger<ProvisionGatewayCredentialCommandHandler>.Instance);

        var result = await handler.Handle(
            new ProvisionGatewayCredentialCommand("adyen", "api_key", "value"),
            CancellationToken.None);

        result.ProviderKey.Should().Be("adyen");
        result.Fingerprint.Should().Be("fingerprint");
    }

    private static GameGuild.Identity.Context.Actors.IActorContextAccessor ActorAccessor()
    {
        var accessor = new Mock<GameGuild.Identity.Context.Actors.IActorContextAccessor>();
        accessor.SetupGet(instance => instance.ActorContext)
            .Returns(new GameGuild.Identity.Context.Actors.ActorContext
            {
                ActorKind = GameGuild.Identity.Context.Actors.ActorKind.User,
                SubjectId = Guid.NewGuid().ToString(),
                Roles = new HashSet<string>(),
                Permissions = new HashSet<string>(),
                TypedAttributes = GameGuild.Identity.Context.Actors.ActorAttributes.Empty,
                IsAuthenticated = true
            });
        return accessor.Object;
    }

    private sealed class FakeGatewayRegistryForCredentials(params IPaymentGateway[] gateways) : IGatewayRegistry
    {
        public IReadOnlyList<IPaymentGateway> Gateways { get; } = gateways;

        public IPaymentGateway? Find(string providerId) =>
            string.IsNullOrWhiteSpace(providerId)
                ? null
                : Gateways.FirstOrDefault(gateway =>
                    string.Equals(gateway.ProviderId, providerId, StringComparison.OrdinalIgnoreCase));
    }

    private sealed class FakeAdapterGateway(string providerId) : IPaymentGateway
    {
        public string ProviderId { get; } = providerId;
        public string DisplayName => $"Fake {ProviderId}";
        public bool IsEnabled => true;

        public Task<GatewayPaymentResult> ProcessPaymentAsync(GatewayPaymentRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<GatewayPaymentResult> GetPaymentAsync(string externalTransactionId, CancellationToken cancellationToken = default, string? providerId = null) =>
            throw new NotSupportedException();

        public Task<GatewayPaymentCancellationResult> CancelPaymentAsync(string externalTransactionId, CancellationToken cancellationToken = default, string? providerId = null) =>
            throw new NotSupportedException();

        public Task<GatewayRefundResult> ProcessRefundAsync(GatewayRefundRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<GatewayDisputeActionResult> AcceptDisputeAsync(GatewayDisputeActionRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<GatewayDisputeActionResult> ChallengeDisputeAsync(GatewayDisputeChallengeRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> ValidateWebhookSignatureAsync(string payload, string signature, string secret) =>
            Task.FromResult(false);

        public Task<GatewayCustomerResult> CreateCustomerAsync(GatewayCustomerRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<GatewayPaymentMethodResult> CreatePaymentMethodAsync(GatewayPaymentMethodRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<GatewayCancellationResult> CancelSubscriptionAsync(string externalSubscriptionId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
