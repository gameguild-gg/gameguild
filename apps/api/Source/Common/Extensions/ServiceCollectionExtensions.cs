using GameGuild.Modules.Payments.Services;
using GameGuild.Modules.Payments.Handlers;
using GameGuild.Modules.Posts;
using GameGuild.Modules.Products.Services;
using GameGuild.Modules.Products.Handlers;
using GameGuild.Modules.Programs.Interfaces;
using GameGuild.Modules.Programs.Services;
using GameGuild.Modules.Projects;
using GameGuild.Modules.Subscriptions.Services;
using GameGuild.Modules.Tenants;
using GameGuild.Modules.TestingLab;
using GameGuild.Modules.UserProfiles;
using GameGuild.Modules.Users;
using GameGuild.Modules.Credentials;
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
    services.AddScoped<ITestService, TestService>();

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
    
    // Register Product CQRS handlers
    services.AddScoped<ProductCommandHandlers>();
    services.AddScoped<ProductQueryHandlers>();

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
    
    // Register Payment CQRS command handlers
    services.AddScoped<CreatePaymentCommandHandler>();
    services.AddScoped<ProcessPaymentCommandHandler>();
    services.AddScoped<RefundPaymentCommandHandler>();
    
    // Register Payment CQRS query handlers  
    services.AddScoped<GetPaymentByIdQueryHandler>();
    services.AddScoped<GetUserPaymentsQueryHandler>();
    services.AddScoped<GetProductPaymentsQueryHandler>();
    services.AddScoped<GetPaymentStatsQueryHandler>();
    services.AddScoped<GetRevenueReportQueryHandler>();

    return services;
  }

  public static IServiceCollection AddCredentialsModule(this IServiceCollection services) {
    // Register Credentials module services
    services.AddScoped<ICredentialService, CredentialService>();

    return services;
  }

  public static IServiceCollection AddPostsModule(this IServiceCollection services) {
    // Register Posts module services
    services.AddScoped<IPostAnnouncementService, PostAnnouncementService>();
    services.AddScoped<IPostService, PostService>();
    
    // Register GraphQL DataLoaders for efficient data loading
    services.AddScoped<IUserDataLoader, UserDataLoader>();
    services.AddScoped<IPostContentReferenceDataLoader, PostContentReferenceDataLoader>();
    services.AddScoped<IPostCommentDataLoader, PostCommentDataLoader>();
    services.AddScoped<IPostLikeDataLoader, PostLikeDataLoader>();
    
    // Domain event handlers are automatically registered by MediatR
    return services;
  }

  public static IServiceCollection AddPostModule(this IServiceCollection services) {
    // Register Post module services
    services.AddScoped<IPostService, PostService>();

    return services;
  }
}
