using GameGuild.Common.Authorization;
using GameGuild.Modules.Permissions;
using HotChocolate.Execution.Configuration;


namespace GameGuild.Common.Extensions;

/// <summary>
/// Extension methods for configuring 3-layer DAC authorization in HotChocolate GraphQL
/// </summary>
public static class GraphQLDACAuthorizationExtensions {
  /// <summary>
  /// Adds 3-layer DAC (Discretionary Access Control) authorization to HotChocolate GraphQL server
  /// </summary>
  /// <param name="builder">The GraphQL request executor builder</param>
  /// <returns>The configured builder for method chaining</returns>
  public static IRequestExecutorBuilder AddDACAuthorization(this IRequestExecutorBuilder builder) {
    return builder
           // Add DAC directive type
           .AddDirectiveType<DACAuthorizeDirectiveType>()

           // Add DAC authorization middleware
           .UseField<DACAuthorizationMiddleware>()

           // Add enum types for authorization
           .AddType<EnumType<DACPermissionLevel>>()
           .AddType<EnumType<PermissionType>>();
  }

  /// <summary>
  /// Registers DAC authorization services in the dependency injection container
  /// </summary>
  /// <param name="services">The service collection</param>
  /// <returns>The service collection for method chaining</returns>
  public static IServiceCollection AddDACAuthorizationServices(this IServiceCollection services) {
    return services
      // Add HTTP context accessor for accessing user context
      .AddHttpContextAccessor();
  }
}
