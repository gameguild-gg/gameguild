using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace GameGuild.Commerce.Orders.UnitTests.Validators;

public class CancelOrderCommandValidatorTests
{
    private readonly CancelOrderCommandValidator _sut = new();

    [Fact]
    public void ShouldHaveError_WhenOrderIdIsEmpty()
    {
        var command = new CancelOrderCommand(Guid.Empty);
        var result = _sut.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.OrderId);
    }

    [Fact]
    public void ShouldNotHaveError_WhenOrderIdIsValid()
    {
        var command = new CancelOrderCommand(Guid.NewGuid());
        var result = _sut.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.OrderId);
    }

    [Fact]
    public void ShouldHaveError_WhenReasonExceedsMaxLength()
    {
        var command = new CancelOrderCommand(Guid.NewGuid(), new string('a', 501));
        var result = _sut.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Reason);
    }

    [Fact]
    public void ShouldNotHaveError_WhenReasonIsNull()
    {
        var command = new CancelOrderCommand(Guid.NewGuid(), null);
        var result = _sut.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Reason);
    }

    [Fact]
    public void ShouldNotHaveError_WhenReasonIsAtMaxLength()
    {
        var command = new CancelOrderCommand(Guid.NewGuid(), new string('a', 500));
        var result = _sut.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Reason);
    }
}

public class DeleteOrderCommandValidatorTests
{
    private readonly DeleteOrderCommandValidator _sut = new();

    [Fact]
    public void ShouldHaveError_WhenOrderIdIsEmpty()
    {
        var command = new DeleteOrderCommand(Guid.Empty);
        var result = _sut.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.OrderId);
    }

    [Fact]
    public void ShouldNotHaveError_WhenOrderIdIsValid()
    {
        var command = new DeleteOrderCommand(Guid.NewGuid());
        var result = _sut.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.OrderId);
    }
}

public class CompleteOrderCommandValidatorTests
{
    private readonly CompleteOrderCommandValidator _sut = new();

    [Fact]
    public void ShouldHaveError_WhenOrderIdIsEmpty()
    {
        var command = new CompleteOrderCommand(Guid.Empty);
        var result = _sut.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.OrderId);
    }

    [Fact]
    public void ShouldNotHaveError_WhenOrderIdIsValid()
    {
        var command = new CompleteOrderCommand(Guid.NewGuid());
        var result = _sut.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.OrderId);
    }
}

public class UpdateOrderCommandValidatorTests
{
    private readonly UpdateOrderCommandValidator _sut = new();

    [Fact]
    public void ShouldHaveError_WhenOrderIdIsEmpty()
    {
        var command = new UpdateOrderCommand(Guid.Empty);
        var result = _sut.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.OrderId);
    }

    [Fact]
    public void ShouldNotHaveError_WhenCurrencyIsNull()
    {
        var command = new UpdateOrderCommand(Guid.NewGuid(), Currency: null);
        var result = _sut.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Currency);
    }

    [Fact]
    public void ShouldHaveError_WhenCurrencyIsNot3Chars()
    {
        var command = new UpdateOrderCommand(Guid.NewGuid(), Currency: "US");
        var result = _sut.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Currency);
    }

    [Fact]
    public void ShouldNotHaveError_WhenCurrencyIs3Chars()
    {
        var command = new UpdateOrderCommand(Guid.NewGuid(), Currency: "USD");
        var result = _sut.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Currency);
    }
}

public class RefundOrderCommandValidatorTests
{
    private readonly RefundOrderCommandValidator _sut = new();

    [Fact]
    public void ShouldHaveError_WhenOrderIdIsEmpty()
    {
        var command = new RefundOrderCommand(Guid.Empty);
        var result = _sut.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.OrderId);
    }

    [Fact]
    public void ShouldHaveError_WhenAmountIsZero()
    {
        var command = new RefundOrderCommand(Guid.NewGuid(), Amount: 0);
        var result = _sut.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void ShouldHaveError_WhenAmountIsNegative()
    {
        var command = new RefundOrderCommand(Guid.NewGuid(), Amount: -10);
        var result = _sut.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void ShouldNotHaveError_WhenAmountIsNull()
    {
        var command = new RefundOrderCommand(Guid.NewGuid(), Amount: null);
        var result = _sut.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void ShouldNotHaveError_WhenAmountIsPositive()
    {
        var command = new RefundOrderCommand(Guid.NewGuid(), Amount: 50m);
        var result = _sut.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Amount);
    }
}

public class AddProductToOrderCommandValidatorTests
{
    private readonly AddProductToOrderCommandValidator _sut = new();

    [Fact]
    public void ShouldHaveError_WhenOrderIdIsEmpty()
    {
        var command = new AddProductToOrderCommand(Guid.Empty, Guid.NewGuid(), 1);
        var result = _sut.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.OrderId);
    }

    [Fact]
    public void ShouldHaveError_WhenProductIdIsEmpty()
    {
        var command = new AddProductToOrderCommand(Guid.NewGuid(), Guid.Empty, 1);
        var result = _sut.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ProductId);
    }

    [Fact]
    public void ShouldHaveError_WhenQuantityIsZero()
    {
        var command = new AddProductToOrderCommand(Guid.NewGuid(), Guid.NewGuid(), 0);
        var result = _sut.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Quantity);
    }

    [Fact]
    public void ShouldNotHaveError_WhenAllValid()
    {
        var command = new AddProductToOrderCommand(Guid.NewGuid(), Guid.NewGuid(), 5);
        var result = _sut.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}

public class CreateOrderCommandValidatorTests
{
    private readonly CreateOrderCommandValidator _sut = new();

    [Fact]
    public void ShouldHaveError_WhenUserIdIsEmpty()
    {
        var command = new CreateOrderCommand(Guid.Empty, "valid-key-12", "USD");
        var result = _sut.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void ShouldHaveError_WhenIdempotencyKeyIsEmpty()
    {
        var command = new CreateOrderCommand(Guid.NewGuid(), "", "USD");
        var result = _sut.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.IdempotencyKey);
    }

    [Fact]
    public void ShouldHaveError_WhenIdempotencyKeyTooShort()
    {
        var command = new CreateOrderCommand(Guid.NewGuid(), "short", "USD");
        var result = _sut.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.IdempotencyKey);
    }

    [Fact]
    public void ShouldHaveError_WhenIdempotencyKeyTooLong()
    {
        var command = new CreateOrderCommand(Guid.NewGuid(), new string('a', 101), "USD");
        var result = _sut.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.IdempotencyKey);
    }

    [Fact]
    public void ShouldHaveError_WhenIdempotencyKeyHasInvalidChars()
    {
        var command = new CreateOrderCommand(Guid.NewGuid(), "invalid key!", "USD");
        var result = _sut.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.IdempotencyKey);
    }

    [Fact]
    public void ShouldNotHaveError_WhenIdempotencyKeyIsValid()
    {
        var command = new CreateOrderCommand(Guid.NewGuid(), "valid-key_123", "USD");
        var result = _sut.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.IdempotencyKey);
    }

    [Fact]
    public void ShouldHaveError_WhenCurrencyIsEmpty()
    {
        var command = new CreateOrderCommand(Guid.NewGuid(), "valid-key-12", "");
        var result = _sut.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Currency);
    }

    [Fact]
    public void ShouldHaveError_WhenCurrencyIsLowercase()
    {
        var command = new CreateOrderCommand(Guid.NewGuid(), "valid-key-12", "usd");
        var result = _sut.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Currency);
    }

    [Fact]
    public void ShouldHaveError_WhenCurrencyIsNot3Chars()
    {
        var command = new CreateOrderCommand(Guid.NewGuid(), "valid-key-12", "US");
        var result = _sut.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Currency);
    }

    [Fact]
    public void ShouldNotHaveError_WhenAllValid()
    {
        var command = new CreateOrderCommand(Guid.NewGuid(), "valid-key-12", "USD");
        var result = _sut.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
