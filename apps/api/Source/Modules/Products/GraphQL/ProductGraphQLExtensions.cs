using GameGuild.Modules.Programs;
using GameGuild.Modules.Products.GraphQL;
using HotChocolate.Execution.Configuration;


using ProductEntity = GameGuild.Modules.Products.Models.Product;
namespace GameGuild.Modules.Products.GraphQL;

/// <summary> Extension methods for configuring Product GraphQL integration </summary>
public static class ProductGraphQlExtensions {
  /// <summary> Configure GraphQL server with DAC authorization for Product entity </summary>
  public static IRequestExecutorBuilder AddProductGraphQl(this IRequestExecutorBuilder builder) {
    return builder
      .AddType<ProductType>()
      .AddType<ProductPricingType>()
      .AddType<UserProductType>()
      .AddType<PromoCodeType>()
      .AddType<ProductProgramType>()
      .AddType<ProgramType>()
      .AddTypeExtension<ProductQueries>()
      .AddTypeExtension<ProductMutations>();
  }
}
