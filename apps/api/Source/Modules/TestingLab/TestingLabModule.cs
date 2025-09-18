namespace GameGuild.Modules.TestingLab;

/// <summary> Extension methods for registering TestingLab module services </summary>
public static class TestingLabModule {
  /// <summary> Registers all TestingLab module services and repositories </summary>
  public static IServiceCollection AddTestingLabModule(this IServiceCollection services) {
    // Register TestingLab repositories
    services.AddScoped<ITestingRequestRepository, TestingRequestRepository>();
    services.AddScoped<ITestingLocationRepository, TestingLocationRepository>();

    // Register TestingLab services  
    services.AddScoped<ITestingRequestService, TestingRequestService>();
    services.AddScoped<ITestingSessionService, TestingSessionService>();

    // CQRS handlers are automatically registered by assembly scanning

    return services;
  }
}
