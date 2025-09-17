using GameGuild.Modules.Permissions;
using GameGuild.Services;


namespace GameGuild.Database;

/// <summary> Seeds default role templates for the permission system </summary>
internal class PermissionSeeder(ISimplePermissionService permissionService, ILogger<PermissionSeeder> logger) {
  // High-performance logging using LoggerMessage delegates
  private static readonly Action<ILogger, Exception?> LogRoleTemplatesAlreadyExist = LoggerMessage.Define(LogLevel.Information, new EventId(1, nameof(LogRoleTemplatesAlreadyExist)), "Role templates already exist, skipping seed");

  private static readonly Action<ILogger, Exception?> LogRoleTemplatesSeededSuccessfully = LoggerMessage.Define(
    LogLevel.Information,
    new EventId(2, nameof(LogRoleTemplatesSeededSuccessfully)),
    "Successfully seeded default TestingLab role templates"
  );

  private static readonly Action<ILogger, Exception?> LogRoleTemplatesSeedingFailed = LoggerMessage.Define(LogLevel.Error, new EventId(3, nameof(LogRoleTemplatesSeedingFailed)), "Failed to seed default role templates");

  private readonly ILogger<PermissionSeeder> _logger = logger;

  private readonly ISimplePermissionService _permissionService = permissionService;

  public async Task SeedDefaultRoleTemplatesAsync() {
    try {
      // Check if we already have role templates
      var existingRoles = await _permissionService.GetRoleTemplatesAsync().ConfigureAwait(false);

      if (existingRoles.Count > 0) {
        LogRoleTemplatesAlreadyExist(_logger, null);

        return;
      }

      // ===== TESTING LAB ROLES =====

      // TestingLab Admin - Full control over all TestingLab resources
      await _permissionService.CreateRoleTemplateAsync(
                                "TestingLabAdmin",
                                "Full administrative control over all Testing Lab resources",
                                [
                                  new PermissionTemplate { Action = "create", ResourceType = "TestingLabSettings" },
                                  new PermissionTemplate { Action = "read", ResourceType = "TestingLabSettings" },
                                  new PermissionTemplate { Action = "edit", ResourceType = "TestingLabSettings" },
                                  new PermissionTemplate { Action = "delete", ResourceType = "TestingLabSettings" },

                                  // Sessions - full control
                                  new PermissionTemplate { Action = "create", ResourceType = "TestingSession" },
                                  new PermissionTemplate { Action = "read", ResourceType = "TestingSession" },
                                  new PermissionTemplate { Action = "edit", ResourceType = "TestingSession" },
                                  new PermissionTemplate { Action = "delete", ResourceType = "TestingSession" },

                                  // Locations - full control
                                  new PermissionTemplate { Action = "create", ResourceType = "TestingLocation" },
                                  new PermissionTemplate { Action = "read", ResourceType = "TestingLocation" },
                                  new PermissionTemplate { Action = "edit", ResourceType = "TestingLocation" },
                                  new PermissionTemplate { Action = "delete", ResourceType = "TestingLocation" },

                                  // Feedback - full control

                                  new PermissionTemplate { Action = "create", ResourceType = "TestingFeedback" },
                                  new PermissionTemplate { Action = "read", ResourceType = "TestingFeedback" },
                                  new PermissionTemplate { Action = "edit", ResourceType = "TestingFeedback" },
                                  new PermissionTemplate { Action = "delete", ResourceType = "TestingFeedback" },
                                  new PermissionTemplate { Action = "moderate", ResourceType = "TestingFeedback" },

                                  // Requests - full control

                                  new PermissionTemplate { Action = "create", ResourceType = "TestingRequest" },
                                  new PermissionTemplate { Action = "read", ResourceType = "TestingRequest" },
                                  new PermissionTemplate { Action = "edit", ResourceType = "TestingRequest" },
                                  new PermissionTemplate { Action = "delete", ResourceType = "TestingRequest" },
                                  new PermissionTemplate { Action = "approve", ResourceType = "TestingRequest" },

                                  // Participants - full control

                                  new PermissionTemplate { Action = "manage", ResourceType = "TestingParticipant" },
                                  new PermissionTemplate { Action = "read", ResourceType = "TestingParticipant" },
                                ]
                              )
                              .ConfigureAwait(false);

      // TestingLab Manager - Can manage but not delete
      await _permissionService.CreateRoleTemplateAsync(
                                "TestingLabManager",
                                "Can manage testing resources but cannot delete sessions or locations",
                                [
                                  new PermissionTemplate { Action = "read", ResourceType = "TestingLabSettings" },
                                  new PermissionTemplate { Action = "edit", ResourceType = "TestingLabSettings" },

                                  // Sessions - can create/edit but not delete
                                  new PermissionTemplate { Action = "create", ResourceType = "TestingSession" },
                                  new PermissionTemplate { Action = "read", ResourceType = "TestingSession" },
                                  new PermissionTemplate { Action = "edit", ResourceType = "TestingSession" },
                                  // No delete permission

                                  // Locations - can edit but not create/delete
                                  new PermissionTemplate { Action = "read", ResourceType = "TestingLocation" },
                                  new PermissionTemplate { Action = "edit", ResourceType = "TestingLocation" },

                                  // Feedback - can manage
                                  new PermissionTemplate { Action = "read", ResourceType = "TestingFeedback" },
                                  new PermissionTemplate { Action = "edit", ResourceType = "TestingFeedback" },
                                  new PermissionTemplate { Action = "moderate", ResourceType = "TestingFeedback" },

                                  // Requests - can handle

                                  new PermissionTemplate { Action = "read", ResourceType = "TestingRequest" },
                                  new PermissionTemplate { Action = "edit", ResourceType = "TestingRequest" },
                                  new PermissionTemplate { Action = "approve", ResourceType = "TestingRequest" },

                                  // Participants - can manage

                                  new PermissionTemplate { Action = "manage", ResourceType = "TestingParticipant" },
                                  new PermissionTemplate { Action = "read", ResourceType = "TestingParticipant" },
                                ]
                              )
                              .ConfigureAwait(false);

      // TestingLab Coordinator - Limited management
      await _permissionService.CreateRoleTemplateAsync(
                                "TestingLabCoordinator",
                                "Can coordinate testing sessions and handle requests",
                                [
                                  new PermissionTemplate { Action = "read", ResourceType = "TestingLabSettings" },

                                  // Sessions - read and edit own
                                  new PermissionTemplate { Action = "read", ResourceType = "TestingSession" },
                                  new PermissionTemplate { Action = "edit", ResourceType = "TestingSession", Constraints = [new PermissionConstraint { Type = "owner", Value = "true" }] },

                                  // Locations - read only
                                  new PermissionTemplate { Action = "read", ResourceType = "TestingLocation" },

                                  // Feedback - read and moderate
                                  new PermissionTemplate { Action = "read", ResourceType = "TestingFeedback" },
                                  new PermissionTemplate { Action = "moderate", ResourceType = "TestingFeedback" },

                                  // Requests - can handle
                                  new PermissionTemplate { Action = "read", ResourceType = "TestingRequest" },
                                  new PermissionTemplate { Action = "edit", ResourceType = "TestingRequest" },

                                  // Participants - read only
                                  new PermissionTemplate { Action = "read", ResourceType = "TestingParticipant" },
                                ]
                              )
                              .ConfigureAwait(false);

      // TestingLab Tester - Basic participation
      await _permissionService.CreateRoleTemplateAsync(
                                "TestingLabTester",
                                "Can participate in testing sessions and provide feedback",
                                [
                                  new PermissionTemplate { Action = "read", ResourceType = "TestingLabSettings" },

                                  // Sessions - read only
                                  new PermissionTemplate { Action = "read", ResourceType = "TestingSession" },

                                  // Locations - read only
                                  new PermissionTemplate { Action = "read", ResourceType = "TestingLocation" },

                                  // Feedback - create and edit own
                                  new PermissionTemplate { Action = "create", ResourceType = "TestingFeedback" },
                                  new PermissionTemplate { Action = "read", ResourceType = "TestingFeedback" },
                                  new PermissionTemplate { Action = "edit", ResourceType = "TestingFeedback", Constraints = [new PermissionConstraint { Type = "owner", Value = "true" }] },

                                  // Requests - create only
                                  new PermissionTemplate { Action = "create", ResourceType = "TestingRequest" },
                                  new PermissionTemplate { Action = "read", ResourceType = "TestingRequest", Constraints = [new PermissionConstraint { Type = "owner", Value = "true" }] },
                                ]
                              )
                              .ConfigureAwait(false);

      // TestingLab LocationManager - Specialized for locations
      await _permissionService.CreateRoleTemplateAsync(
                                "TestingLabLocationManager",
                                "Can manage testing locations and view sessions",
                                [
                                  new PermissionTemplate { Action = "read", ResourceType = "TestingLabSettings" },

                                  // Sessions - read only
                                  new PermissionTemplate { Action = "read", ResourceType = "TestingSession" },

                                  // Locations - full control
                                  new PermissionTemplate { Action = "create", ResourceType = "TestingLocation" },
                                  new PermissionTemplate { Action = "read", ResourceType = "TestingLocation" },
                                  new PermissionTemplate { Action = "edit", ResourceType = "TestingLocation" },
                                  new PermissionTemplate { Action = "delete", ResourceType = "TestingLocation" },

                                  // Feedback - read only
                                  new PermissionTemplate { Action = "read", ResourceType = "TestingFeedback" },
                                ]
                              )
                              .ConfigureAwait(false);

      // TestingLab ReadOnly - View everything but edit nothing
      await _permissionService.CreateRoleTemplateAsync(
                                "TestingLabReadOnly",
                                "Can view all testing lab resources but cannot make changes",
                                [
                                  new PermissionTemplate { Action = "read", ResourceType = "TestingLabSettings" },
                                  new PermissionTemplate { Action = "read", ResourceType = "TestingSession" },
                                  new PermissionTemplate { Action = "read", ResourceType = "TestingLocation" },
                                  new PermissionTemplate { Action = "read", ResourceType = "TestingFeedback" },
                                  new PermissionTemplate { Action = "read", ResourceType = "TestingRequest" },
                                  new PermissionTemplate { Action = "read", ResourceType = "TestingParticipant" },
                                ]
                              )
                              .ConfigureAwait(false);

      LogRoleTemplatesSeededSuccessfully(_logger, null);
    }
    catch (Exception ex) {
      LogRoleTemplatesSeedingFailed(_logger, ex);

      throw;
    }
  }
}
