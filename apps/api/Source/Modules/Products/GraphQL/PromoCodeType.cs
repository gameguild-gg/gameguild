using GameGuild.Database;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Products;

/// <summary>
/// GraphQL type for PromoCode entity
/// </summary>
public class PromoCodeType : ObjectType<PromoCode> {
  protected override void Configure(IObjectTypeDescriptor<PromoCode> descriptor) {
    descriptor.Name("PromoCode");
    descriptor.Description("Represents a promotional code for products");

    descriptor.Field(pc => pc.Id).Type<NonNullType<UuidType>>().Description("The unique identifier for the promo code");

    descriptor.Field(pc => pc.Code).Type<NonNullType<StringType>>().Description("The promotional code");

    descriptor.Field(pc => pc.Type)
              .Type<NonNullType<EnumType<PromoCodeType>>>()
              .Description("The type of discount (percentage or fixed amount)");

    descriptor.Field(pc => pc.DiscountPercentage)
              .Type<DecimalType>()
              .Description("The discount percentage (for percentage-based discounts)");

    descriptor.Field(pc => pc.DiscountAmount)
              .Type<DecimalType>()
              .Description("The discount amount (for fixed amount discounts)");

    descriptor.Field(pc => pc.ValidFrom).Type<DateTimeType>().Description("When the promo code becomes valid");

    descriptor.Field(pc => pc.ValidUntil).Type<DateTimeType>().Description("When the promo code expires");

    descriptor.Field(pc => pc.MaxUses).Type<IntType>().Description("Maximum number of times this code can be used");

    descriptor.Field("currentUsageCount")
              .Type<NonNullType<IntType>>()
              .Description("Current number of times this code has been used")
              .Resolve(async context => {
                  var promoCode = context.Parent<PromoCode>();
                  var dbContext = context.Service<ApplicationDbContext>();

                  return await dbContext.PromoCodeUses.Where(pcu => !pcu.IsDeleted && pcu.PromoCodeId == promoCode.Id)
                                        .CountAsync();
                }
              );

    descriptor.Field("isValid")
              .Type<NonNullType<BooleanType>>()
              .Description("Indicates if the promo code is currently valid")
              .Resolve(async context => {
                  var promoCode = context.Parent<PromoCode>();
                  var productService = context.Service<Services.IProductService>();

                  return await productService.IsPromoCodeValidAsync(promoCode.Code);
                }
              );
  }
}
