using FluentAssertions;
using Xunit;

namespace GameGuild.Commerce.Orders.UnitTests;

public sealed class MarketplaceCartTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    [Fact]
    public void Create_RequiresTenantAndUser()
    {
        var actWithoutTenant = () => MarketplaceCart.Create(Guid.Empty, _userId);
        var actWithoutUser = () => MarketplaceCart.Create(_tenantId, Guid.Empty);
        actWithoutTenant.Should().Throw<ArgumentException>();
        actWithoutUser.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddItem_IsIdempotent_AndAggregatesTheSameImmutablePrice()
    {
        var cart = MarketplaceCart.Create(_tenantId, _userId);
        var productId = Guid.NewGuid();
        var pricingId = Guid.NewGuid();
        var versionId = Guid.NewGuid();

        var first = cart.AddItem(productId, pricingId, versionId, 2, "first");
        var duplicate = cart.AddItem(productId, pricingId, versionId, 2, "first");
        var aggregated = cart.AddItem(productId, pricingId, versionId, 3, "second");

        duplicate.Should().BeSameAs(first);
        aggregated.Should().BeSameAs(first);
        cart.Items.Should().ContainSingle();
        first.Quantity.Should().Be(5);
        first.TenantId.Should().Be(_tenantId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void AddItem_RejectsInvalidQuantity(int quantity)
    {
        var cart = MarketplaceCart.Create(_tenantId, _userId);
        var act = () => cart.AddItem(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), quantity, "key");
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void AddItem_RejectsMissingIdentifiersAndKey()
    {
        var cart = MarketplaceCart.Create(_tenantId, _userId);
        var valid = Guid.NewGuid();
        FluentActions.Invoking(() => cart.AddItem(Guid.Empty, valid, valid, 1, "key")).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => cart.AddItem(valid, Guid.Empty, valid, 1, "key")).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => cart.AddItem(valid, valid, Guid.Empty, 1, "key")).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => cart.AddItem(valid, valid, valid, 1, " ")).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SetAndRemove_RequireExistingItemsAndValidQuantities()
    {
        var cart = MarketplaceCart.Create(_tenantId, _userId);
        var item = cart.AddItem(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, "key");

        cart.SetQuantity(item.Id, 4);
        item.Quantity.Should().Be(4);
        FluentActions.Invoking(() => cart.SetQuantity(item.Id, 0)).Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => cart.SetQuantity(Guid.NewGuid(), 1)).Should().Throw<KeyNotFoundException>();
        FluentActions.Invoking(() => cart.RemoveItem(Guid.NewGuid())).Should().Throw<KeyNotFoundException>();

        cart.RemoveItem(item.Id);
        cart.Items.Should().BeEmpty();
    }

    [Fact]
    public void Checkout_IsTerminalAndRequiresItems()
    {
        var empty = MarketplaceCart.Create(_tenantId, _userId);
        FluentActions.Invoking(() => empty.MarkCheckedOut(SystemClock.UtcNow)).Should().Throw<InvalidOperationException>();

        var cart = MarketplaceCart.Create(_tenantId, _userId);
        var item = cart.AddItem(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, "key");
        var checkedOutAt = SystemClock.UtcNow;
        cart.MarkCheckedOut(checkedOutAt);

        cart.State.Should().Be(MarketplaceCartState.CheckedOut);
        cart.CheckedOutAt.Should().Be(checkedOutAt);
        FluentActions.Invoking(() => cart.AddItem(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, "next")).Should().Throw<InvalidOperationException>();
        FluentActions.Invoking(() => cart.SetQuantity(item.Id, 2)).Should().Throw<InvalidOperationException>();
        FluentActions.Invoking(() => cart.RemoveItem(item.Id)).Should().Throw<InvalidOperationException>();
        FluentActions.Invoking(() => cart.MarkCheckedOut(checkedOutAt)).Should().Throw<InvalidOperationException>();
    }
}
