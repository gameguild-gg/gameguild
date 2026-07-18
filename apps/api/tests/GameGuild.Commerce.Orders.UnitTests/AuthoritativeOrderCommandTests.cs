using FluentAssertions;
using GameGuild.Commerce.Products;
using GameGuild.Identity.Context.Actors;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Orders.UnitTests;

public sealed class AuthoritativeOrderCommandTests
{
    [Fact]
    public async Task CreateOrder_DerivesUserAndTenantFromActorContext()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var repository = new Mock<IOrderRepository>();
        Order? addedOrder = null;
        repository.Setup(mock => mock.GetByIdempotencyKeyAsync("actor-owned-key", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);
        repository.Setup(mock => mock.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Callback<Order, CancellationToken>((order, _) => addedOrder = order);

        var handler = new CreateOrderCommandHandler(repository.Object, CreateActor(userId, tenantId));

        var result = await handler.Handle(
            new CreateOrderCommand("actor-owned-key", "127.0.0.1", "unit-test"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        addedOrder.Should().NotBeNull();
        addedOrder!.UserId.Should().Be(userId);
        addedOrder.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void CreateOrderRequest_DoesNotAcceptIdentityOrCurrencyFromTheBody()
    {
        typeof(CreateOrderRequest).GetProperty("UserId").Should().BeNull();
        typeof(CreateOrderRequest).GetProperty("TenantId").Should().BeNull();
        typeof(CreateOrderRequest).GetProperty("Currency").Should().BeNull();
    }

    [Fact]
    public async Task CreateOrder_RejectsAuthenticatedNonUserActor()
    {
        var repository = new Mock<IOrderRepository>();
        var handler = new CreateOrderCommandHandler(
            repository.Object,
            CreateActor(Guid.NewGuid(), Guid.NewGuid(), ActorKind.Service));

        var result = await handler.Handle(new CreateOrderCommand("service-actor-key"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Orders.UserActorRequired");
        repository.Verify(mock => mock.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateOrder_RejectsCrossUserMutation()
    {
        var tenantId = Guid.NewGuid();
        var order = Order.Create(Guid.NewGuid(), "cross-user-key", tenantId);
        var repository = RepositoryWith(order);
        var handler = new UpdateOrderCommandHandler(
            repository.Object,
            CreateActor(Guid.NewGuid(), tenantId));

        var result = await handler.Handle(new UpdateOrderCommand(order.Id, Notes: "spoofed"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Orders.Forbidden");
        repository.Verify(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateOrder_RejectsCrossTenantMutation()
    {
        var userId = Guid.NewGuid();
        var order = Order.Create(userId, "cross-tenant-key", Guid.NewGuid());
        var repository = RepositoryWith(order);
        var handler = new UpdateOrderCommandHandler(
            repository.Object,
            CreateActor(userId, Guid.NewGuid()));

        var result = await handler.Handle(new UpdateOrderCommand(order.Id, Notes: "spoofed"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Orders.Forbidden");
        repository.Verify(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddProduct_RejectsUnpublishedProduct()
    {
        var fixture = CreatePricingFixture(isPublished: false);

        var result = await fixture.Handler.Handle(fixture.Command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Products.Unavailable");
        fixture.Order.LineItems.Should().BeEmpty();
    }

    [Fact]
    public async Task AddProduct_RejectsCrossTenantProductAndPricing()
    {
        var fixture = CreatePricingFixture(productTenantId: Guid.NewGuid());

        var result = await fixture.Handler.Handle(fixture.Command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Orders.PricingTenantMismatch");
        fixture.Order.LineItems.Should().BeEmpty();
    }

    [Fact]
    public async Task AddProduct_RejectsStalePricingVersion()
    {
        var fixture = CreatePricingFixture(staleVersion: true);

        var result = await fixture.Handler.Handle(fixture.Command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Orders.StalePricing");
        fixture.Order.LineItems.Should().BeEmpty();
    }

    [Fact]
    public async Task AddProduct_RejectsZeroPrice()
    {
        var fixture = CreatePricingFixture(basePrice: 0m);

        var result = await fixture.Handler.Handle(fixture.Command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Orders.InvalidPrice");
        fixture.Order.LineItems.Should().BeEmpty();
    }

    [Fact]
    public async Task AddProduct_RejectsMixedCurrencies()
    {
        var fixture = CreatePricingFixture(currency: "EUR");
        fixture.Order.AddLineItem(
            Guid.NewGuid(),
            "Existing item",
            new OrderLineItemPricingSnapshot(Guid.NewGuid(), Guid.NewGuid(), 1, 10m, null, 10m, "USD"));

        var result = await fixture.Handler.Handle(fixture.Command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Orders.MixedCurrency");
        fixture.Order.LineItems.Should().ContainSingle();
    }

    [Fact]
    public async Task AddProduct_RejectsFixedDiscountInAnotherCurrency()
    {
        var fixture = CreatePricingFixture(currency: "BRL", fixedPromoCurrency: "USD");

        var result = await fixture.Handler.Handle(fixture.Command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Orders.MixedCurrency");
        fixture.Order.LineItems.Should().BeEmpty();
    }

    [Fact]
    public async Task AddProduct_SnapshotsPriceCurrencyVersionAndComputesTotalsServerSide()
    {
        var fixture = CreatePricingFixture(basePrice: 100m, salePrice: 80m, currency: "BRL", quantity: 2);

        var result = await fixture.Handler.Handle(fixture.Command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var lineItem = fixture.Order.LineItems.Should().ContainSingle().Subject;
        lineItem.ProductPricingId.Should().Be(fixture.Pricing.Id);
        lineItem.ProductPricingVersionId.Should().Be(fixture.Version.Id);
        lineItem.PriceVersionSnapshot.Should().Be(fixture.Version.PriceVersion);
        lineItem.BasePriceSnapshot.Should().Be(100m);
        lineItem.SalePriceSnapshot.Should().Be(80m);
        lineItem.UnitPriceSnapshot.Should().Be(80m);
        lineItem.CurrencySnapshot.Should().Be("BRL");
        lineItem.LineTotal.Should().Be(160m);
        fixture.Order.Currency.Should().Be("BRL");
        fixture.Order.Subtotal.Should().Be(160m);
        fixture.Order.Total.Should().Be(160m);

        fixture.Pricing.Versions.Add(fixture.Version);
        fixture.Pricing.UpdatePrices(250m, 200m, "post-checkout mutation");

        lineItem.BasePriceSnapshot.Should().Be(100m);
        lineItem.SalePriceSnapshot.Should().Be(80m);
        lineItem.UnitPriceSnapshot.Should().Be(80m);
        lineItem.CurrencySnapshot.Should().Be("BRL");
        fixture.Order.Total.Should().Be(160m);
    }

    [Fact]
    public void OrderLineItem_RejectsReflectionBasedSnapshotMutation()
    {
        var order = OrderTestFactory.CreatePendingOrder();
        var lineItem = OrderTestFactory.AddLineItem(order, Guid.NewGuid(), "Immutable product", 45m);

        var mutate = () => lineItem.SetProperties(new Dictionary<string, object?>
        {
            [nameof(OrderLineItem.UnitPriceSnapshot)] = 1m,
            [nameof(OrderLineItem.CurrencySnapshot)] = "EUR",
            [nameof(OrderLineItem.PriceVersionSnapshot)] = 99
        });

        mutate.Should().Throw<InvalidOperationException>();
        lineItem.UnitPriceSnapshot.Should().Be(45m);
        lineItem.CurrencySnapshot.Should().Be("USD");
        lineItem.PriceVersionSnapshot.Should().Be(1);
    }

    [Fact]
    public void OrderLineItem_DoesNotExposePublicConstructionBypass()
    {
        typeof(OrderLineItem).GetConstructors().Should().BeEmpty();
    }

    [Fact]
    public async Task CaptureOrder_CannotMarkPendingOrderPaid()
    {
        var order = OrderTestFactory.CreatePendingOrder();
        var repository = RepositoryWith(order, withLineItems: true);
        var handler = new CaptureOrderCommandHandler(
            repository.Object,
            CreateActor(order.UserId, order.TenantId!.Value));

        var result = await handler.Handle(new CaptureOrderCommand(order.Id, order.Total), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Orders.PaymentAuthorityRequired");
        order.Status.Should().Be(OrderStatus.Pending);
        order.PaidAt.Should().BeNull();
        repository.Verify(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CaptureOrder_RejectsCrossTenantActor()
    {
        var order = OrderTestFactory.CreatePendingOrder();
        var repository = RepositoryWith(order, withLineItems: true);
        var handler = new CaptureOrderCommandHandler(
            repository.Object,
            CreateActor(order.UserId, Guid.NewGuid()));

        var result = await handler.Handle(new CaptureOrderCommand(order.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Orders.Forbidden");
        order.Status.Should().Be(OrderStatus.Pending);
        repository.Verify(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CompleteOrder_RejectsArbitraryPaymentReferencesOnPendingOrder()
    {
        var order = OrderTestFactory.CreatePendingOrder();
        var repository = RepositoryWith(order, withLineItems: true);
        var entitlementService = new Mock<IEntitlementService>();
        var dbContext = new Mock<IApplicationDbContext>();
        var handler = new CompleteOrderCommandHandler(
            repository.Object,
            entitlementService.Object,
            dbContext.Object,
            Mock.Of<IOrderPaymentAuthority>(),
            CreateActor(order.UserId, order.TenantId!.Value));

        var result = await handler.Handle(
            new CompleteOrderCommand(order.Id, Guid.NewGuid(), "attacker-reference", "card"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Orders.PaymentAuthorityRequired");
        order.Status.Should().Be(OrderStatus.Pending);
        order.PaymentId.Should().BeNull();
        entitlementService.VerifyNoOtherCalls();
        dbContext.Verify(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CompleteOrder_RejectsCrossUserEvenWhenOrderIsPaidAndBound()
    {
        var order = OrderTestFactory.CreatePendingOrder();
        order.MarkAsPaidPendingFulfillment(Guid.NewGuid(), "authoritative-reference");
        var repository = RepositoryWith(order, withLineItems: true);
        var handler = new CompleteOrderCommandHandler(
            repository.Object,
            Mock.Of<IEntitlementService>(),
            Mock.Of<IApplicationDbContext>(),
            Mock.Of<IOrderPaymentAuthority>(),
            CreateActor(Guid.NewGuid(), order.TenantId!.Value));

        var result = await handler.Handle(new CompleteOrderCommand(order.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Orders.Forbidden");
        order.Status.Should().Be(OrderStatus.Paid);
    }

    [Fact]
    public async Task CompleteOrder_RejectsUnconfirmedPaymentBinding()
    {
        var order = OrderTestFactory.CreatePendingOrder();
        OrderTestFactory.AddLineItem(order, Guid.NewGuid(), "Bound product", 45m);
        var paymentId = Guid.NewGuid();
        order.MarkAsPaidPendingFulfillment(paymentId, "provider-object");
        var repository = RepositoryWith(order, withLineItems: true);
        var paymentAuthority = new Mock<IOrderPaymentAuthority>();
        paymentAuthority.Setup(mock => mock.IsSettledAsync(
                It.Is<OrderPaymentBinding>(binding =>
                    binding.OrderId == order.Id &&
                    binding.PaymentId == paymentId &&
                    binding.UserId == order.UserId &&
                    binding.TenantId == order.TenantId &&
                    binding.Amount == order.Total &&
                    binding.Currency == order.Currency),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var handler = new CompleteOrderCommandHandler(
            repository.Object,
            Mock.Of<IEntitlementService>(),
            Mock.Of<IApplicationDbContext>(),
            paymentAuthority.Object,
            CreateActor(order.UserId, order.TenantId!.Value));

        var result = await handler.Handle(new CompleteOrderCommand(order.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Orders.PaymentAuthorityRequired");
        order.Status.Should().Be(OrderStatus.Paid);
        repository.Verify(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CompleteOrder_RejectsSubscriptionWithoutSubscriptionAuthority()
    {
        var order = OrderTestFactory.CreatePendingOrder();
        OrderTestFactory.AddLineItem(
            order,
            Guid.NewGuid(),
            "Subscription product",
            30m,
            isSubscription: true);
        order.MarkAsPaidPendingFulfillment(Guid.NewGuid(), "provider-subscription");
        var repository = RepositoryWith(order, withLineItems: true);
        var dbContext = new Mock<IApplicationDbContext>();
        var handler = new CompleteOrderCommandHandler(
            repository.Object,
            Mock.Of<IEntitlementService>(),
            dbContext.Object,
            OrderTestFactory.CreatePaymentAuthority(),
            CreateActor(order.UserId, order.TenantId!.Value));

        var result = await handler.Handle(new CompleteOrderCommand(order.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Orders.SubscriptionAuthorityRequired");
        order.Status.Should().Be(OrderStatus.Paid);
        dbContext.Verify(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static PricingFixture CreatePricingFixture(
        bool isPublished = true,
        Guid? productTenantId = null,
        bool staleVersion = false,
        decimal basePrice = 100m,
        decimal? salePrice = null,
        string currency = "USD",
        int quantity = 1,
        string? fixedPromoCurrency = null)
    {
        var userId = Guid.NewGuid();
        var orderTenantId = Guid.NewGuid();
        var resolvedProductTenantId = productTenantId ?? orderTenantId;
        var order = Order.Create(userId, $"pricing-{Guid.NewGuid():N}", orderTenantId);
        var product = Product.Create("Authoritative product", tenantId: resolvedProductTenantId);
        product.IsPublished = isPublished;
        var (pricing, initialVersion) = ProductPricing.CreateWithVersion(
            product.Id,
            "Explicit tier",
            basePrice,
            salePrice,
            currency,
            isDefault: false,
            tenantId: resolvedProductTenantId);

        var requestedVersion = initialVersion;
        if (staleVersion)
        {
            pricing.Versions.Add(initialVersion);
            pricing.UpdateBasePrice(basePrice + 10m, "new current price");
        }

        var orderRepository = RepositoryWith(order, withLineItems: true);
        var productRepository = new Mock<IProductRepository>();
        productRepository.Setup(mock => mock.GetByIdAsync(
                product.Id,
                It.IsAny<CancellationToken>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<bool?>()))
            .ReturnsAsync(product);
        var pricingRepository = new Mock<IProductPricingRepository>();
        pricingRepository.Setup(mock => mock.GetByIdAsync(pricing.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pricing);
        var dbContext = new Mock<IApplicationDbContext>();
        dbContext.Setup(mock => mock.Set<ProductPricingVersion>())
            .Returns(new TestAsyncDbSet<ProductPricingVersion>([requestedVersion]));
        var promoCodeService = new Mock<IPromoCodeService>();
        if (fixedPromoCurrency is not null)
        {
            var promoCode = new PromoCode
            {
                Code = "FIXED",
                Type = PromoCodeType.FixedAmountOff,
                DiscountAmount = 10m,
                Currency = fixedPromoCurrency,
                TenantId = orderTenantId
            };
            promoCodeService.Setup(mock => mock.GetPromoCodeByCodeAsync(promoCode.Code))
                .ReturnsAsync(promoCode);
            promoCodeService.Setup(mock => mock.ValidatePromoCodeAsync(promoCode.Code, userId, product.Id))
                .ReturnsAsync(true);
        }

        var handler = new AddProductToOrderCommandHandler(
            orderRepository.Object,
            productRepository.Object,
            pricingRepository.Object,
            promoCodeService.Object,
            dbContext.Object,
            CreateActor(userId, orderTenantId));
        var command = new AddProductToOrderCommand(
            order.Id,
            product.Id,
            pricing.Id,
            requestedVersion.Id,
            quantity,
            fixedPromoCurrency is null ? null : "FIXED");

        return new PricingFixture(order, pricing, requestedVersion, command, handler);
    }

    private static Mock<IOrderRepository> RepositoryWith(Order order, bool withLineItems = false)
    {
        var repository = new Mock<IOrderRepository>();
        if (withLineItems)
        {
            repository.Setup(mock => mock.GetWithLineItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);
        }
        else
        {
            repository.Setup(mock => mock.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);
        }

        return repository;
    }

    private static IActorContextAccessor CreateActor(
        Guid userId,
        Guid tenantId,
        ActorKind actorKind = ActorKind.User)
    {
        var accessor = new Mock<IActorContextAccessor>();
        accessor.SetupGet(mock => mock.ActorContext).Returns(new ActorContext
        {
            ActorKind = actorKind,
            SubjectId = userId.ToString(),
            TenantId = tenantId,
            Roles = new HashSet<string>(),
            Permissions = new HashSet<string>(),
            IsAuthenticated = true
        });
        return accessor.Object;
    }

    private sealed record PricingFixture(
        Order Order,
        ProductPricing Pricing,
        ProductPricingVersion Version,
        AddProductToOrderCommand Command,
        AddProductToOrderCommandHandler Handler);
}
