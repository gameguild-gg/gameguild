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
        _sut.TestValidate(new AddProductToOrderCommand(Guid.Empty, Guid.NewGuid(), 1)).ShouldHaveValidationErrorFor(command => command.OrderId);
        _sut.TestValidate(new AddProductToOrderCommand(Guid.NewGuid(), Guid.Empty, 1)).ShouldHaveValidationErrorFor(command => command.ProductId);
        _sut.TestValidate(new AddProductToOrderCommand(Guid.NewGuid(), Guid.NewGuid(), 0)).ShouldHaveValidationErrorFor(command => command.Quantity);
        _sut.TestValidate(new AddProductToOrderCommand(Guid.NewGuid(), Guid.NewGuid(), 5)).ShouldNotHaveAnyValidationErrors();
    }
}

public class CreateOrderCommandValidatorTests
{
    private readonly CreateOrderCommandValidator _sut = new();

    [Fact]
    public void ShouldValidateUserIdempotencyAndCurrency()
    {
        _sut.TestValidate(new CreateOrderCommand(Guid.Empty, "valid-key-12", "USD")).ShouldHaveValidationErrorFor(command => command.UserId);
        _sut.TestValidate(new CreateOrderCommand(Guid.NewGuid(), string.Empty, "USD")).ShouldHaveValidationErrorFor(command => command.IdempotencyKey);
        _sut.TestValidate(new CreateOrderCommand(Guid.NewGuid(), "short", "USD")).ShouldHaveValidationErrorFor(command => command.IdempotencyKey);
        _sut.TestValidate(new CreateOrderCommand(Guid.NewGuid(), new string('a', 101), "USD")).ShouldHaveValidationErrorFor(command => command.IdempotencyKey);
        _sut.TestValidate(new CreateOrderCommand(Guid.NewGuid(), "invalid key!", "USD")).ShouldHaveValidationErrorFor(command => command.IdempotencyKey);
        _sut.TestValidate(new CreateOrderCommand(Guid.NewGuid(), "valid-key_123", "USD")).ShouldNotHaveValidationErrorFor(command => command.IdempotencyKey);
        _sut.TestValidate(new CreateOrderCommand(Guid.NewGuid(), "valid-key-12", string.Empty)).ShouldHaveValidationErrorFor(command => command.Currency);
        _sut.TestValidate(new CreateOrderCommand(Guid.NewGuid(), "valid-key-12", "usd")).ShouldHaveValidationErrorFor(command => command.Currency);
        _sut.TestValidate(new CreateOrderCommand(Guid.NewGuid(), "valid-key-12", "US")).ShouldHaveValidationErrorFor(command => command.Currency);
        _sut.TestValidate(new CreateOrderCommand(Guid.NewGuid(), "valid-key-12", "USD")).ShouldNotHaveAnyValidationErrors();
    }
}