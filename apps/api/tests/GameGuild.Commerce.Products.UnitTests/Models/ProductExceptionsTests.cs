using FluentAssertions;
using Xunit;

namespace GameGuild.Commerce.Products.UnitTests.Models;

public class ProductExceptionsTests
{
    [Fact]
    public void ProductNotFound_ShouldSetProductId()
    {
        var id = Guid.NewGuid();
        var ex = new ProductNotFoundException(id);

        ex.ProductId.Should().Be(id);
        ex.Message.Should().Contain(id.ToString());
    }

    [Fact]
    public void ProductNotFound_WithMessage_ShouldSetCustomMessage()
    {
        var id = Guid.NewGuid();
        var ex = new ProductNotFoundException(id, "custom");

        ex.Message.Should().Be("custom");
        ex.ProductId.Should().Be(id);
    }

    [Fact]
    public void ConcurrencyException_ShouldSetMessage()
    {
        var ex = new ConcurrencyException("conflict");
        ex.Message.Should().Be("conflict");
    }

    [Fact]
    public void ConcurrencyException_WithInner_ShouldWrap()
    {
        var inner = new Exception("inner");
        var ex = new ConcurrencyException("conflict", inner);
        ex.InnerException.Should().Be(inner);
    }

    [Fact]
    public void PromoCodeNotFound_ByCode_ShouldSetCode()
    {
        var ex = new PromoCodeNotFoundException("SAVE20");
        ex.Code.Should().Be("SAVE20");
        ex.Message.Should().Contain("SAVE20");
    }

    [Fact]
    public void PromoCodeNotFound_ById_ShouldSetId()
    {
        var id = Guid.NewGuid();
        var ex = new PromoCodeNotFoundException(id);
        ex.PromoCodeId.Should().Be(id);
    }

    [Fact]
    public void InvalidPromoCode_ShouldSetCodeAndReason()
    {
        var ex = new InvalidPromoCodeException("EXPIRED", "Code has expired");
        ex.Code.Should().Be("EXPIRED");
        ex.Reason.Should().Be("Code has expired");
        ex.Message.Should().Contain("EXPIRED").And.Contain("Code has expired");
    }
}
