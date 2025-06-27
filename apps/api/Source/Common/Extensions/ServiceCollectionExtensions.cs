using GameGuild.Common.Services;

namespace GameGuild.Common.Extensions;

public static class ServiceCollectionExtensions {
  public static IServiceCollection AddUserModule(this IServiceCollection services) {
    // Register User module services
    services.AddScoped<Modules.User.Services.IUserService, Modules.User.Services.UserService>();

    return services;
  }

  public static IServiceCollection AddTenantModule(this IServiceCollection services) {
    // Register Tenant module services
    services.AddScoped<Modules.Tenant.Services.ITenantService, Modules.Tenant.Services.TenantService>();
    services.AddScoped<Modules.Tenant.Services.ITenantContextService, Modules.Tenant.Services.TenantContextService>();
    services.AddScoped<Modules.Tenant.Services.ITenantDomainService, Modules.Tenant.Services.TenantDomainService>();

    return services;
  }

  public static IServiceCollection AddUserProfileModule(this IServiceCollection services) {
    // Register UserProfile module services
    services.AddScoped<Modules.UserProfile.Services.IUserProfileService, Modules.UserProfile.Services.UserProfileService>();

    return services;
  }

  public static IServiceCollection AddProjectModule(this IServiceCollection services) {
    // Register Project module services
    services.AddScoped<Modules.Project.Services.IProjectService, Modules.Project.Services.ProjectService>();

    return services;
  }

  public static IServiceCollection AddTestModule(this IServiceCollection services) {
    // Register Test module services
    services.AddScoped<Modules.TestingLab.Services.ITestService, Modules.TestingLab.Services.TestService>();

    return services;
  }

  public static IServiceCollection AddProgramModule(this IServiceCollection services) {
    // Register Program module services
    services.AddScoped<Modules.Program.Services.IProgramService, Modules.Program.Services.ProgramService>();
    services.AddScoped<Modules.Program.Interfaces.IProgramContentService, Modules.Program.Services.ProgramContentService>();

    return services;
  }

  public static IServiceCollection AddCommonServices(this IServiceCollection services) {
    // Add logging
    services.AddLogging();

    // Add memory cache
    services.AddMemoryCache();

    // Add permission service for three-layer permission system
    services.AddScoped<IPermissionService, PermissionService>();

    return services;
  }
}
