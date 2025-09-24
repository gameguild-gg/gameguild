using GameGuild.Modules.Programs;

namespace GameGuild.Modules.Products;

/// <summary>
/// GraphQL type definition for ProductProgram junction entity
/// </summary>
public class ProductProgramType : ObjectType<ProductProgram> {
    protected override void Configure(IObjectTypeDescriptor<ProductProgram> descriptor) {
        descriptor.Description("Represents a program included in a product");

        // Base entity fields
        descriptor.Field(pp => pp.Id)
                  .Type<NonNullType<UuidType>>()
                  .Description("The unique identifier for this product-program relationship");

        descriptor.Field(pp => pp.SortOrder)
                  .Type<NonNullType<IntType>>()
                  .Description("The display order of this program within the product");

        // Navigation properties
        descriptor.Field(pp => pp.Product)
                  .Type<ProductType>()
                  .Description("The product that contains this program");

        descriptor.Field(pp => pp.Program)
                  .Type<ProgramType>()
                  .Description("The program included in the product");

        descriptor.Field(pp => pp.CreatedAt)
                  .Type<NonNullType<DateTimeType>>()
                  .Description("When this program was added to the product");
    }
}
