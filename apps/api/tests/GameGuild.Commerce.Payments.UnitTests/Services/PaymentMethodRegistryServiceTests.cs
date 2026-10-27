using FluentAssertions;
using GameGuild.Commerce.Payments;
using GameGuild.Commerce;
using GameGuild.CQRS;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Payments.UnitTests.Services;

/// <summary>
///     Payment-method registry service tests (issue #414): CRUD scoping, single-default
/// invariant with subscription re-pointing, fail-closed delete guard and checkout upsert.
/// </summary>
public class PaymentMethodRegistryServiceTests : IDisposable
{
    private readonly PaymentsPersistenceTestDbContext _context;
    private readonly PaymentMethodRepository _repository;
    private readonly Mock<IStripeCustomerService> _stripeCustomerService = new();
    private readonly Mock<ISubscriptionPaymentMethodCoordination> _coordination = new();
    private readonly Mock<IRevenueAuditService> _revenueAudit = new();
    private readonly Mock<IPublisher> _publisher = new();
    private readonly PaymentMethodRegistryService _service;

    public PaymentMethodRegistryServiceTests()
    {
        var options = new DbContextOptionsBuilder<PaymentsPersistenceTestDbContext>()
            .UseInMemoryDatabase($"payment-methods-{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _context = new PaymentsPersistenceTestDbContext(options);
        _repository = new PaymentMethodRepository(_context, NullLogger<PaymentMethodRepository>.Instance);
        _service = new PaymentMethodRegistryService(
            _repository,
            _stripeCustomerService.Object,
            _coordination.Object,
            _context,
            NullLogger<PaymentMethodRegistryService>.Instance,
            gatewayIdProtector: null,
            revenueAuditService: _revenueAudit.Object,
            publisher: _publisher.Object);
    }

    public void Dispose() => _context.Dispose();

    private static PaymentMethodActor OwnerActor(Guid tenantId, Guid userId) => new(tenantId, userId, IsSystemAdmin: false);

    private static PaymentMethodActor AdminActor() => new(TenantId: null, UserId: Guid.Empty, IsSystemAdmin: true);

    private void SetupGatewayDetails(string gatewayId, int? expMonth = 12, int? expYear = 2031)
    {
        _stripeCustomerService
            .Setup(service => service.GetPaymentMethodAsync(gatewayId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GatewayPaymentMethodDetailsResult(
                Success: true,
                PaymentMethodId: gatewayId,
                Details: new GatewayPaymentMethodDetails("card", "visa", "4242", expMonth, expYear, $"fp_{gatewayId}", null, null, null),
                ErrorCode: null,
                ErrorMessage: null));
    }

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

    // ── Create ───────────────────────────────────────────────────────────

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Create_CannotClaimAnotherOwnersLiveOrDeletedToken(bool deleted)
    {
        var tenantId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var stored = await SeedAsync(tenantId, ownerId, "pm_owned_fixture");
        if (deleted)
        {
            stored.SoftDelete();
            await _context.SaveChangesAsync();
        }
        SetupGatewayDetails("pm_owned_fixture");

        var result = await _service.CreateAsync(
            OwnerActor(tenantId, Guid.NewGuid()),
            new CreatePaymentMethodRequest(tenantId, "pm_owned_fixture"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PaymentMethods.OwnerMismatch");
        _context.ChangeTracker.Clear();
        var persisted = await _context.Set<PaymentMethod>().SingleAsync();
        persisted.UserId.Should().Be(ownerId);
        persisted.IsDeleted.Should().Be(deleted);
        _coordination.VerifyNoOtherCalls();
        _publisher.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Create_ShouldRejectNonTokenizedPaymentMethodIds()
    {
        var actor = OwnerActor(Guid.NewGuid(), Guid.NewGuid());

        var result = await _service.CreateAsync(actor, new CreatePaymentMethodRequest(actor.TenantId!.Value, "4242424242424242"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("PaymentMethods.InvalidToken");
        _stripeCustomerService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Create_ShouldFailClosed_WhenGatewayLookupFails()
    {
        var actor = OwnerActor(Guid.NewGuid(), Guid.NewGuid());
        _stripeCustomerService
            .Setup(service => service.GetPaymentMethodAsync("pm_unknown", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GatewayPaymentMethodDetailsResult(false, null, null, "resource_missing", "no such pm"));

        var result = await _service.CreateAsync(actor, new CreatePaymentMethodRequest(actor.TenantId!.Value, "pm_unknown"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("PaymentMethods.GatewayLookupFailed");
    }

    [Fact]
    public async Task Create_ShouldDenyCrossTenantRegistration()
    {
        var actor = OwnerActor(Guid.NewGuid(), Guid.NewGuid());

        var result = await _service.CreateAsync(actor, new CreatePaymentMethodRequest(Guid.NewGuid(), "pm_other_tenant"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("PaymentMethods.CrossTenant");
    }

    [Fact]
    public async Task Create_FirstMethodForOwner_ShouldAutoBecomeDefault()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        SetupGatewayDetails("pm_auto_default");

        var result = await _service.CreateAsync(actor: OwnerActor(tenantId, userId), new CreatePaymentMethodRequest(tenantId, "pm_auto_default"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsDefault.Should().BeTrue();
        result.Value.CardLast4.Should().Be("4242");
        result.Value.CardBrand.Should().Be("visa");
        result.Value.ExpiresAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Create_WithExistingDefault_ShouldDemotePreviousDefaultAndRepointSubscriptions()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var previous = await SeedAsync(tenantId, userId, "pm_old_default", isDefault: true);
        SetupGatewayDetails("pm_new_default");
        _coordination
            .Setup(c => c.RepointSubscriptionsAsync(tenantId, previous.Id, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var result = await _service.CreateAsync(
            OwnerActor(tenantId, userId),
            new CreatePaymentMethodRequest(tenantId, "pm_new_default", SetAsDefault: true),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsDefault.Should().BeTrue();

        var demoted = await _repository.GetByIdAsync(previous.Id, CancellationToken.None);
        demoted!.IsDefault.Should().BeFalse();

        _coordination.Verify(
            c => c.RepointSubscriptionsAsync(tenantId, previous.Id, result.Value.Id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Create_SameGatewayTokenTwice_ShouldUpsertWithoutDuplicating()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        SetupGatewayDetails("pm_duplicate");

        var first = await _service.CreateAsync(OwnerActor(tenantId, userId), new CreatePaymentMethodRequest(tenantId, "pm_duplicate", Nickname: "First"), CancellationToken.None);
        var second = await _service.CreateAsync(OwnerActor(tenantId, userId), new CreatePaymentMethodRequest(tenantId, "pm_duplicate", Nickname: "Second"), CancellationToken.None);

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        second.Value.Id.Should().Be(first.Value.Id);

        var all = await _repository.ListAsync(tenantId, null, CancellationToken.None);
        all.Should().ContainSingle();
    }

    [Fact]
    public async Task Create_ReRegisteringSoftDeletedToken_ShouldRestoreRow()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var seeded = await SeedAsync(tenantId, userId, "pm_removed");
        seeded.SoftDelete();
        await _repository.UpdateAsync(seeded, CancellationToken.None);
        SetupGatewayDetails("pm_removed");

        var result = await _service.CreateAsync(OwnerActor(tenantId, userId), new CreatePaymentMethodRequest(tenantId, "pm_removed"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(seeded.Id);
        (await _repository.GetByIdAsync(seeded.Id, CancellationToken.None)).Should().NotBeNull();
    }

    [Fact]
    public async Task Create_BySystemAdmin_ShouldRequireOwnerUserId()
    {
        var tenantId = Guid.NewGuid();
        SetupGatewayDetails("pm_admin");

        var missingOwner = await _service.CreateAsync(AdminActor(), new CreatePaymentMethodRequest(tenantId, "pm_admin"), CancellationToken.None);
        missingOwner.IsSuccess.Should().BeFalse();
        missingOwner.Error.Code.Should().Be("PaymentMethods.OwnerRequired");

        var withOwner = await _service.CreateAsync(AdminActor(), new CreatePaymentMethodRequest(tenantId, "pm_admin", OwnerUserId: Guid.NewGuid()), CancellationToken.None);
        withOwner.IsSuccess.Should().BeTrue();
    }

    // ── List / Get scoping ───────────────────────────────────────────────

    [Fact]
    public async Task List_NonAdminSeesOwnMethods_AdminSeesWholeTenant()
    {
        var tenantId = Guid.NewGuid();
        var owner = Guid.NewGuid();
        var otherUser = Guid.NewGuid();
        await SeedAsync(tenantId, owner, "pm_owner_card");
        await SeedAsync(tenantId, otherUser, "pm_other_card");

        var selfList = await _service.ListAsync(OwnerActor(tenantId, owner), tenantId, CancellationToken.None);
        var adminList = await _service.ListAsync(AdminActor(), tenantId, CancellationToken.None);

        selfList.Value.Should().ContainSingle().Which.GatewayPaymentMethodId.Should().Be("pm_owner_card");
        adminList.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task List_ActorWithoutTenantContext_ShouldFailClosed()
    {
        var result = await _service.ListAsync(new PaymentMethodActor(TenantId: null, Guid.NewGuid(), IsSystemAdmin: false), Guid.NewGuid(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("PaymentMethods.NoTenantContext");
    }

    [Fact]
    public async Task Get_ForeignTenantMethod_ShouldReturnNotFound_NotForbidden()
    {
        var tenantId = Guid.NewGuid();
        var foreignTenant = Guid.NewGuid();
        var seeded = await SeedAsync(foreignTenant, Guid.NewGuid(), "pm_foreign");

        var result = await _service.GetByIdAsync(OwnerActor(tenantId, Guid.NewGuid()), seeded.Id, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Get_SameTenantNonOwner_ShouldReturnForbidden()
    {
        var tenantId = Guid.NewGuid();
        var owner = Guid.NewGuid();
        var seeded = await SeedAsync(tenantId, owner, "pm_owner_only");

        var result = await _service.GetByIdAsync(OwnerActor(tenantId, Guid.NewGuid()), seeded.Id, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("PaymentMethods.NotOwner");
        result.Error.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task Get_OwnerAndAdmin_ShouldSucceed()
    {
        var tenantId = Guid.NewGuid();
        var owner = Guid.NewGuid();
        var seeded = await SeedAsync(tenantId, owner, "pm_visible");

        var asOwner = await _service.GetByIdAsync(OwnerActor(tenantId, owner), seeded.Id, CancellationToken.None);
        var asAdmin = await _service.GetByIdAsync(AdminActor(), seeded.Id, CancellationToken.None);

        asOwner.IsSuccess.Should().BeTrue();
        asOwner.Value.Id.Should().Be(seeded.Id);
        asAdmin.IsSuccess.Should().BeTrue();
    }

    // ── Update / SetDefault ─────────────────────────────────────────────

    [Fact]
    public async Task Update_ShouldChangeDisplayFieldsOnly()
    {
        var tenantId = Guid.NewGuid();
        var owner = Guid.NewGuid();
        var seeded = await SeedAsync(tenantId, owner, "pm_display");

        var result = await _service.UpdateAsync(OwnerActor(tenantId, owner), seeded.Id, new UpdatePaymentMethodRequest("Corp Card", "{\"city\":\"Porto\"}"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Nickname.Should().Be("Corp Card");
        result.Value.BillingAddress.Should().Be("{\"city\":\"Porto\"}");
        result.Value.GatewayPaymentMethodId.Should().Be("pm_display");
    }

    [Fact]
    public async Task Update_NonOwner_ShouldReturnForbidden()
    {
        var tenantId = Guid.NewGuid();
        var seeded = await SeedAsync(tenantId, Guid.NewGuid(), "pm_locked");

        var result = await _service.UpdateAsync(OwnerActor(tenantId, Guid.NewGuid()), seeded.Id, new UpdatePaymentMethodRequest("nope"), CancellationToken.None);

        result.Error.Code.Should().Be("PaymentMethods.NotOwner");
    }

    [Fact]
    public async Task SetDefault_ShouldDemotePreviousDefaultAndRepointSubscriptions()
    {
        var tenantId = Guid.NewGuid();
        var owner = Guid.NewGuid();
        var oldDefault = await SeedAsync(tenantId, owner, "pm_default_old", isDefault: true);
        var candidate = await SeedAsync(tenantId, owner, "pm_default_new");
        _coordination
            .Setup(c => c.RepointSubscriptionsAsync(tenantId, oldDefault.Id, candidate.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        var result = await _service.SetDefaultAsync(OwnerActor(tenantId, owner), candidate.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await _repository.GetByIdAsync(oldDefault.Id, CancellationToken.None))!.IsDefault.Should().BeFalse();
        (await _repository.GetByIdAsync(candidate.Id, CancellationToken.None))!.IsDefault.Should().BeTrue();
        _coordination.Verify(c => c.RepointSubscriptionsAsync(tenantId, oldDefault.Id, candidate.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetDefault_AlreadyDefault_ShouldNotRepoint()
    {
        var tenantId = Guid.NewGuid();
        var owner = Guid.NewGuid();
        var seeded = await SeedAsync(tenantId, owner, "pm_stays_default", isDefault: true);

        var result = await _service.SetDefaultAsync(OwnerActor(tenantId, owner), seeded.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _coordination.Verify(
            c => c.RepointSubscriptionsAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SetDefault_ExpiredMethod_ShouldFailClosed()
    {
        var tenantId = Guid.NewGuid();
        var owner = Guid.NewGuid();
        var expired = await SeedAsync(tenantId, owner, "pm_expired", status: PaymentMethodStatus.Expired);

        var result = await _service.SetDefaultAsync(OwnerActor(tenantId, owner), expired.Id, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("PaymentMethods.NotUsable");
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }

    // ── Delete ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_WithBillableSubscriptions_ShouldFailClosedWithConflict()
    {
        var tenantId = Guid.NewGuid();
        var owner = Guid.NewGuid();
        var seeded = await SeedAsync(tenantId, owner, "pm_in_use");
        _coordination
            .Setup(c => c.CountBillableSubscriptionsOnPaymentMethodAsync(tenantId, seeded.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var result = await _service.DeleteAsync(OwnerActor(tenantId, owner), seeded.Id, confirm: true, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("PaymentMethods.InUseBySubscriptions");
        result.Error.Type.Should().Be(ErrorType.Conflict);
        result.Error.Description.Should().Contain("2 active subscription(s)");

        var stillThere = await _repository.GetByIdAsync(seeded.Id, CancellationToken.None);
        stillThere.Should().NotBeNull();
        stillThere!.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_WithoutSubscriptions_ShouldSoftDelete()
    {
        var tenantId = Guid.NewGuid();
        var owner = Guid.NewGuid();
        var seeded = await SeedAsync(tenantId, owner, "pm_removable");
        _coordination
            .Setup(c => c.CountBillableSubscriptionsOnPaymentMethodAsync(tenantId, seeded.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var result = await _service.DeleteAsync(OwnerActor(tenantId, owner), seeded.Id, confirm: true, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await _repository.GetByIdAsync(seeded.Id, CancellationToken.None)).Should().BeNull();
        var deletedRow = await _context.Set<PaymentMethod>().SingleAsync(pm => pm.Id == seeded.Id);
        deletedRow.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_NonOwner_ShouldReturnForbidden()
    {
        var tenantId = Guid.NewGuid();
        var seeded = await SeedAsync(tenantId, Guid.NewGuid(), "pm_owner_delete");

        var result = await _service.DeleteAsync(OwnerActor(tenantId, Guid.NewGuid()), seeded.Id, confirm: true, CancellationToken.None);

        result.Error.Code.Should().Be("PaymentMethods.NotOwner");
    }

    // ── Checkout upsert ─────────────────────────────────────────────────

    [Fact]
    public async Task UpsertFromCheckout_ShouldAssignSubscriptionAndMarkUsed()
    {
        var tenantId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();
        var billingUser = Guid.NewGuid();
        SetupGatewayDetails("pm_checkout");
        _coordination
            .Setup(c => c.GetSubscriptionBillingUserIdAsync(subscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(billingUser);

        // Service actor without a user subject (UserId empty) resolves the billing owner.
        var actor = new PaymentMethodActor(tenantId, Guid.Empty, IsSystemAdmin: false);
        var result = await _service.UpsertFromCheckoutAsync(actor, tenantId, subscriptionId, "pm_checkout", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be(billingUser);
        result.Value.IsDefault.Should().BeTrue();
        result.Value.LastUsedAt.Should().NotBeNull();

        _coordination.Verify(
            c => c.AssignSubscriptionPaymentMethodAsync(subscriptionId, result.Value.Id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpsertFromCheckout_ShouldRejectMalformedTokens()
    {
        var actor = new PaymentMethodActor(Guid.NewGuid(), Guid.NewGuid(), IsSystemAdmin: false);

        var result = await _service.UpsertFromCheckoutAsync(actor, actor.TenantId!.Value, Guid.NewGuid(), "not_a_pm", CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("PaymentMethods.InvalidToken");
    }

    [Fact]
    public async Task UpsertFromCheckout_WhenGatewayDetailsUnavailable_ShouldStillRegisterTheReference()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();
        _stripeCustomerService
            .Setup(service => service.GetPaymentMethodAsync("pm_gateway_down", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GatewayPaymentMethodDetailsResult(false, null, null, "api_error", "gateway down"));

        var result = await _service.UpsertFromCheckoutAsync(
            new PaymentMethodActor(tenantId, userId, IsSystemAdmin: false),
            tenantId, subscriptionId, "pm_gateway_down", CancellationToken.None);

        result.IsSuccess.Should().BeTrue("checkout already succeeded at the gateway; the local mirror must not fail the flow");
        result.Value.GatewayPaymentMethodId.Should().Be("pm_gateway_down");
        result.Value.CardLast4.Should().BeNull();
    }

    // ── Duplicate-card detection (issue #414) ───────────────────────────

    [Fact]
    public async Task Create_DuplicateCardFingerprintForSameOwner_ShouldBeRejectedWithConflict()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await SeedAsync(tenantId, userId, "pm_original_card"); // fingerprint fp_pm_original_card
        _stripeCustomerService
            .Setup(service => service.GetPaymentMethodAsync("pm_same_card", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GatewayPaymentMethodDetailsResult(
                Success: true,
                PaymentMethodId: "pm_same_card",
                Details: new GatewayPaymentMethodDetails("card", "visa", "4242", 12, 2031, "fp_pm_original_card", null, null, null),
                ErrorCode: null,
                ErrorMessage: null));

        var result = await _service.CreateAsync(
            OwnerActor(tenantId, userId),
            new CreatePaymentMethodRequest(tenantId, "pm_same_card"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("PaymentMethods.DuplicateCard");
        result.Error.Type.Should().Be(ErrorType.Conflict);

        var all = await _repository.ListAsync(tenantId, null, CancellationToken.None);
        all.Should().ContainSingle("the duplicate card must not create a second registry row");
    }

    [Fact]
    public async Task Create_SameFingerprintForDifferentOwner_ShouldBeAllowed()
    {
        var tenantId = Guid.NewGuid();
        await SeedAsync(tenantId, Guid.NewGuid(), "pm_corporate_card");
        _stripeCustomerService
            .Setup(service => service.GetPaymentMethodAsync("pm_shared_card", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GatewayPaymentMethodDetailsResult(
                Success: true,
                PaymentMethodId: "pm_shared_card",
                Details: new GatewayPaymentMethodDetails("card", "visa", "4242", 12, 2031, "fp_pm_corporate_card", null, null, null),
                ErrorCode: null,
                ErrorMessage: null));

        var result = await _service.CreateAsync(
            OwnerActor(tenantId, Guid.NewGuid()),
            new CreatePaymentMethodRequest(tenantId, "pm_shared_card"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue("fingerprint dedup is scoped per owner; different users may share a card");
    }

    [Fact]
    public async Task Create_SameFingerprintAcrossTenants_ShouldBeAllowed()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        await SeedAsync(tenantA, Guid.NewGuid(), "pm_tenant_a_card");
        _stripeCustomerService
            .Setup(service => service.GetPaymentMethodAsync("pm_tenant_b_card", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GatewayPaymentMethodDetailsResult(
                Success: true,
                PaymentMethodId: "pm_tenant_b_card",
                Details: new GatewayPaymentMethodDetails("card", "visa", "4242", 12, 2031, "fp_pm_tenant_a_card", null, null, null),
                ErrorCode: null,
                ErrorMessage: null));

        var result = await _service.CreateAsync(
            OwnerActor(tenantB, Guid.NewGuid()),
            new CreatePaymentMethodRequest(tenantB, "pm_tenant_b_card"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue("fingerprint dedup is scoped per tenant and owner");
    }

    [Fact]
    public async Task Create_ReRegisteringRemovedToken_WhenAnotherLiveMethodHoldsTheFingerprint_ShouldBeRejected()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var removed = await SeedAsync(tenantId, userId, "pm_removed_card");
        removed.SoftDelete();
        await _repository.UpdateAsync(removed, CancellationToken.None);
        // Same owner later registers the same physical card through a fresh token that
        // shares the fingerprint with a different live method.
        await SeedAsync(tenantId, userId, "pm_reissued_card");
        _stripeCustomerService
            .Setup(service => service.GetPaymentMethodAsync("pm_removed_card", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GatewayPaymentMethodDetailsResult(
                Success: true,
                PaymentMethodId: "pm_removed_card",
                Details: new GatewayPaymentMethodDetails("card", "visa", "4242", 12, 2031, "fp_pm_reissued_card", null, null, null),
                ErrorCode: null,
                ErrorMessage: null));

        var result = await _service.CreateAsync(
            OwnerActor(tenantId, userId),
            new CreatePaymentMethodRequest(tenantId, "pm_removed_card"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("PaymentMethods.DuplicateCard");
    }

    [Fact]
    public async Task Create_MethodWithoutFingerprint_ShouldBypassDedup()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await SeedAsync(tenantId, userId, "pm_with_fp");
        _stripeCustomerService
            .Setup(service => service.GetPaymentMethodAsync("pm_no_fp", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GatewayPaymentMethodDetailsResult(
                Success: true,
                PaymentMethodId: "pm_no_fp",
                Details: new GatewayPaymentMethodDetails("us_bank_account", null, null, null, null, null, "First Bank", "0002", null),
                ErrorCode: null,
                ErrorMessage: null));

        var result = await _service.CreateAsync(
            OwnerActor(tenantId, userId),
            new CreatePaymentMethodRequest(tenantId, "pm_no_fp"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue("methods without a gateway fingerprint cannot be deduplicated");
    }

    // ── Delete confirmation (issue #414) ────────────────────────────────

    [Fact]
    public async Task Delete_WithoutConfirmFlag_ShouldFailClosedWithValidationError()
    {
        var tenantId = Guid.NewGuid();
        var owner = Guid.NewGuid();
        var seeded = await SeedAsync(tenantId, owner, "pm_needs_confirm");

        var result = await _service.DeleteAsync(OwnerActor(tenantId, owner), seeded.Id, confirm: false, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("PaymentMethods.DeleteConfirmationRequired");
        result.Error.Type.Should().Be(ErrorType.Validation);

        var stillThere = await _repository.GetByIdAsync(seeded.Id, CancellationToken.None);
        stillThere.Should().NotBeNull("the unconfirmed delete must not touch the row");
    }

    // ── Audit trail + confirmation events (issue #414) ──────────────────

    [Fact]
    public async Task Create_ShouldRecordAuditTrailRow()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        SetupGatewayDetails("pm_audited");

        var result = await _service.CreateAsync(
            OwnerActor(tenantId, userId),
            new CreatePaymentMethodRequest(tenantId, "pm_audited"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _revenueAudit.Verify(
            audit => audit.RecordAuditTrailAsync(
                nameof(PaymentMethod),
                result.Value.Id,
                "Created",
                userId,
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Delete_ShouldRecordAuditTrailRowAndPublishDeletedEvent()
    {
        var tenantId = Guid.NewGuid();
        var owner = Guid.NewGuid();
        var seeded = await SeedAsync(tenantId, owner, "pm_audit_delete");
        _coordination
            .Setup(c => c.CountBillableSubscriptionsOnPaymentMethodAsync(tenantId, seeded.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var result = await _service.DeleteAsync(OwnerActor(tenantId, owner), seeded.Id, confirm: true, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _revenueAudit.Verify(
            audit => audit.RecordAuditTrailAsync(
                nameof(PaymentMethod),
                seeded.Id,
                "Deleted",
                owner,
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _publisher.Verify(
            publisher => publisher.Publish(
                It.Is<PaymentMethodDeletedEvent>(domainEvent =>
                    domainEvent.PaymentMethodId == seeded.Id
                    && domainEvent.UserId == owner
                    && domainEvent.CardLast4 == "4242"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SetDefault_ShouldRecordAuditTrailRow()
    {
        var tenantId = Guid.NewGuid();
        var owner = Guid.NewGuid();
        var candidate = await SeedAsync(tenantId, owner, "pm_audit_default");

        var result = await _service.SetDefaultAsync(OwnerActor(tenantId, owner), candidate.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _revenueAudit.Verify(
            audit => audit.RecordAuditTrailAsync(
                nameof(PaymentMethod),
                candidate.Id,
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Create_ShouldPublishRegisteredEventWithDisplayDetails()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        SetupGatewayDetails("pm_confirm_email");

        var result = await _service.CreateAsync(
            OwnerActor(tenantId, userId),
            new CreatePaymentMethodRequest(tenantId, "pm_confirm_email"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _publisher.Verify(
            publisher => publisher.Publish(
                It.Is<PaymentMethodRegisteredEvent>(domainEvent =>
                    domainEvent.PaymentMethodId == result.Value.Id
                    && domainEvent.UserId == userId
                    && domainEvent.TenantId == tenantId
                    && domainEvent.GatewayPaymentMethodId == "pm_confirm_email"
                    && domainEvent.CardBrand == "visa"
                    && domainEvent.CardLast4 == "4242"
                    && !domainEvent.Restored),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Create_PublishFailure_ShouldNotFailTheRegistration()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        SetupGatewayDetails("pm_publish_blows");
        _publisher
            .Setup(publisher => publisher.Publish(It.IsAny<PaymentMethodRegisteredEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("notification pipeline down"));

        var result = await _service.CreateAsync(
            OwnerActor(tenantId, userId),
            new CreatePaymentMethodRequest(tenantId, "pm_publish_blows"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue("confirmation emails are best-effort; the registry mutation stays committed");
    }

    // ── Encrypted-at-rest gateway references (issue #414) ───────────────

    [Theory]
    [InlineData("paypal", "ba_registry_fixture")]
    [InlineData("authorizenet", "COMMON.ACCEPT.INAPP.PAYMENT|opaque_fixture")]
    [InlineData("braintree", "nonce_registry_fixture")]
    [InlineData("adyen", "storedRegistryFixture")]
    public async Task Create_WithProtector_ShouldScopeEncryptedUpsertsToTheResolvedGateway(string provider, string token)
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await using var context = NewContext();
        var repository = new PaymentMethodRepository(context, NullLogger<PaymentMethodRepository>.Instance);
        var protector = new DataProtectionGatewayPaymentMethodIdProtector(new EphemeralDataProtectionProvider());
        var service = new PaymentMethodRegistryService(
            repository, _stripeCustomerService.Object, _coordination.Object, context,
            NullLogger<PaymentMethodRegistryService>.Instance, gatewayIdProtector: protector);
        var actor = OwnerActor(tenantId, userId);
        var request = new CreatePaymentMethodRequest(tenantId, token, GatewayProvider: provider);

        var first = await service.CreateAsync(actor, request, CancellationToken.None);
        first.IsSuccess.Should().BeTrue();
        var second = await service.CreateAsync(actor, request, CancellationToken.None);
        second.IsSuccess.Should().BeTrue();
        second.Value.Id.Should().Be(first.Value.Id);

        var stored = await context.Set<PaymentMethod>().SingleAsync();
        stored.GatewayProvider.Should().Be(provider);
        protector.IsProtected(stored.GatewayPaymentMethodId).Should().BeTrue();
        stored.GatewayPaymentMethodId.Should().NotBe(token);
        stored.GatewayPaymentMethodIdHash.Should().Be(GatewayPaymentMethodIdHash.Compute(provider, token));
        var resolved = await repository.GetByGatewayIdAsync(tenantId, provider, token, CancellationToken.None);
        resolved.Should().NotBeNull();
        resolved!.Id.Should().Be(stored.Id);
        _stripeCustomerService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Create_WithProtector_ShouldStoreProtectedReferenceAndHash()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var context = NewContext();
        var repository = new PaymentMethodRepository(context, NullLogger<PaymentMethodRepository>.Instance);
        var protector = new DataProtectionGatewayPaymentMethodIdProtector(new EphemeralDataProtectionProvider());
        var service = new PaymentMethodRegistryService(
            repository,
            _stripeCustomerService.Object,
            _coordination.Object,
            context,
            NullLogger<PaymentMethodRegistryService>.Instance,
            gatewayIdProtector: protector);
        SetupGatewayDetails("pm_secret_storage");

        var result = await service.CreateAsync(
            OwnerActor(tenantId, userId),
            new CreatePaymentMethodRequest(tenantId, "pm_secret_storage"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.GatewayPaymentMethodId.Should().Be("pm_secret_storage", "the DTO always exposes the plaintext token");

        var stored = await context.Set<PaymentMethod>().SingleAsync(pm => pm.Id == result.Value.Id, CancellationToken.None);
        stored.GatewayPaymentMethodId.Should().NotBe("pm_secret_storage", "the stored reference must not be plaintext");
        protector.IsProtected(stored.GatewayPaymentMethodId).Should().BeTrue();
        stored.GatewayPaymentMethodIdHash.Should().Be(GatewayPaymentMethodIdHash.Compute("stripe", "pm_secret_storage"));

        // The registry resolves the row by its plaintext token through the hash.
        var resolved = await repository.GetByGatewayIdAsync(tenantId, "stripe", "pm_secret_storage", CancellationToken.None);
        resolved.Should().NotBeNull();
        resolved!.Id.Should().Be(result.Value.Id);
    }

    [Fact]
    public async Task Create_WithProtector_LegacyPlaintextRowIsMigratedOnReRegister()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var context = NewContext();
        var repository = new PaymentMethodRepository(context, NullLogger<PaymentMethodRepository>.Instance);
        var protector = new DataProtectionGatewayPaymentMethodIdProtector(new EphemeralDataProtectionProvider());
        var service = new PaymentMethodRegistryService(
            repository,
            _stripeCustomerService.Object,
            _coordination.Object,
            context,
            NullLogger<PaymentMethodRegistryService>.Instance,
            gatewayIdProtector: protector);

        var legacy = new PaymentMethod(tenantId, userId, "stripe", "pm_legacy_plain", PaymentMethodType.Card, isDefault: true);
        legacy.UpdateGatewayMetadata(PaymentMethodType.Card, "visa", "1881", 11, 2030, "fp_legacy");
        context.Set<PaymentMethod>().Add(legacy);
        await context.SaveChangesAsync(CancellationToken.None);
        SetupGatewayDetails("pm_legacy_plain");

        var result = await service.CreateAsync(
            OwnerActor(tenantId, userId),
            new CreatePaymentMethodRequest(tenantId, "pm_legacy_plain"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var stored = await context.Set<PaymentMethod>().SingleAsync(pm => pm.Id == legacy.Id, CancellationToken.None);
        protector.IsProtected(stored.GatewayPaymentMethodId).Should().BeTrue("re-registering migrates the legacy plaintext reference");
        stored.GatewayPaymentMethodIdHash.Should().NotBeNull();
        result.Value.GatewayPaymentMethodId.Should().Be("pm_legacy_plain");
    }

    [Fact]
    public void GatewayPaymentMethodIdHash_ShouldBeDeterministicAndProviderScoped()
    {
        var first = GatewayPaymentMethodIdHash.Compute("Stripe", "pm_x");
        var second = GatewayPaymentMethodIdHash.Compute("stripe", "pm_x");
        var otherProvider = GatewayPaymentMethodIdHash.Compute("paypal", "pm_x");
        var otherToken = GatewayPaymentMethodIdHash.Compute("stripe", "pm_y");

        first.Should().Be(second, "the provider is normalized before hashing");
        first.Should().NotBe(otherProvider);
        first.Should().NotBe(otherToken);
        first.Length.Should().BeLessThanOrEqualTo(64);
    }

    [Fact]
    public void DataProtectionProtector_RoundTripsAndMarksProtectedValues()
    {
        var protector = new DataProtectionGatewayPaymentMethodIdProtector(new EphemeralDataProtectionProvider());
        var protectedValue = protector.Protect("pm_roundtrip");

        protectedValue.Should().NotBe("pm_roundtrip");
        protector.IsProtected(protectedValue).Should().BeTrue();
        protector.IsProtected("pm_legacy_plaintext").Should().BeFalse();
        protector.Unprotect(protectedValue).Should().Be("pm_roundtrip");
        protector.Unprotect("pm_legacy_plaintext").Should().Be("pm_legacy_plaintext", "legacy plaintext values pass through unchanged");
    }

    private PaymentsPersistenceTestDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<PaymentsPersistenceTestDbContext>()
            .UseInMemoryDatabase($"payment-methods-{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new PaymentsPersistenceTestDbContext(options);
    }
}
