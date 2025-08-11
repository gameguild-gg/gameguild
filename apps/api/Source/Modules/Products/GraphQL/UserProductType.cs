using GameGuild.Common;
using GameGuild.Modules.Users;


namespace GameGuild.Modules.Products;

/// <summary>
/// GraphQL type for UserProduct entity
/// </summary>
public class UserProductType : ObjectType<UserProduct> {
  protected override void Configure(IObjectTypeDescriptor<UserProduct> descriptor) {
    descriptor.Name("UserProduct");
    descriptor.Description("Represents a user's access to a product");

    descriptor.Field(up => up.Id)
              .Type<NonNullType<UuidType>>()
              .Description("The unique identifier for the user product record");

    descriptor.Field(up => up.AcquisitionType)
              .Type<NonNullType<EnumType<ProductAcquisitionType>>>()
              .Description("How the user acquired access to this product");

    descriptor.Field(up => up.AccessStatus)
              .Type<NonNullType<EnumType<ProductAccessStatus>>>()
              .Description("The current access status");

    descriptor.Field(up => up.PricePaid)
              .Type<NonNullType<DecimalType>>()
              .Description("The price paid for this product");

    descriptor.Field(up => up.Currency).Type<NonNullType<StringType>>().Description("The currency used for payment");

    descriptor.Field(up => up.AccessEndDate)
              .Type<DateTimeType>()
              .Description("When the access expires (null for permanent access)");

    descriptor.Field(up => up.User)
              .Type<ObjectType<User>>()
              .Description("The user who has access");

    descriptor.Field(up => up.Product).Type<ProductType>().Description("The product being accessed");
  }
}
