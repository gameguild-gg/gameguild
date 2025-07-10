using GameGuild.Modules.Products.Models;


namespace GameGuild.Modules.Products.GraphQL;

/// <summary>
/// GraphQL type for ProductPricing entity
/// </summary>
public class ProductPricingType : ObjectType<ProductPricing> {
  protected override void Configure(IObjectTypeDescriptor<ProductPricing> descriptor) {
    descriptor.Name("ProductPricing");
    descriptor.Description("Represents pricing information for a product");

    descriptor.Field(pp => pp.Id).Type<NonNullType<UuidType>>().Description("The unique identifier for the pricing");

    descriptor.Field(pp => pp.Name).Type<NonNullType<StringType>>().Description("The name of this pricing tier");

    descriptor.Field(pp => pp.BasePrice)
              .Type<NonNullType<DecimalType>>()
              .Description("The base price for this product");

    descriptor.Field(pp => pp.Currency)
              .Type<NonNullType<StringType>>()
              .Description("The currency code (e.g., USD, EUR)");

    descriptor.Field(pp => pp.IsDefault)
              .Type<NonNullType<BooleanType>>()
              .Description("Indicates if this is the default pricing");

    descriptor.Field(pp => pp.CreatedAt).Type<NonNullType<DateTimeType>>().Description("When this pricing was created");
  }
}
