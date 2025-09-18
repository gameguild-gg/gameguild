using Microsoft.Extensions.DependencyInjection;


namespace GameGuild.Modules.Products;

/// <summary> Extension methods for registering Products module services </summary>
public static class ProductsModule {
    /// <summary> Adds Products module services to the service collection </summary>
    /// <param name="services"> The service collection </param>
    /// <returns> The service collection for chaining </returns>
    public static IServiceCollection AddProductsModule(this IServiceCollection services) {
        // Register core product services
        services.AddScoped<IProductService, ProductService>();

        return services;
    }
}