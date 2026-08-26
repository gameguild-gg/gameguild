using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace GameGuild.Commerce.Orders.UnitTests.Validators;

public class CancelOrderCommandValidatorTests
{
    private readonly CancelOrderCommandValidator _sut = new();

    [Fact]
    public void ShouldValidateOrderIdAndReason()
    {
        _sut.TestValidate(new CancelOrderCommand(Guid.Empty)).ShouldHaveValidationErrorFor(command => command.OrderId);
        _sut.TestValidate(new CancelOrderCommand(Guid.NewGuid(), new string('a', 501))).ShouldHaveValidationErrorFor(command => command.Reason);
        _sut.TestValidate(new CancelOrderCommand(Guid.NewGuid(), new string('a', 500))).ShouldNotHaveValidationErrorFor(command => command.Reason);
    }
}

public class DeleteOrderCommandValidatorTests
{
    private readonly DeleteOrderCommandValidator _sut = new();

    [Fact]
    public void ShouldRequireValidOrderId()
    {
        _sut.TestValidate(new DeleteOrderCommand(Guid.Empty)).ShouldHaveValidationErrorFor(command => command.OrderId);
        _sut.TestValidate(new DeleteOrderCommand(Guid.NewGuid())).ShouldNotHaveValidationErrorFor(command => command.OrderId);
    }
}

public class CompleteOrderCommandValidatorTests
{
    private readonly CompleteOrderCommandValidator _sut = new();

    [Fact]
    public void ShouldRequireValidOrderId()
    {
        _sut.TestValidate(new CompleteOrderCommand(Guid.Empty)).ShouldHaveValidationErrorFor(command => command.OrderId);
        _sut.TestValidate(new CompleteOrderCommand(Guid.NewGuid())).ShouldNotHaveValidationErrorFor(command => command.OrderId);
    }

    [Fact]
    public void ShouldValidateEconomyMarketplaceSettlementEvidence()
    {
        var invalid = new CompleteOrderMarketplaceSettlement(
            (OrderMarketplaceCurrencyChoice)0,
            string.Empty,
            Guid.Empty,
            string.Empty,
            string.Empty);
        var valid = new CompleteOrderMarketplaceSettlement(
            OrderMarketplaceCurrencyChoice.FixedMix,
            "BR",
            Guid.NewGuid(),
            "operation",
            "idempotency");

        _sut.TestValidate(new CompleteOrderCommand(Guid.NewGuid(), MarketplaceSettlement: invalid))
            .ShouldHaveValidationErrorFor(command => command.MarketplaceSettlement);
        _sut.TestValidate(new CompleteOrderCommand(Guid.NewGuid(), MarketplaceSettlement: valid))
            .ShouldNotHaveValidationErrorFor(command => command.MarketplaceSettlement);
    }
}

public class UpdateOrderCommandValidatorTests
{
    private readonly UpdateOrderCommandValidator _sut = new();

    [Fact]
    public void ShouldValidateOrderIdAndCurrency()
    {
        _sut.TestValidate(new UpdateOrderCommand(Guid.Empty)).ShouldHaveValidationErrorFor(command => command.OrderId);
        _sut.TestValidate(new UpdateOrderCommand(Guid.NewGuid(), Currency: null)).ShouldNotHaveValidationErrorFor(command => command.Currency);
        _sut.TestValidate(new UpdateOrderCommand(Guid.NewGuid(), Currency: "US")).ShouldHaveValidationErrorFor(command => command.Currency);
        _sut.TestValidate(new UpdateOrderCommand(Guid.NewGuid(), Currency: "USD")).ShouldNotHaveValidationErrorFor(command => command.Currency);
    }
}

public class RefundOrderCommandValidatorTests
{
    private readonly RefundOrderCommandValidator _sut = new();

    [Fact]
    public void ShouldValidateOrderIdAndAmount()
    {
        _sut.TestValidate(new RefundOrderCommand(Guid.Empty)).ShouldHaveValidationErrorFor(command => command.OrderId);
        _sut.TestValidate(new RefundOrderCommand(Guid.NewGuid(), Amount: 0)).ShouldHaveValidationErrorFor(command => command.Amount);
        _sut.TestValidate(new RefundOrderCommand(Guid.NewGuid(), Amount: -10)).ShouldHaveValidationErrorFor(command => command.Amount);
        _sut.TestValidate(new RefundOrderCommand(Guid.NewGuid(), Amount: null)).ShouldNotHaveValidationErrorFor(command => command.Amount);
        _sut.TestValidate(new RefundOrderCommand(Guid.NewGuid(), Amount: 50m)).ShouldNotHaveValidationErrorFor(command => command.Amount);
    }
}

public class AddProductToOrderCommandValidatorTests
{
    private readonly AddProductToOrderCommandValidator _sut = new();

    [Fact]
    public void ShouldValidateOrderProductAndQuantity()
    {
        var productId = Guid.NewGuid();
        var pricingId = Guid.NewGuid();
        var pricingVersionId = Guid.NewGuid();
        _sut.TestValidate(new AddProductToOrderCommand(Guid.Empty, productId, pricingId, pricingVersionId, 1)).ShouldHaveValidationErrorFor(command => command.OrderId);
        _sut.TestValidate(new AddProductToOrderCommand(Guid.NewGuid(), Guid.Empty, pricingId, pricingVersionId, 1)).ShouldHaveValidationErrorFor(command => command.ProductId);
        _sut.TestValidate(new AddProductToOrderCommand(Guid.NewGuid(), productId, Guid.Empty, pricingVersionId, 1)).ShouldHaveValidationErrorFor(command => command.ProductPricingId);
        _sut.TestValidate(new AddProductToOrderCommand(Guid.NewGuid(), productId, pricingId, Guid.Empty, 1)).ShouldHaveValidationErrorFor(command => command.ProductPricingVersionId);
        _sut.TestValidate(new AddProductToOrderCommand(Guid.NewGuid(), productId, pricingId, pricingVersionId, 0)).ShouldHaveValidationErrorFor(command => command.Quantity);
        _sut.TestValidate(new AddProductToOrderCommand(Guid.NewGuid(), productId, pricingId, pricingVersionId, 5)).ShouldNotHaveAnyValidationErrors();
    }
}

public class CreateOrderCommandValidatorTests
{
    private readonly CreateOrderCommandValidator _sut = new();

    [Fact]
    public void ShouldValidateIdempotencyKey()
    {
        _sut.TestValidate(new CreateOrderCommand(string.Empty)).ShouldHaveValidationErrorFor(command => command.IdempotencyKey);
        _sut.TestValidate(new CreateOrderCommand("short")).ShouldHaveValidationErrorFor(command => command.IdempotencyKey);
        _sut.TestValidate(new CreateOrderCommand(new string('a', 101))).ShouldHaveValidationErrorFor(command => command.IdempotencyKey);
        _sut.TestValidate(new CreateOrderCommand("invalid key!")).ShouldHaveValidationErrorFor(command => command.IdempotencyKey);
        _sut.TestValidate(new CreateOrderCommand("valid-key_123")).ShouldNotHaveValidationErrorFor(command => command.IdempotencyKey);
        _sut.TestValidate(new CreateOrderCommand("valid-key-12")).ShouldNotHaveAnyValidationErrors();
    }
}
