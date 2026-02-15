using FluentAssertions;
using GameGuild.Commerce.Products;
using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Orders.UnitTests;

/// <summary>
/// Tests for OrdersController via ISender mocking.
/// </summary>
public class OrdersControllerTests
{
    private readonly Mock<ISender> _senderMock = new();
    private readonly Mock<IActorContextAccessor> _actorMock = new();
    private readonly OrdersController _sut;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _tenantId = Guid.NewGuid();

    public OrdersControllerTests()
    {
        var actorContext = new ActorContext
        {
            SubjectId = _userId.ToString(),
            TenantId = _tenantId,
            ActorKind = ActorKind.User,
            Roles = new HashSet<string>(),
            Permissions = new HashSet<string>(),
            IsAuthenticated = true
        };
        _actorMock.Setup(a => a.ActorContext).Returns(actorContext);
        _sut = new OrdersController(_senderMock.Object, _actorMock.Object);

        // Setup HttpContext for GetIpAddress() and GetUserAgent()
        var httpContext = new DefaultHttpContext();
        _sut.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    private Order CreateTestOrder()
    {
        return Order.Create(_userId, "idem-" + Guid.NewGuid().ToString("N")[..16], _tenantId);
    }

    // ── CreateOrder ──

    [Fact]
    public async Task CreateOrder_ReturnsCreated_OnSuccess()
    {
        var order = CreateTestOrder();
        var opResult = OrderOperationResult.FromOrder(order);
        _senderMock.Setup(s => s.Send<Result<OrderOperationResult>>(It.IsAny<CreateOrderCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<OrderOperationResult>(opResult));

        var request = new CreateOrderRequest(_userId, "idem-key-12345678");
        var result = await _sut.CreateOrder(request);

        result.Result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task CreateOrder_ReturnsBadRequest_OnFailure()
    {
        _senderMock.Setup(s => s.Send<Result<OrderOperationResult>>(It.IsAny<CreateOrderCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<OrderOperationResult>(Error.Failure("Error", "failed")));

        var result = await _sut.CreateOrder(new CreateOrderRequest(_userId, "key-12345678"));

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CreateOrder_ReturnsOk_WhenDuplicate()
    {
        var order = CreateTestOrder();
        var opResult = OrderOperationResult.FromOrder(order, wasDuplicate: true);
        _senderMock.Setup(s => s.Send<Result<OrderOperationResult>>(It.IsAny<CreateOrderCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<OrderOperationResult>(opResult));

        var result = await _sut.CreateOrder(new CreateOrderRequest(_userId, "dup-key-12345678"));

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    // ── AddProductToOrder ──

    [Fact]
    public async Task AddProductToOrder_ReturnsOk_OnSuccess()
    {
        var order = CreateTestOrder();
        _senderMock.Setup(s => s.Send<Result<Order>>(It.IsAny<AddProductToOrderCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<Order>(order));

        var result = await _sut.AddProductToOrder(order.Id, new AddOrderItemRequest(Guid.NewGuid()));

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task AddProductToOrder_ReturnsBadRequest_OnFailure()
    {
        _senderMock.Setup(s => s.Send<Result<Order>>(It.IsAny<AddProductToOrderCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<Order>(Error.Failure("Error", "not found")));

        var result = await _sut.AddProductToOrder(Guid.NewGuid(), new AddOrderItemRequest(Guid.NewGuid()));

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ── CompleteOrder ──

    [Fact]
    public async Task CompleteOrder_ReturnsOk_OnSuccess()
    {
        var order = CreateTestOrder();
        _senderMock.Setup(s => s.Send<Result<OrderOperationResult>>(It.IsAny<CompleteOrderCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<OrderOperationResult>(OrderOperationResult.FromOrder(order)));

        var result = await _sut.CompleteOrder(order.Id, new CompleteOrderRequest());

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    // ── CancelOrder ──

    [Fact]
    public async Task CancelOrder_ReturnsNoContent_OnSuccess()
    {
        _senderMock.Setup(s => s.Send<Result>(It.IsAny<CancelOrderCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await _sut.CancelOrder(Guid.NewGuid());

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task CancelOrder_ReturnsBadRequest_OnFailure()
    {
        _senderMock.Setup(s => s.Send<Result>(It.IsAny<CancelOrderCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(Error.Failure("Error", "cannot cancel")));

        var result = await _sut.CancelOrder(Guid.NewGuid());

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ── RefundOrder ──

    [Fact]
    public async Task RefundOrder_ReturnsOk_OnSuccess()
    {
        var order = CreateTestOrder();
        _senderMock.Setup(s => s.Send<Result<OrderOperationResult>>(It.IsAny<RefundOrderCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<OrderOperationResult>(OrderOperationResult.FromOrder(order)));

        var result = await _sut.RefundOrder(order.Id, new RefundOrderRequest(10m, "reason"));

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    // ── GetOrder ──

    [Fact]
    public async Task GetOrder_ReturnsOk_WhenFound()
    {
        var order = CreateTestOrder();
        _senderMock.Setup(s => s.Send<Order?>(It.IsAny<GetOrderQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var result = await _sut.GetOrder(order.Id);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetOrder_ReturnsNotFound_WhenNull()
    {
        _senderMock.Setup(s => s.Send<Order?>(It.IsAny<GetOrderQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var result = await _sut.GetOrder(Guid.NewGuid());

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    // ── ListOrders ──

    [Fact]
    public async Task ListOrders_ReturnsAllOrders_WhenNoOwnerFilter()
    {
        var order = CreateTestOrder();
        _senderMock.Setup(s => s.Send<IEnumerable<Order>>(It.IsAny<GetAllOrdersQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { order });

        var result = await _sut.ListOrders();

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ListOrders_ReturnsUserOrders_WhenOwnerIsMe()
    {
        var order = CreateTestOrder();
        _senderMock.Setup(s => s.Send<IEnumerable<Order>>(It.IsAny<GetUserOrdersQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { order });

        var result = await _sut.ListOrders(owner: "me");

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    // ── OrderExists ──

    [Fact]
    public async Task OrderExists_ReturnsOk_WhenExists()
    {
        _senderMock.Setup(s => s.Send<bool>(It.IsAny<OrderExistsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.OrderExists(Guid.NewGuid());

        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task OrderExists_ReturnsNotFound_WhenNotExists()
    {
        _senderMock.Setup(s => s.Send<bool>(It.IsAny<OrderExistsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _sut.OrderExists(Guid.NewGuid());

        result.Should().BeOfType<NotFoundResult>();
    }

    // ── UpdateOrder ──

    [Fact]
    public async Task UpdateOrder_ReturnsOk_OnSuccess()
    {
        var order = CreateTestOrder();
        _senderMock.Setup(s => s.Send<Result<OrderOperationResult>>(It.IsAny<UpdateOrderCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<OrderOperationResult>(OrderOperationResult.FromOrder(order)));

        var result = await _sut.UpdateOrder(order.Id, new PatchOrderRequest(Currency: "EUR"));

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    // ── DeleteOrder ──

    [Fact]
    public async Task DeleteOrder_ReturnsNoContent_OnSuccess()
    {
        _senderMock.Setup(s => s.Send<Result>(It.IsAny<DeleteOrderCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await _sut.DeleteOrder(Guid.NewGuid(), "reason");

        result.Should().BeOfType<NoContentResult>();
    }

    // ── CaptureOrder ──

    [Fact]
    public async Task CaptureOrder_ReturnsOk_OnSuccess()
    {
        var order = CreateTestOrder();
        _senderMock.Setup(s => s.Send<Result<OrderOperationResult>>(It.IsAny<CaptureOrderCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<OrderOperationResult>(OrderOperationResult.FromOrder(order)));

        var result = await _sut.CaptureOrder(order.Id, new CaptureOrderRequest(50m));

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    // ── HoldOrder ──

    [Fact]
    public async Task HoldOrder_ReturnsOk_OnSuccess()
    {
        var order = CreateTestOrder();
        _senderMock.Setup(s => s.Send<Result<OrderOperationResult>>(It.IsAny<HoldOrderCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<OrderOperationResult>(OrderOperationResult.FromOrder(order)));

        var result = await _sut.HoldOrder(order.Id, new HoldOrderRequest("hold reason"));

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    // ── ReleaseOrder ──

    [Fact]
    public async Task ReleaseOrder_ReturnsOk_OnSuccess()
    {
        var order = CreateTestOrder();
        _senderMock.Setup(s => s.Send<Result<OrderOperationResult>>(It.IsAny<ReleaseOrderCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<OrderOperationResult>(OrderOperationResult.FromOrder(order)));

        var result = await _sut.ReleaseOrder(order.Id);

        result.Result.Should().BeOfType<OkObjectResult>();
    }
}

/// <summary>
/// Tests for handler instantiation (covers primary constructor lines),
/// module DI registration, and EF model configuration.
/// </summary>
public class OrdersInfrastructureTests
{
    // ── Handler Instantiation ──

    [Fact]
    public void CommandHandlers_CanBeInstantiated()
    {
        var orderRepo = Mock.Of<IOrderRepository>();
        var productRepo = Mock.Of<IProductRepository>();
        var pricingRepo = Mock.Of<IProductPricingRepository>();
        var promoService = Mock.Of<IPromoCodeService>();
        var entitlementService = Mock.Of<IEntitlementService>();
        var dbContext = Mock.Of<IApplicationDbContext>();

        var handlers = new object[]
        {
            new CreateOrderCommandHandler(orderRepo),
            new AddProductToOrderCommandHandler(orderRepo, productRepo, pricingRepo, promoService),
            new CancelOrderCommandHandler(orderRepo),
            new CaptureOrderCommandHandler(orderRepo),
            new CompleteOrderCommandHandler(orderRepo, entitlementService, dbContext),
            new DeleteOrderCommandHandler(orderRepo),
            new HoldOrderCommandHandler(orderRepo),
            new RefundOrderCommandHandler(orderRepo, entitlementService),
            new ReleaseOrderCommandHandler(orderRepo),
            new UpdateOrderCommandHandler(orderRepo)
        };

        handlers.Should().AllSatisfy(h => h.Should().NotBeNull());
    }

    [Fact]
    public void QueryHandlers_CanBeInstantiated()
    {
        var orderRepo = Mock.Of<IOrderRepository>();
        var dbContext = Mock.Of<IApplicationDbContext>();

        var handlers = new object[]
        {
            new GetOrderQueryHandler(orderRepo),
            new GetAllOrdersQueryHandler(dbContext),
            new GetUserOrdersQueryHandler(orderRepo),
            new OrderExistsQueryHandler(orderRepo)
        };

        handlers.Should().AllSatisfy(h => h.Should().NotBeNull());
    }

    [Fact]
    public void OrderStateChangedEventHandler_CanBeInstantiated()
    {
        var dbContext = Mock.Of<IApplicationDbContext>();
        var logger = NullLogger<OrderStateChangedEventHandler>.Instance;

        var handler = new OrderStateChangedEventHandler(dbContext, logger);

        handler.Should().NotBeNull();
    }

    // ── Repository ──

    [Fact]
    public void OrderRepository_CanBeInstantiated()
    {
        var dbContext = new Mock<IApplicationDbContext>();
        var mockDbSet = new Mock<Microsoft.EntityFrameworkCore.DbSet<Order>>();
        dbContext.Setup(c => c.Set<Order>()).Returns(mockDbSet.Object);

        var repo = new OrderRepository(dbContext.Object);

        repo.Should().NotBeNull();
        repo.Should().BeAssignableTo<IOrderRepository>();
    }

    // ── Module DI Registration ──

    [Fact]
    public void AddOrdersModule_RegistersOrderRepository()
    {
        var services = new ServiceCollection();
        services.AddScoped<IApplicationDbContext>(_ => Mock.Of<IApplicationDbContext>());

        services.AddOrdersModule();

        var provider = services.BuildServiceProvider();
        var repo = provider.GetService<IOrderRepository>();
        repo.Should().NotBeNull();
        repo.Should().BeOfType<OrderRepository>();
    }

    [Fact]
    public void AddOrdersModule_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();
        var result = services.AddOrdersModule();
        result.Should().BeSameAs(services);
    }

    // ── EF Model Configuration ──

    [Fact]
    public void ConfigureOrdersModel_ConfiguresEntities()
    {
        var modelBuilder = new ModelBuilder(new ConventionSet());

        OrdersModule.ConfigureOrdersModel(modelBuilder);

        modelBuilder.Model.FindEntityType(typeof(Order)).Should().NotBeNull();
        modelBuilder.Model.FindEntityType(typeof(OrderLineItem)).Should().NotBeNull();
        modelBuilder.Model.FindEntityType(typeof(OrderAuditLog)).Should().NotBeNull();
    }

    [Fact]
    public void OrdersModelConfiguration_DelegatesToModule()
    {
        var config = new OrdersModelConfiguration();
        var modelBuilder = new ModelBuilder(new ConventionSet());

        config.Configure(modelBuilder);

        modelBuilder.Model.FindEntityType(typeof(Order)).Should().NotBeNull();
    }

    // ── DTO Instantiation ──

    [Fact]
    public void OrderDto_CanBeCreated()
    {
        var dto = new OrderDto(
            Guid.NewGuid(), Guid.NewGuid(), "key", OrderStatus.Pending,
            100m, 10m, 5m, 95m, "USD", null, null, null, null, null, null,
            DateTime.UtcNow, DateTime.UtcNow, new List<OrderLineItemDto>());

        dto.Should().NotBeNull();
        dto.Status.Should().Be(OrderStatus.Pending);
        dto.LineItems.Should().BeEmpty();
    }

    [Fact]
    public void OrderLineItemDto_CanBeCreated()
    {
        var dto = new OrderLineItemDto(
            Guid.NewGuid(), Guid.NewGuid(), "Product", 10m, 12m, 10m,
            2, 4m, "PROMO10", 16m, false);

        dto.Should().NotBeNull();
        dto.ProductName.Should().Be("Product");
        dto.Quantity.Should().Be(2);
    }

    [Fact]
    public void RequestRecords_CanBeCreated()
    {
        var create = new CreateOrderRequest(Guid.NewGuid(), "key-12345678", "USD", Guid.NewGuid());
        var addItem = new AddOrderItemRequest(Guid.NewGuid(), 2, "PROMO");
        var complete = new CompleteOrderRequest(Guid.NewGuid(), "ref-123", "card");
        var patch = new PatchOrderRequest("EUR", "notes", new Dictionary<string, string> { ["k"] = "v" });
        var capture = new CaptureOrderRequest(50m);
        var hold = new HoldOrderRequest("hold reason");

        create.Should().NotBeNull();
        addItem.Quantity.Should().Be(2);
        complete.PaymentMethod.Should().Be("card");
        patch.Currency.Should().Be("EUR");
        capture.Amount.Should().Be(50m);
        hold.Reason.Should().Be("hold reason");
    }

    // ── OrderLineItem partial constructor ──

    [Fact]
    public void OrderLineItem_PartialConstructor_CanBeCreated()
    {
        var lineItem = new OrderLineItem(new object());
        lineItem.Should().NotBeNull();
    }
}
