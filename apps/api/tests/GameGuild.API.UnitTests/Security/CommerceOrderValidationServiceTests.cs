using FluentAssertions;
using GameGuild.API.Security;
using GameGuild.Assets.Security;
using GameGuild.Commerce.Orders;
using Moq;
using Xunit;
using AssetsOrderStatus = GameGuild.Assets.Security.OrderStatus;

namespace GameGuild.API.UnitTests.Security;

/// <summary>
///     Tests for the composition-root order-validation adapter that binds the Assets
///     module's generic <see cref="IOrderValidationService"/> to the commerce stack.
/// </summary>
public sealed class CommerceOrderValidationServiceTests
{
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly CommerceOrderValidationService _sut;

    public CommerceOrderValidationServiceTests()
    {
        _sut = new CommerceOrderValidationService(_orderRepository.Object);
    }

    [Fact]
    public async Task GetOrderStatusAsync_FulfilledOrder_MapsToFulfilled()
    {
        var orderId = Guid.NewGuid();
        _orderRepository.Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateOrder(orderId, fulfilled: true));

        var status = await _sut.GetOrderStatusAsync(orderId);

        status.Should().Be(AssetsOrderStatus.Fulfilled);
    }

    [Fact]
    public async Task GetOrderStatusAsync_UnknownOrder_ReturnsNull()
    {
        _orderRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var status = await _sut.GetOrderStatusAsync(Guid.NewGuid());

        status.Should().BeNull();
    }

    [Fact]
    public async Task IsOrderValidForDownloadAsync_PaidOrder_ReturnsTrue()
    {
        var orderId = Guid.NewGuid();
        _orderRepository.Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateOrder(orderId, fulfilled: false));

        var valid = await _sut.IsOrderValidForDownloadAsync(orderId);

        valid.Should().BeTrue();
    }

    [Fact]
    public async Task IsOrderValidForDownloadAsync_UnknownOrder_ReturnsFalse()
    {
        _orderRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var valid = await _sut.IsOrderValidForDownloadAsync(Guid.NewGuid());

        valid.Should().BeFalse();
    }

    private static Order CreateOrder(Guid id, bool fulfilled)
    {
        var order = Order.Create(
            Guid.NewGuid(),
            $"idem-{Guid.NewGuid():N}",
            Guid.NewGuid());
        typeof(Order).GetProperty(nameof(Order.Id))!.SetValue(order, id);
        order.MarkAsPaidPendingFulfillment(Guid.NewGuid());
        if (fulfilled)
        {
            order.MarkAsFulfilled();
        }

        return order;
    }
}
