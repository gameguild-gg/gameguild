using GameGuild.Common.Services;
using Microsoft.Extensions.Logging;
using PermissionTemplate = GameGuild.Modules.Permissions.PermissionTemplate;
using PermissionConstraint = GameGuild.Modules.Permissions.PermissionConstraint;

namespace GameGuild.Database;

/// <summary>
/// Seeds default role templates for the permission system
/// </summary>
internal class PermissionSeeder(ISimplePermissionService permissionService, ILogger<PermissionSeeder> logger)
{
    private readonly ISimplePermissionService _permissionService = permissionService;
    private readonly ILogger<PermissionSeeder> _logger = logger;

    // High-performance logging using LoggerMessage delegates
    private static readonly Action<ILogger, Exception?> LogRoleTemplatesAlreadyExist =
        LoggerMessage.Define(LogLevel.Information, new EventId(1, nameof(LogRoleTemplatesAlreadyExist)),
            "Role templates already exist, skipping seed");

    private static readonly Action<ILogger, Exception?> LogRoleTemplatesSeededSuccessfully =
        LoggerMessage.Define(LogLevel.Information, new EventId(2, nameof(LogRoleTemplatesSeededSuccessfully)),
            "Successfully seeded default TestingLab role templates");

    private static readonly Action<ILogger, Exception?> LogRoleTemplatesSeedingFailed =
        LoggerMessage.Define(LogLevel.Error, new EventId(3, nameof(LogRoleTemplatesSeedingFailed)),
            "Failed to seed default role templates");

    public async Task SeedDefaultRoleTemplatesAsync()
    {
        try
        {
            // Check if we already have role templates
            var existingRoles = await _permissionService.GetRoleTemplatesAsync().ConfigureAwait(false);
            if (existingRoles.Count > 0)
            {
                LogRoleTemplatesAlreadyExist(_logger, null);
                return;
            }

            // ===== TESTING LAB ROLES =====

            // TestingLab Admin - Full control over all TestingLab resources
            await _permissionService.CreateRoleTemplateAsync(
                "TestingLabAdmin",
                "Full administrative control over all Testing Lab resources",
                new List<GameGuild.Modules.Permissions.PermissionTemplate>
                {
                    // Sessions - full control
                    new() { Action = "create", ResourceType = "TestingSession" },
                    new() { Action = "read", ResourceType = "TestingSession" },
                    new() { Action = "edit", ResourceType = "TestingSession" },
                    new() { Action = "delete", ResourceType = "TestingSession" },
                    
                    // Locations - full control
                    new() { Action = "create", ResourceType = "TestingLocation" },
                    new() { Action = "read", ResourceType = "TestingLocation" },
                    new() { Action = "edit", ResourceType = "TestingLocation" },
                    new() { Action = "delete", ResourceType = "TestingLocation" },
                    
                    // Feedback - full control
                    new() { Action = "create", ResourceType = "TestingFeedback" },
                    new() { Action = "read", ResourceType = "TestingFeedback" },
                    new() { Action = "edit", ResourceType = "TestingFeedback" },
                    new() { Action = "delete", ResourceType = "TestingFeedback" },
                    new() { Action = "moderate", ResourceType = "TestingFeedback" },
                    
                    // Requests - full control
                    new() { Action = "create", ResourceType = "TestingRequest" },
                    new() { Action = "read", ResourceType = "TestingRequest" },
                    new() { Action = "edit", ResourceType = "TestingRequest" },
                    new() { Action = "delete", ResourceType = "TestingRequest" },
                    new() { Action = "approve", ResourceType = "TestingRequest" },
                    
                    // Participants - full control
                    new() { Action = "manage", ResourceType = "TestingParticipant" },
                    new() { Action = "read", ResourceType = "TestingParticipant" },
                }).ConfigureAwait(false);

            // TestingLab Manager - Can manage but not delete
            await _permissionService.CreateRoleTemplateAsync(
                "TestingLabManager", 
                "Can manage testing resources but cannot delete sessions or locations",
                new List<GameGuild.Modules.Permissions.PermissionTemplate>
                {
                    // Sessions - can create/edit but not delete
                    new() { Action = "create", ResourceType = "TestingSession" },
                    new() { Action = "read", ResourceType = "TestingSession" },
                    new() { Action = "edit", ResourceType = "TestingSession" },
                    // No delete permission
                    
                    // Locations - can edit but not create/delete
                    new() { Action = "read", ResourceType = "TestingLocation" },
                    new() { Action = "edit", ResourceType = "TestingLocation" },
                    
                    // Feedback - can manage
                    new() { Action = "read", ResourceType = "TestingFeedback" },
                    new() { Action = "edit", ResourceType = "TestingFeedback" },
                    new() { Action = "moderate", ResourceType = "TestingFeedback" },
                    
                    // Requests - can handle
                    new() { Action = "read", ResourceType = "TestingRequest" },
                    new() { Action = "edit", ResourceType = "TestingRequest" },
                    new() { Action = "approve", ResourceType = "TestingRequest" },
                    
                    // Participants - can manage
                    new() { Action = "manage", ResourceType = "TestingParticipant" },
                    new() { Action = "read", ResourceType = "TestingParticipant" },
                }).ConfigureAwait(false);

            // TestingLab Coordinator - Limited management
            await _permissionService.CreateRoleTemplateAsync(
                "TestingLabCoordinator",
                "Can coordinate testing sessions and handle requests",
                new List<GameGuild.Modules.Permissions.PermissionTemplate>
                {
                    // Sessions - read and edit own
                    new() { Action = "read", ResourceType = "TestingSession" },
                    new() { Action = "edit", ResourceType = "TestingSession", 
                           Constraints = new List<GameGuild.Modules.Permissions.PermissionConstraint> { new GameGuild.Modules.Permissions.PermissionConstraint() { Type = "owner", Value = "true" } } },
                    
                    // Locations - read only
                    new() { Action = "read", ResourceType = "TestingLocation" },
                    
                    // Feedback - read and moderate
                    new() { Action = "read", ResourceType = "TestingFeedback" },
                    new() { Action = "moderate", ResourceType = "TestingFeedback" },
                    
                    // Requests - can handle
                    new() { Action = "read", ResourceType = "TestingRequest" },
                    new() { Action = "edit", ResourceType = "TestingRequest" },
                    
                    // Participants - read only
                    new() { Action = "read", ResourceType = "TestingParticipant" },
                }).ConfigureAwait(false);

            // TestingLab Tester - Basic participation
            await _permissionService.CreateRoleTemplateAsync(
                "TestingLabTester",
                "Can participate in testing sessions and provide feedback",
                new List<GameGuild.Modules.Permissions.PermissionTemplate>
                {
                    // Sessions - read only
                    new() { Action = "read", ResourceType = "TestingSession" },
                    
                    // Locations - read only
                    new() { Action = "read", ResourceType = "TestingLocation" },
                    
                    // Feedback - create and edit own
                    new() { Action = "create", ResourceType = "TestingFeedback" },
                    new() { Action = "read", ResourceType = "TestingFeedback" },
                    new() { Action = "edit", ResourceType = "TestingFeedback", 
                           Constraints = new List<GameGuild.Modules.Permissions.PermissionConstraint> { new GameGuild.Modules.Permissions.PermissionConstraint() { Type = "owner", Value = "true" } } },
                    
                    // Requests - create only
                    new() { Action = "create", ResourceType = "TestingRequest" },
                    new() { Action = "read", ResourceType = "TestingRequest", 
                           Constraints = new List<GameGuild.Modules.Permissions.PermissionConstraint> { new GameGuild.Modules.Permissions.PermissionConstraint() { Type = "owner", Value = "true" } } },
                }).ConfigureAwait(false);

            // TestingLab LocationManager - Specialized for locations
            await _permissionService.CreateRoleTemplateAsync(
                "TestingLabLocationManager",
                "Can manage testing locations and view sessions",
                new List<GameGuild.Modules.Permissions.PermissionTemplate>
                {
                    // Sessions - read only
                    new() { Action = "read", ResourceType = "TestingSession" },
                    
                    // Locations - full control
                    new() { Action = "create", ResourceType = "TestingLocation" },
                    new() { Action = "read", ResourceType = "TestingLocation" },
                    new() { Action = "edit", ResourceType = "TestingLocation" },
                    new() { Action = "delete", ResourceType = "TestingLocation" },
                    
                    // Feedback - read only
                    new() { Action = "read", ResourceType = "TestingFeedback" },
                }).ConfigureAwait(false);

            // TestingLab ReadOnly - View everything but edit nothing
            await _permissionService.CreateRoleTemplateAsync(
                "TestingLabReadOnly",
                "Can view all testing lab resources but cannot make changes",
                new List<GameGuild.Modules.Permissions.PermissionTemplate>
                {
                    new() { Action = "read", ResourceType = "TestingSession" },
                    new() { Action = "read", ResourceType = "TestingLocation" },
                    new() { Action = "read", ResourceType = "TestingFeedback" },
                    new() { Action = "read", ResourceType = "TestingRequest" },
                    new() { Action = "read", ResourceType = "TestingParticipant" },
                }).ConfigureAwait(false);

            LogRoleTemplatesSeededSuccessfully(_logger, null);
        }
        catch (Exception ex)
        {
            LogRoleTemplatesSeedingFailed(_logger, ex);
            throw;
        }
    }
}
