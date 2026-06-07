using GameGuild.CQRS;
using GameGuild.Identity.Users;


namespace GameGuild.TestingLab;

/// <summary> Handles UserCreatedEvent to grant basic TestingLab permissions to new users </summary>
internal class UserCreatedTestingLabPermissionHandler : IDomainEventHandler<UserCreatedEvent> {
  private readonly IConfiguration _configuration;

  private readonly IApplicationDbContext _context;

  private readonly ILogger<UserCreatedTestingLabPermissionHandler> _logger;

  public UserCreatedTestingLabPermissionHandler(ILogger<UserCreatedTestingLabPermissionHandler> logger, IApplicationDbContext context, IConfiguration configuration) {
    _logger = logger;
    _context = context;
    _configuration = configuration;
  }

  public async Task Handle(UserCreatedEvent domainEvent, CancellationToken cancellationToken) {
    ArgumentNullException.ThrowIfNull(domainEvent);

    _logger.LogInformation("Setting up basic TestingLab permissions for new user {UserId} ({UserName})", domainEvent.UserId, domainEvent.Name);

    try {
      // Query the user's tenant associations since UserCreatedEvent doesn't include tenant context
      var userTenants = await _context.Set<TenantPermission>().Where(tp => tp.UserId == domainEvent.UserId && tp.DeletedAt == null && (tp.ExpiresAt == null || tp.ExpiresAt > SystemClock.UtcNow))
                                      .Select(tp => tp.TenantId)
                                      .Where(tenantId => tenantId.HasValue)
                                      .ToListAsync(cancellationToken);

      if (userTenants.Count == 0) {
        _logger.LogWarning("No tenant associations found for user {UserId}. Basic TestingLab permissions cannot be granted without tenant context.", domainEvent.UserId);

        return;
      }

      // Grant basic permissions for each tenant the user belongs to
      foreach (var tenantId in userTenants.Where(t => t.HasValue).Select(t => t!.Value)) { await GrantBasicTestingLabPermissions(domainEvent.UserId, tenantId, cancellationToken); }

      _logger.LogInformation("Successfully set up basic TestingLab permissions for user {UserId} in {TenantCount} tenants", domainEvent.UserId, userTenants.Count);
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Failed to set up TestingLab permissions for user {UserId}", domainEvent.UserId);

      // Don't rethrow - permission setup failures shouldn't fail user creation
        throw;
    }
  }

  private async Task GrantBasicTestingLabPermissions(Guid userId, Guid tenantId, CancellationToken cancellationToken) {
    _logger.LogDebug("Granting basic TestingLab permissions to user {UserId} in tenant {TenantId}", userId, tenantId);

    try {
      var defaultPermissions = (_configuration.GetSection("TestingLab:DefaultUserPermissions").Get<string[]>() ?? [
        $"{TestingLabResourceTypes.Request}:{TestingLabActions.Read}",
        $"{TestingLabResourceTypes.Session}:{TestingLabActions.Read}",
        $"{TestingLabResourceTypes.Location}:{TestingLabActions.Read}",
      ]);

      var existing = await _context.Set<TenantPermission>()
        .FirstOrDefaultAsync(tp => tp.UserId == userId && tp.TenantId == tenantId && tp.DeletedAt == null, cancellationToken)
        .ConfigureAwait(false);

      if (existing == null) {
        existing = new TenantPermission {
          UserId = userId,
          TenantId = tenantId,
          Permissions = defaultPermissions,
          Reason = "TestingLab default user permissions",
        };
        _context.Set<TenantPermission>().Add(existing);
      }
      else {
        existing.AddPermissions(defaultPermissions);
      }

      await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

      _logger.LogInformation("Successfully assigned default TestingLab permissions to user {UserId} in tenant {TenantId}", userId, tenantId);
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Failed to assign default TestingLab role to user {UserId} in tenant {TenantId}", userId, tenantId);
      // Don't rethrow - permission failures shouldn't fail user creation
        throw;
    }
  }
}
