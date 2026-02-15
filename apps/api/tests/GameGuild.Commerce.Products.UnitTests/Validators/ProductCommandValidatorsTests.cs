using FluentValidation.TestHelper;
using FluentAssertions;
using Xunit;

namespace GameGuild.Commerce.Products.UnitTests.Validators;

public class CreateProductCommandValidatorTests
{
    private readonly CreateProductCommandValidator _validator = new();

    [Fact]
    public void ShouldFail_WhenNameEmpty()
    {
        var cmd = new CreateProductCommand(Name: "");
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void ShouldFail_WhenNameTooShort()
    {
        var cmd = new CreateProductCommand(Name: "A");
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void ShouldFail_WhenNameTooLong()
    {
        var cmd = new CreateProductCommand(Name: new string('A', 201));
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void ShouldFail_WhenDescriptionTooLong()
    {
        var cmd = new CreateProductCommand(Name: "Valid", Description: new string('A', 4001));
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void ShouldFail_WhenReferralCommissionOutOfRange()
    {
        var cmd = new CreateProductCommand(Name: "Valid", ReferralCommissionPercentage: 101);
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.ReferralCommissionPercentage);
    }

    [Fact]
    public void ShouldFail_WhenMaxAffiliateDiscountNegative()
    {
        var cmd = new CreateProductCommand(Name: "Valid", MaxAffiliateDiscount: -1);
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.MaxAffiliateDiscount);
    }

    [Fact]
    public void ShouldFail_WhenBundleWithoutItems()
    {
        var cmd = new CreateProductCommand(Name: "Valid", IsBundle: true, BundleItems: null);
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.BundleItems);
    }

    [Fact]
    public void ShouldPass_WhenValid()
    {
        var cmd = new CreateProductCommand(Name: "My Product");
        _validator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }
}

public class UpdateProductCommandValidatorTests
{
    private readonly UpdateProductCommandValidator _validator = new();

    [Fact]
    public void ShouldFail_WhenProductIdEmpty()
    {
        var cmd = new UpdateProductCommand(ProductId: Guid.Empty);
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.ProductId);
    }

    [Fact]
    public void ShouldFail_WhenNameTooShort()
    {
        var cmd = new UpdateProductCommand(ProductId: Guid.NewGuid(), Name: "A");
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void ShouldPass_WhenOnlyProductIdProvided()
    {
        var cmd = new UpdateProductCommand(ProductId: Guid.NewGuid());
        _validator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }
}

public class DeleteProductCommandValidatorTests
{
    private readonly DeleteProductCommandValidator _validator = new();

    [Fact]
    public void ShouldFail_WhenProductIdEmpty()
    {
        var cmd = new DeleteProductCommand(ProductId: Guid.Empty);
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.ProductId);
    }

    [Fact]
    public void ShouldFail_WhenReasonTooLong()
    {
        var cmd = new DeleteProductCommand(ProductId: Guid.NewGuid(), Reason: new string('R', 501));
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Reason);
    }

    [Fact]
    public void ShouldPass_WhenValid()
    {
        var cmd = new DeleteProductCommand(ProductId: Guid.NewGuid());
        _validator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }
}

public class SetProductPricingCommandValidatorTests
{
    private readonly SetProductPricingCommandValidator _validator = new();

    [Fact]
    public void ShouldFail_WhenProductIdEmpty()
    {
        var cmd = new SetProductPricingCommand(ProductId: Guid.Empty, Name: "Price", BasePrice: 10m);
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.ProductId);
    }

    [Fact]
    public void ShouldFail_WhenNameEmpty()
    {
        var cmd = new SetProductPricingCommand(ProductId: Guid.NewGuid(), Name: "", BasePrice: 10m);
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void ShouldFail_WhenBasePriceNegative()
    {
        var cmd = new SetProductPricingCommand(ProductId: Guid.NewGuid(), Name: "Price", BasePrice: -1m);
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.BasePrice);
    }

    [Fact]
    public void ShouldFail_WhenCurrencyWrongLength()
    {
        var cmd = new SetProductPricingCommand(ProductId: Guid.NewGuid(), Name: "Price", BasePrice: 10m, Currency: "US");
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Currency);
    }

    [Fact]
    public void ShouldFail_WhenSalePriceNegative()
    {
        var cmd = new SetProductPricingCommand(ProductId: Guid.NewGuid(), Name: "Price", BasePrice: 100m, SalePrice: -1m);
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.SalePrice);
    }

    [Fact]
    public void ShouldFail_WhenSalePriceGreaterThanBasePrice()
    {
        var cmd = new SetProductPricingCommand(ProductId: Guid.NewGuid(), Name: "Price", BasePrice: 50m, SalePrice: 60m);
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.SalePrice);
    }

    [Fact]
    public void ShouldPass_WhenValid()
    {
        var cmd = new SetProductPricingCommand(ProductId: Guid.NewGuid(), Name: "Standard", BasePrice: 99.99m);
        _validator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }
}

public class CreatePromoCodeCommandValidatorTests
{
    private readonly CreatePromoCodeCommandValidator _validator = new();

    [Fact]
    public void ShouldFail_WhenCodeEmpty()
    {
        var cmd = new CreatePromoCodeCommand(Code: "", Name: "Test");
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Fact]
    public void ShouldFail_WhenCodeTooShort()
    {
        var cmd = new CreatePromoCodeCommand(Code: "AB", Name: "Test");
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Fact]
    public void ShouldFail_WhenCodeHasInvalidChars()
    {
        var cmd = new CreatePromoCodeCommand(Code: "SAVE 20!", Name: "Test");
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Fact]
    public void ShouldFail_WhenPercentageOff_WithoutDiscount()
    {
        var cmd = new CreatePromoCodeCommand(Code: "SAVE20", Name: "Test", Type: PromoCodeType.PercentageOff, DiscountPercentage: null);
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.DiscountPercentage);
    }

    [Fact]
    public void ShouldFail_WhenPercentageOff_Over100()
    {
        var cmd = new CreatePromoCodeCommand(Code: "SAVE20", Name: "Test", Type: PromoCodeType.PercentageOff, DiscountPercentage: 101m);
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.DiscountPercentage);
    }

    [Fact]
    public void ShouldFail_WhenFixedAmountOff_WithoutAmount()
    {
        var cmd = new CreatePromoCodeCommand(Code: "SAVE20", Name: "Test", Type: PromoCodeType.FixedAmountOff, DiscountAmount: null);
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.DiscountAmount);
    }

    [Fact]
    public void ShouldFail_WhenStackingPriorityNegative()
    {
        var cmd = new CreatePromoCodeCommand(Code: "PROMO", Name: "Test", StackingPriority: -1);
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.StackingPriority);
    }

    [Fact]
    public void ShouldPass_WhenValid()
    {
        var cmd = new CreatePromoCodeCommand(Code: "SAVE20", Name: "Save 20%", Type: PromoCodeType.PercentageOff, DiscountPercentage: 20m);
        _validator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }
}

public class ValidatePromoCodeCommandValidatorTests
{
    private readonly ValidatePromoCodeCommandValidator _validator = new();

    [Fact]
    public void ShouldFail_WhenCodeEmpty()
    {
        var cmd = new ValidatePromoCodeCommand(Code: "", OrderAmount: 10m);
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Fact]
    public void ShouldFail_WhenOrderAmountZero()
    {
        var cmd = new ValidatePromoCodeCommand(Code: "TEST", OrderAmount: 0m);
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.OrderAmount);
    }

    [Fact]
    public void ShouldPass_WhenValid()
    {
        var cmd = new ValidatePromoCodeCommand(Code: "TEST", OrderAmount: 50m);
        _validator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }
}

public class ApplyPromoCodesCommandValidatorTests
{
    private readonly ApplyPromoCodesCommandValidator _validator = new();

    [Fact]
    public void ShouldFail_WhenOrderAmountZero()
    {
        var cmd = new ApplyPromoCodesCommand(OrderAmount: 0m, PromoCodes: new List<string> { "CODE" });
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.OrderAmount);
    }

    [Fact]
    public void ShouldFail_WhenPromoCodesEmpty()
    {
        var cmd = new ApplyPromoCodesCommand(OrderAmount: 50m, PromoCodes: new List<string>());
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.PromoCodes);
    }

    [Fact]
    public void ShouldPass_WhenValid()
    {
        var cmd = new ApplyPromoCodesCommand(OrderAmount: 50m, PromoCodes: new List<string> { "CODE1" });
        _validator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }
}

public class AddOrderItemCommandValidatorTests
{
    private readonly AddOrderItemCommandValidator _validator = new();

    [Fact]
    public void ShouldFail_WhenOrderIdEmpty()
    {
        var cmd = new AddOrderItemCommand(OrderId: Guid.Empty, ProductId: Guid.NewGuid());
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.OrderId);
    }

    [Fact]
    public void ShouldFail_WhenQuantityZero()
    {
        var cmd = new AddOrderItemCommand(Guid.NewGuid(), Guid.NewGuid(), Quantity: 0);
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Quantity);
    }

    [Fact]
    public void ShouldFail_WhenQuantityExceeds100()
    {
        var cmd = new AddOrderItemCommand(Guid.NewGuid(), Guid.NewGuid(), Quantity: 101);
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Quantity);
    }

    [Fact]
    public void ShouldFail_WhenPromoCodeHasInvalidChars()
    {
        var cmd = new AddOrderItemCommand(Guid.NewGuid(), Guid.NewGuid(), PromoCode: "BAD CODE!");
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.PromoCode);
    }

    [Fact]
    public void ShouldPass_WhenValid()
    {
        var cmd = new AddOrderItemCommand(Guid.NewGuid(), Guid.NewGuid(), 2);
        _validator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }
}

public class CompleteOrderCommandValidatorTests
{
    private readonly CompleteOrderCommandValidator _validator = new();

    [Fact]
    public void ShouldFail_WhenOrderIdEmpty()
    {
        var cmd = new CompleteOrderCommand(OrderId: Guid.Empty);
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.OrderId);
    }

    [Fact]
    public void ShouldFail_WhenPaymentReferenceTooLong()
    {
        var cmd = new CompleteOrderCommand(Guid.NewGuid(), PaymentProviderReference: new string('X', 201));
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.PaymentProviderReference);
    }

    [Fact]
    public void ShouldPass_WhenValid()
    {
        var cmd = new CompleteOrderCommand(Guid.NewGuid());
        _validator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }
}

public class GrantProductAccessCommandValidatorTests
{
    private readonly GrantProductAccessCommandValidator _validator = new();

    [Fact]
    public void ShouldFail_WhenUserIdEmpty()
    {
        var cmd = new GrantProductAccessCommand(UserId: Guid.Empty, ProductId: Guid.NewGuid());
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void ShouldFail_WhenProductIdEmpty()
    {
        var cmd = new GrantProductAccessCommand(UserId: Guid.NewGuid(), ProductId: Guid.Empty);
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.ProductId);
    }

    [Fact]
    public void ShouldFail_WhenPricePaidNegative()
    {
        var cmd = new GrantProductAccessCommand(Guid.NewGuid(), Guid.NewGuid(), PricePaid: -1m);
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.PricePaid);
    }

    [Fact]
    public void ShouldFail_WhenCurrencyWrongLength()
    {
        var cmd = new GrantProductAccessCommand(Guid.NewGuid(), Guid.NewGuid(), Currency: "US");
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Currency);
    }

    [Fact]
    public void ShouldPass_WhenValid()
    {
        var cmd = new GrantProductAccessCommand(Guid.NewGuid(), Guid.NewGuid());
        _validator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }
}

public class RevokeProductAccessCommandValidatorTests
{
    private readonly RevokeProductAccessCommandValidator _validator = new();

    [Fact]
    public void ShouldFail_WhenUserIdEmpty()
    {
        var cmd = new RevokeProductAccessCommand(UserId: Guid.Empty, ProductId: Guid.NewGuid());
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void ShouldFail_WhenProductIdEmpty()
    {
        var cmd = new RevokeProductAccessCommand(UserId: Guid.NewGuid(), ProductId: Guid.Empty);
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.ProductId);
    }

    [Fact]
    public void ShouldPass_WhenValid()
    {
        var cmd = new RevokeProductAccessCommand(Guid.NewGuid(), Guid.NewGuid());
        _validator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }
}

public class DeletePromoCodeCommandValidatorTests
{
    private readonly DeletePromoCodeCommandValidator _validator = new();

    [Fact]
    public void ShouldFail_WhenIdEmpty()
    {
        var cmd = new DeletePromoCodeCommand(Id: Guid.Empty);
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void ShouldPass_WhenValid()
    {
        var cmd = new DeletePromoCodeCommand(Id: Guid.NewGuid());
        _validator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }
}
