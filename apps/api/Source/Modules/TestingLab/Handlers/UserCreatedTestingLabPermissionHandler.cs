using GameGuild.Common;
using GameGuild.Common.Services;
using GameGuild.Database;
using GameGuild.Modules.Users;


namespace GameGuild.Modules.TestingLab;

/// <summary> Handles UserCreatedEvent to grant basic TestingLab permissions to new users </summary>
internal class UserCreatedTestingLabPermissionHandler : IDomainEventHandler<UserCreatedEvent> {
  private readonly IConfiguration _configuration;

  private readonly ApplicationDbContext _context;

  private readonly ILogger<UserCreatedTestingLabPermissionHandler> _logger;

  private readonly IServiceProvider _serviceProvider;

  public UserCreatedTestingLabPermissionHandler(
    ILogger<UserCreatedTestingLabPermissionHandler> logger,
    ApplicationDbContext context,
    IServiceProvider serviceProvider,
    IConfiguration configuration
  ) {
    _logger = logger;
    _context = context;
    _serviceProvider = serviceProvider;
    _configuration = configuration;
  }

  public async Task Handle(UserCreatedEvent domainEvent, CancellationToken cancellationToken) {
    ArgumentNullException.ThrowIfNull(domainEvent);

    _logger.LogInformation(
      "Setting up basic TestingLab permissions for new user {UserId} ({UserName})",
      domainEvent.UserId,
      domainEvent.Name
    );

    try {
      // Query the user's tenant associations since UserCreatedEvent doesn't include tenant context
      var userTenants = await _context.TenantPermissions
                                      .Where(tp => tp.UserId == domainEvent.UserId &&
                                                   tp.DeletedAt == null &&
                                                   (tp.ExpiresAt == null || tp.ExpiresAt > DateTime.UtcNow)
                                      )
                                      .Select(tp => tp.TenantId)
                                      .Where(tenantId => tenantId.HasValue)
                                      .ToListAsync(cancellationToken);

      if (userTenants.Count == 0) {
        _logger.LogWarning(
          "No tenant associations found for user {UserId}. Basic TestingLab permissions cannot be granted without tenant context.",
          domainEvent.UserId
        );

        return;
      }

      // Grant basic permissions for each tenant the user belongs to
      foreach (var tenantId in userTenants.Where(t => t.HasValue).Select(t => t!.Value)) { await GrantBasicTestingLabPermissions(domainEvent.UserId, tenantId, cancellationToken); }

      _logger.LogInformation(
        "Successfully set up basic TestingLab permissions for user {UserId} in {TenantCount} tenants",
        domainEvent.UserId,
        userTenants.Count
      );
    }
    catch (Exception ex) {
      _logger.LogError(
        ex,
        "Failed to set up TestingLab permissions for user {UserId}",
        domainEvent.UserId
      );

      // Don't rethrow - permission setup failures shouldn't fail user creation
    }
  }

  private async Task GrantBasicTestingLabPermissions(Guid userId, Guid tenantId, CancellationToken cancellationToken) {
    _logger.LogDebug("Granting basic TestingLab permissions to user {UserId} in tenant {TenantId}", userId, tenantId);

    try {
      // Get the default role from configuration, fallback to "TestingLabTester"
      var defaultRoleName = _configuration["TestingLab:DefaultUserRole"] ?? "TestingLabTester";

      // Use the simple permission service to assign the configurable default role
      var permissionService = _serviceProvider.GetRequiredService<ISimplePermissionService>();

      await permissionService.AssignRoleToUserAsync(
        userId,
        tenantId,
        defaultRoleName,
        null
      );

      _logger.LogInformation(
        "Successfully assigned {RoleName} role to user {UserId} in tenant {TenantId}",
        defaultRoleName,
        userId,
        tenantId
      );
    }
    catch (Exception ex) {
      _logger.LogError(
        ex,
        "Failed to assign default TestingLab role to user {UserId} in tenant {TenantId}",
        userId,
        tenantId
      );
      // Don't rethrow - permission failures shouldn't fail user creation
    }
  }
}
