using GameGuild.API.Database;
using GameGuild.API.Eventing;
using GameGuild.API.IntegrationTests.Infrastructure;
using GameGuild.Commerce;
using GameGuild.Commerce.Payments;
using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Tenants;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace GameGuild.API.IntegrationTests;

[Collection(ApiPostgreSqlCollection.Name)]
public sealed class PaymentMethodRegistryPostgreSqlTests(ApiPostgreSqlFixture fixture)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RegistrationInsertsThenUpdatesOneEncryptedRowAndPreservesItsOwner(bool ambientCommand)
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var marker = Guid.NewGuid().ToString("N");
        var tenant = new Tenant
        {
            Name = "Payment registry " + marker,
            Slug = "payment-registry-" + marker,
            AdminEmail = marker + "@example.test"
        };
        db.Add(tenant);
        await db.SaveChangesAsync();
        var owner = Guid.NewGuid();
        var actor = new PaymentMethodActor(tenant.Id, owner, IsSystemAdmin: false);
        var token = "nonce_registry_" + marker;
        var request = new CreatePaymentMethodRequest(tenant.Id, token, GatewayProvider: "braintree");
        var protector = new DataProtectionGatewayPaymentMethodIdProtector(new EphemeralDataProtectionProvider());
        var stripe = new Mock<IStripeCustomerService>();
        var service = new PaymentMethodRegistryService(
            new PaymentMethodRepository(db, NullLogger<PaymentMethodRepository>.Instance),
            stripe.Object, Mock.Of<ISubscriptionPaymentMethodCoordination>(), db,
            NullLogger<PaymentMethodRegistryService>.Instance, gatewayIdProtector: protector);

        var actorAccessor = new Mock<IActorContextAccessor>();
        actorAccessor.SetupGet(value => value.ActorContext).Returns(ActorContext.Anonymous);
        var behavior = new UseCaseOperationBehavior<RegisterPaymentMethodCommand, Result<PaymentMethodDto>>(
            db, actorAccessor.Object, scope.ServiceProvider.GetRequiredService<IUseCaseOperationContextAccessor>());
        Task<Result<PaymentMethodDto>> Register() => ambientCommand
            ? behavior.Handle(new RegisterPaymentMethodCommand(tenant.Id, token, GatewayProvider: "braintree"),
                () => service.CreateAsync(actor, request), CancellationToken.None)
            : service.CreateAsync(actor, request);

        var created = await Register();
        Assert.True(created.IsSuccess);
        db.ChangeTracker.Clear();
        var replay = await Register();
        Assert.True(replay.IsSuccess);
        Assert.Equal(created.Value.Id, replay.Value.Id);
        var otherOwner = await service.CreateAsync(
            new PaymentMethodActor(tenant.Id, Guid.NewGuid(), IsSystemAdmin: false), request, CancellationToken.None);
        Assert.True(otherOwner.IsFailure);
        Assert.Equal("PaymentMethods.OwnerMismatch", otherOwner.Error.Code);

        db.ChangeTracker.Clear();
        var stored = Assert.Single(await db.Set<PaymentMethod>()
            .AsNoTracking().Where(method => method.TenantId == tenant.Id).ToListAsync());
        Assert.Equal(owner, stored.UserId);
        Assert.Equal("braintree", stored.GatewayProvider);
        Assert.True(stored.IsDefault);
        Assert.True(stored.Version >= 2);
        Assert.NotEqual(token, stored.GatewayPaymentMethodId);
        Assert.Equal(token, protector.Unprotect(stored.GatewayPaymentMethodId));
        Assert.Equal(GatewayPaymentMethodIdHash.Compute("braintree", token), stored.GatewayPaymentMethodIdHash);
        stripe.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RegistrationAndDefaultChangeLeaveCommitAndRollbackToTheCommand()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var marker = Guid.NewGuid().ToString("N");
        var tenant = new Tenant
        {
            Name = "Payment rollback " + marker,
            Slug = "payment-rollback-" + marker,
            AdminEmail = marker + "@example.test"
        };
        db.Add(tenant);
        await db.SaveChangesAsync();
        var actor = new PaymentMethodActor(tenant.Id, Guid.NewGuid(), IsSystemAdmin: false);
        var service = new PaymentMethodRegistryService(
            new PaymentMethodRepository(db, NullLogger<PaymentMethodRepository>.Instance),
            Mock.Of<IStripeCustomerService>(), Mock.Of<ISubscriptionPaymentMethodCoordination>(), db,
            NullLogger<PaymentMethodRegistryService>.Instance);
        var actorAccessor = new Mock<IActorContextAccessor>();
        actorAccessor.SetupGet(value => value.ActorContext).Returns(ActorContext.Anonymous);
        var operationAccessor = scope.ServiceProvider.GetRequiredService<IUseCaseOperationContextAccessor>();
        var register = new UseCaseOperationBehavior<RegisterPaymentMethodCommand, Result<PaymentMethodDto>>(
            db, actorAccessor.Object, operationAccessor);
        var token = "nonce_rollback_" + marker;
        await Assert.ThrowsAsync<DbUpdateException>(() => register.Handle(
            new RegisterPaymentMethodCommand(tenant.Id, token, GatewayProvider: "braintree"), async () =>
            {
                var result = await service.CreateAsync(actor,
                    new CreatePaymentMethodRequest(tenant.Id, token, GatewayProvider: "braintree"));
                Assert.True(result.IsSuccess);
                Assert.NotNull(db.Database.CurrentTransaction);
                throw new DbUpdateException("synthetic command failure after registration");
            }, CancellationToken.None));
        Assert.False(await db.Set<PaymentMethod>().AnyAsync(value => value.TenantId == tenant.Id));

        var first = await service.CreateAsync(actor,
            new CreatePaymentMethodRequest(tenant.Id, "nonce_first_" + marker, GatewayProvider: "braintree"));
        var second = await service.CreateAsync(actor,
            new CreatePaymentMethodRequest(tenant.Id, "nonce_second_" + marker, GatewayProvider: "braintree"));
        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        var setDefault = new UseCaseOperationBehavior<SetDefaultPaymentMethodCommand, Result<PaymentMethodDto>>(
            db, actorAccessor.Object, operationAccessor);
        await Assert.ThrowsAsync<DbUpdateException>(() => setDefault.Handle(
            new SetDefaultPaymentMethodCommand(second.Value.Id), async () =>
            {
                var result = await service.SetDefaultAsync(actor, second.Value.Id);
                Assert.True(result.IsSuccess);
                Assert.NotNull(db.Database.CurrentTransaction);
                throw new DbUpdateException("synthetic command failure after changing the default");
            }, CancellationToken.None));
        var stored = await db.Set<PaymentMethod>().AsNoTracking()
            .Where(value => value.TenantId == tenant.Id).ToListAsync();
        Assert.Equal(first.Value.Id, Assert.Single(stored, value => value.IsDefault).Id);
        Assert.Equal(2, stored.Count);
    }
}
