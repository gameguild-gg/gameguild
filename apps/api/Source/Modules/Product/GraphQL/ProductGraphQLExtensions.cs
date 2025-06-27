using HotChocolate.Execution.Configuration;

namespace GameGuild.Modules.Product.GraphQL;

/// <summary>
/// Extension methods for configuring Product GraphQL integration
/// </summary>
public static class ProductGraphQlExtensions {
  /// <summary>
  /// Configure GraphQL server with DAC authorization for Product entity
  /// </summary>
  public static IRequestExecutorBuilder AddProductGraphQl(this IRequestExecutorBuilder builder) {
    return builder.AddType<ProductType>().AddType<ProductPricingType>().AddType<UserProductType>().AddType<PromoCodeType>().AddTypeExtension<ProductQueries>().AddTypeExtension<ProductMutations>();
  }
}
