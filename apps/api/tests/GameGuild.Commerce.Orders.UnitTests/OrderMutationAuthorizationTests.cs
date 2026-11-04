using FluentAssertions;
using GameGuild.Commerce.Products;
using GameGuild.Identity.Context.Actors;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Orders.UnitTests;
public sealed class OrderMutationAuthorizationTests
{
    public static TheoryData<string, string> DeniedActors
    {
        get
        {
            var cases = new TheoryData<string, string>();
            foreach (var operation in new[]
            {
                "cancel",
                "delete",
                "hold",
                "release"
            }

            )
            {
                foreach (var actor in new[]
                {
                    "other-user",
                    "other-tenant",
                    "anonymous",
                    "missing",
                    "no-tenant",
                    "invalid-subject",
                    "service"
                }

                )
                {
                    cases.Add(operation, actor);
                }
            }

            return cases;
        }
    }

    [Theory]
    [MemberData(nameof(DeniedActors))]
    public async Task Mutation_RejectsActorWithoutChangingOrderOrInventory(string operation, string actorKind)
    {
        var order = CreateOrder(operation);
        var repository = RepositoryWith(order);
        var inventory = new Mock<IProductInventoryService>();
        var actor = ActorFor(order, actorKind);
        var status = order.Status;
        var updatedAt = order.UpdatedAt;
        var version = order.Version;
        var events = order.DomainEvents.ToArray();
        var result = await Execute(operation, order.Id, repository.Object, actor, inventory.Object);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(actorKind switch
        {
            "other-user" or "other-tenant" => "Orders.Forbidden",
            "service" => "Orders.UserActorRequired",
            _ => "Orders.Unauthenticated"
        });
        order.Status.Should().Be(status);
        order.IsDeleted.Should().BeFalse();
        order.UpdatedAt.Should().Be(updatedAt);
        order.Version.Should().Be(version);
        order.DomainEvents.Should().Equal(events);
        repository.Verify(mock => mock.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        inventory.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("cancel")]
    [InlineData("delete")]
    [InlineData("hold")]
    [InlineData("release")]
    public async Task Mutation_AuthorizesBeforeDisclosingInvalidOrderState(string operation)
    {
        var order = CreateOrder(operation);
        typeof(Order).GetProperty(nameof(Order.Status))!.SetValue(order, OrderStatus.Fulfilled);
        var repository = RepositoryWith(order);
        var inventory = new Mock<IProductInventoryService>();
        var result = await Execute(operation, order.Id, repository.Object, ActorFor(order, "other-user"), inventory.Object);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Orders.Forbidden");
        repository.Verify(mock => mock.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        inventory.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("cancel", OrderStatus.Cancelled)]
    [InlineData("delete", OrderStatus.Pending)]
    [InlineData("hold", OrderStatus.OnHold)]
    [InlineData("release", OrderStatus.Pending)]
    public async Task Mutation_AllowsOwnerInCurrentTenant(string operation, OrderStatus expectedStatus)
    {
        var order = CreateOrder(operation);
        var repository = RepositoryWith(order);
        var inventory = new Mock<IProductInventoryService>();
        var result = await Execute(operation, order.Id, repository.Object, ActorFor(order, "owner"), inventory.Object);
        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(expectedStatus);
        order.IsDeleted.Should().Be(operation == "delete");
        repository.Verify(mock => mock.UpdateAsync(order, It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        if (operation == "cancel")
        {
            inventory.Verify(mock => mock.ReleaseAsync(It.Is<IReadOnlyCollection<(Guid ProductId, int Quantity)>>(lines => lines.Count == 1 && lines.First().ProductId == order.LineItems.First().ProductId), It.IsAny<CancellationToken>()), Times.Once);
        }

        inventory.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("cancel")]
    [InlineData("delete")]
    [InlineData("hold")]
    [InlineData("release")]
    public async Task Mutation_PreservesNotFoundForMissingOrder(string operation)
    {
        var repository = new Mock<IOrderRepository>();
        var inventory = new Mock<IProductInventoryService>();
        var result = await Execute(operation, Guid.NewGuid(), repository.Object, Mock.Of<IActorContextAccessor>(), inventory.Object);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Orders.NotFound");
        repository.Verify(mock => mock.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        inventory.VerifyNoOtherCalls();
    }

    private static Order CreateOrder(string operation)
    {
        var order = OrderTestFactory.CreatePendingOrder();
        OrderTestFactory.AddLineItem(order, Guid.NewGuid(), "Reserved item", 10m);
        if (operation == "release")
        {
            order.PlaceOnHold("Review");
        }

        return order;
    }

    private static Mock<IOrderRepository> RepositoryWith(Order order)
    {
        var repository = new Mock<IOrderRepository>();
        repository.Setup(mock => mock.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        repository.Setup(mock => mock.GetWithLineItemsAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        return repository;
    }

    private static IActorContextAccessor ActorFor(Order order, string kind)
    {
        if (kind == "missing")
        {
            return Mock.Of<IActorContextAccessor>();
        }

        var actor = new ActorContext
        {
            ActorKind = kind == "service" ? ActorKind.Service : ActorKind.User,
            SubjectId = kind == "invalid-subject" ? "invalid" : (kind == "other-user" ? Guid.NewGuid() : order.UserId).ToString(),
            TenantId = kind == "no-tenant" ? null : kind == "other-tenant" ? Guid.NewGuid() : order.TenantId,
            IsAuthenticated = kind != "anonymous",
            Roles = new HashSet<string>(),
            Permissions = new HashSet<string>()
        };
        var accessor = new Mock<IActorContextAccessor>();
        accessor.SetupGet(mock => mock.ActorContext).Returns(actor);
        return accessor.Object;
    }

    private static async Task<Result> Execute(string operation, Guid orderId, IOrderRepository repository, IActorContextAccessor actor, IProductInventoryService inventory)
    {
        using var services = new ServiceCollection().AddSingleton(repository).AddSingleton(actor).AddSingleton(inventory).BuildServiceProvider();
        return operation switch
        {
            "cancel" => await ActivatorUtilities.CreateInstance<CancelOrderCommandHandler>(services).Handle(new CancelOrderCommand(orderId, "Requested"), CancellationToken.None),
            "delete" => await ActivatorUtilities.CreateInstance<DeleteOrderCommandHandler>(services).Handle(new DeleteOrderCommand(orderId), CancellationToken.None),
            "hold" => await ActivatorUtilities.CreateInstance<HoldOrderCommandHandler>(services).Handle(new HoldOrderCommand(orderId, "Review"), CancellationToken.None),
            "release" => await ActivatorUtilities.CreateInstance<ReleaseOrderCommandHandler>(services).Handle(new ReleaseOrderCommand(orderId), CancellationToken.None),
            _ => throw new ArgumentOutOfRangeException(nameof(operation))};
    }
}
