using FluentAssertions;
using GameGuild.Commerce.Payments;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GameGuild.Commerce.Payments.UnitTests.Services;

/// <summary>
///     Default-first charge-method resolution with fallback (issue #414): the owner's
///     default leads, then the next usable saved method in registry order; unusable
///     (expired/failed) and removed methods never charge.
/// </summary>
public class PaymentMethodChargeResolverTests : IDisposable
{
    private readonly PaymentsPersistenceTestDbContext _context;
    private readonly PaymentMethodRepository _repository;

    public PaymentMethodChargeResolverTests()
    {
        var options = new DbContextOptionsBuilder<PaymentsPersistenceTestDbContext>()
            .UseInMemoryDatabase($"charge-resolver-{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _context = new PaymentsPersistenceTestDbContext(options);
        _repository = new PaymentMethodRepository(_context, NullLogger<PaymentMethodRepository>.Instance);
    }

    public void Dispose() => _context.Dispose();

    private async Task<PaymentMethod> SeedAsync(
        Guid tenantId,
        Guid userId,
        string gatewayId,
        bool isDefault = false,
        PaymentMethodStatus status = PaymentMethodStatus.Active)
    {
        var paymentMethod = new PaymentMethod(tenantId, userId, "stripe", gatewayId, PaymentMethodType.Card, isDefault);
        paymentMethod.UpdateGatewayMetadata(PaymentMethodType.Card, "visa", "4242", 12, 2031, $"fp_{gatewayId}");
        if (status == PaymentMethodStatus.Expired)
        {
            paymentMethod.MarkExpired();
        }

        paymentMethod.Version = 1;
        _context.Set<PaymentMethod>().Add(paymentMethod);
        await _context.SaveChangesAsync();
        return paymentMethod;
    }

    [Fact]
    public async Task Resolve_DefaultLeads_FallbackFollowsInRegistryOrder()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await SeedAsync(tenantId, userId, "pm_oldest");
        await SeedAsync(tenantId, userId, "pm_default", isDefault: true);
        await SeedAsync(tenantId, userId, "pm_newest");

        var resolver = new PaymentMethodChargeResolver(_repository);
        var candidates = await resolver.ResolveChargeCandidatesAsync(tenantId, userId, recordedGatewayPaymentMethodId: null, CancellationToken.None);

        candidates.Should().HaveCount(3);
        candidates[0].Should().Be("pm_default", "the owner's default always leads the charge candidates");
        candidates.Should().Contain(["pm_newest", "pm_oldest"], "the remaining usable methods follow as fallbacks");
    }

    [Fact]
    public async Task Resolve_UsableRecordedMethodKeepsPriority()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await SeedAsync(tenantId, userId, "pm_explicit");
        await SeedAsync(tenantId, userId, "pm_default", isDefault: true);

        var resolver = new PaymentMethodChargeResolver(_repository);
        var candidates = await resolver.ResolveChargeCandidatesAsync(tenantId, userId, "pm_explicit", CancellationToken.None);

        candidates.Should().HaveCount(2);
        candidates[0].Should().Be("pm_explicit", "an explicit, still-usable recorded method keeps priority over the default");
    }

    [Fact]
    public async Task Resolve_UnusableMethodsAreSkipped()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await SeedAsync(tenantId, userId, "pm_expired_default", isDefault: true, status: PaymentMethodStatus.Expired);
        await SeedAsync(tenantId, userId, "pm_usable");

        var resolver = new PaymentMethodChargeResolver(_repository);
        var candidates = await resolver.ResolveChargeCandidatesAsync(tenantId, userId, recordedGatewayPaymentMethodId: null, CancellationToken.None);

        candidates.Should().Equal("pm_usable");
    }

    [Fact]
    public async Task Resolve_DeletedMethodsNeverCharge()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var deleted = await SeedAsync(tenantId, userId, "pm_removed", isDefault: true);
        deleted.SoftDelete();
        await _repository.UpdateAsync(deleted, CancellationToken.None);

        var resolver = new PaymentMethodChargeResolver(_repository);
        var candidates = await resolver.ResolveChargeCandidatesAsync(tenantId, userId, "pm_removed", CancellationToken.None);

        candidates.Should().BeEmpty();
    }

    [Fact]
    public async Task Resolve_IsScopedToOwner()
    {
        var tenantId = Guid.NewGuid();
        var owner = Guid.NewGuid();
        await SeedAsync(tenantId, Guid.NewGuid(), "pm_someone_else", isDefault: true);

        var resolver = new PaymentMethodChargeResolver(_repository);
        var candidates = await resolver.ResolveChargeCandidatesAsync(tenantId, owner, recordedGatewayPaymentMethodId: null, CancellationToken.None);

        candidates.Should().BeEmpty("other owners' methods are never charge candidates");
    }

    [Fact]
    public async Task Resolve_WithProtector_PlaintextTokensAreReturned()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var protector = new DataProtectionGatewayPaymentMethodIdProtector(new EphemeralDataProtectionProvider());
        var stored = new PaymentMethod(tenantId, userId, "stripe", protector.Protect("pm_hidden"), PaymentMethodType.Card, isDefault: true);
        stored.StoreProtectedGatewayReference(stored.GatewayPaymentMethodId, GatewayPaymentMethodIdHash.Compute("stripe", "pm_hidden"));
        stored.UpdateGatewayMetadata(PaymentMethodType.Card, "visa", "4242", 12, 2031, "fp_hidden");
        stored.Version = 1;
        _context.Set<PaymentMethod>().Add(stored);
        await _context.SaveChangesAsync(CancellationToken.None);

        var resolver = new PaymentMethodChargeResolver(_repository, protector);
        var candidates = await resolver.ResolveChargeCandidatesAsync(tenantId, userId, recordedGatewayPaymentMethodId: null, CancellationToken.None);

        candidates.Should().Equal("pm_hidden");
    }
}
