using GameGuild.Modules.Payments.Services;
using GameGuild.Modules.Products.Services;
using GameGuild.Modules.Programs.Interfaces;
using GameGuild.Modules.Programs.Services;
using GameGuild.Modules.Projects.Services;
using GameGuild.Modules.Subscriptions.Services;
using GameGuild.Modules.Tenants;
using GameGuild.Modules.Tenants.Entities;
using GameGuild.Modules.UserProfiles.Services;
using GameGuild.Modules.Users;
using IProgramService = GameGuild.Modules.Programs.Services.IProgramService;


namespace GameGuild.Common;

public static class ServiceCollectionExtensions {
  public static IServiceCollection AddUserModule(this IServiceCollection services) {
    // Register User module services
    services.AddScoped<IUserService, UserService>();

    return services;
  }

  public static IServiceCollection AddTenantModule(this IServiceCollection services) {
    // Register Tenant module services
    services.AddScoped<ITenantService, TenantService>();
    services.AddScoped<ITenantContextService, TenantContextService>();
    services.AddScoped<ITenantDomainService, TenantDomainService>();

    return services;
  }

  public static IServiceCollection AddUserProfileModule(this IServiceCollection services) {
    // Register UserProfile module services
    services
      .AddScoped<IUserProfileService, UserProfileService>();

    return services;
  }

  public static IServiceCollection AddProjectModule(this IServiceCollection services) {
    // Register Project module services
    services.AddScoped<IProjectService, ProjectService>();

    return services;
  }

  public static IServiceCollection AddTestModule(this IServiceCollection services) {
    // Register Test module services
    services.AddScoped<Modules.TestingLab.Services.ITestService, Modules.TestingLab.Services.TestService>();

    return services;
  }

  public static IServiceCollection AddProgramModule(this IServiceCollection services) {
    // Register Program module services
    services.AddScoped<IProgramService, ProgramService>();
    services
      .AddScoped<IProgramContentService, ProgramContentService>();
    services
      .AddScoped<IContentInteractionService, ContentInteractionService>();
    services
      .AddScoped<IActivityGradeService, ActivityGradeService>();

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

  public static IServiceCollection AddProductModule(this IServiceCollection services) {
    // Register Product module services
    services.AddScoped<IProductService, ProductService>();

    return services;
  }

  public static IServiceCollection AddSubscriptionModule(this IServiceCollection services) {
    // Register Subscription module services
    services.AddScoped<ISubscriptionService, SubscriptionService>();

    return services;
  }

  public static IServiceCollection AddPaymentModule(this IServiceCollection services) {
    // Register Payment module services
    services.AddScoped<IPaymentService, PaymentService>();

    return services;
  }
}
