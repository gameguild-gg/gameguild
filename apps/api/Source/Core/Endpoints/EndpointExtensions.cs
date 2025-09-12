using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;


namespace GameGuild;

internal static class EndpointExtensions {
  public static IServiceCollection AddEndpoints(this IServiceCollection services, Assembly assembly) {
    ArgumentNullException.ThrowIfNull(assembly);

    var serviceDescriptors = assembly.DefinedTypes.Where(type => type is { IsAbstract: false, IsInterface: false } && type.IsAssignableTo(typeof(IEndpoint))).Select(type => ServiceDescriptor.Transient(typeof(IEndpoint), type)).ToArray();

    services.TryAddEnumerable(serviceDescriptors);

    return services;
  }

  public static IApplicationBuilder MapEndpoints(this WebApplication app, RouteGroupBuilder? routeGroupBuilder) {
    ArgumentNullException.ThrowIfNull(app);

    var endpoints = app.Services.GetRequiredService<IEnumerable<IEndpoint>>();

    IEndpointRouteBuilder builder = routeGroupBuilder is null ? app : routeGroupBuilder;

    foreach (var endpoint in endpoints) endpoint.MapEndpoint(builder);

    return app;
  }

  public static RouteHandlerBuilder HasPermission(this RouteHandlerBuilder app, string permission) { return app.RequireAuthorization(permission); }
}
