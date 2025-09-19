using System.Text.Json;
using GameGuild.Database;
using GameGuild.Source.Core.Services;
using GameGuild.Source.Core.Users;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Source.Core.Services;

/// <summary>
/// Service interface for managing user privacy settings
/// </summary>
public interface IUserPrivacyService {
    // Privacy Settings Management
    Task<UserPrivacySettings> GetUserPrivacySettingsAsync(Guid userId, Guid? tenantId = null);
    Task<UserPrivacySettings> CreateDefaultPrivacySettingsAsync(Guid userId, Guid? tenantId = null);
    Task<UserPrivacySettings> UpdatePrivacySettingsAsync(Guid userId, UpdatePrivacySettingsRequest request, Guid? tenantId = null);
    Task<bool> DeletePrivacySettingsAsync(Guid userId, Guid? tenantId = null);

    // Privacy Checking
    Task<bool> CanViewFieldAsync(Guid viewerUserId, Guid targetUserId, string fieldName, Guid? tenantId = null);
    Task<bool> CanContactUserAsync(Guid senderUserId, Guid recipientUserId, ContactMethod method, Guid? tenantId = null);
    Task<PrivacyLevel> GetFieldVisibilityAsync(Guid userId, string fieldName, Guid? tenantId = null);

    // Privacy Templates
    Task<UserPrivacySettings> ApplyPrivacyTemplateAsync(Guid userId, PrivacyTemplate template, Guid? tenantId = null);

    // Bulk Privacy Operations
    Task<IEnumerable<UserPrivacySettings>> GetBulkPrivacySettingsAsync(IEnumerable<Guid> userIds, Guid? tenantId = null);
    Task<Dictionary<Guid, bool>> CheckBulkFieldVisibilityAsync(Guid viewerUserId, IEnumerable<Guid> targetUserIds, string fieldName, Guid? tenantId = null);

    // Privacy Auditing
    Task LogPrivacyChangeAsync(Guid userId, string settingName, string? oldValue, string? newValue, Guid? changedByUserId = null, string? reason = null, Guid? tenantId = null);
    Task<IEnumerable<UserPrivacyAuditLog>> GetPrivacyAuditLogAsync(Guid userId, Guid? tenantId = null, int limit = 100);
}

/// <summary>
/// Contact methods for privacy checking
/// </summary>
public enum ContactMethod {
    DirectMessage,
    Mention,
    Invitation,
    Friend,
    Follow
}

/// <summary>
/// Privacy templates for quick setup
/// </summary>
public enum PrivacyTemplate {
    Default,
    Public,
    Private,
    TenantOnly
}

/// <summary>
/// Implementation of user privacy service
/// </summary>
public class UserPrivacyService(
  ApplicationDbContext context,
  ITenantIsolationService tenantIsolationService,
  IHttpContextAccessor httpContextAccessor,
  ILogger<UserPrivacyService> logger) : IUserPrivacyService {

    public async Task<UserPrivacySettings> GetUserPrivacySettingsAsync(Guid userId, Guid? tenantId = null) {
        var query = context.UserPrivacySettings
          .Where(ups => ups.UserId == userId);

        query = tenantIsolationService.ApplyTenantFilter(query, tenantId);

        var settings = await query.FirstOrDefaultAsync();

        if (settings == null) {
            // Create default settings if none exist
            settings = await CreateDefaultPrivacySettingsAsync(userId, tenantId);
        }

        return settings;
    }

    public async Task<UserPrivacySettings> CreateDefaultPrivacySettingsAsync(Guid userId, Guid? tenantId = null) {
        // Check if settings already exist
        var existing = await context.UserPrivacySettings
          .FirstOrDefaultAsync(ups => ups.UserId == userId &&
                                     (tenantId == null ? ups.Tenant == null : ups.Tenant!.Id == tenantId));

        if (existing != null) {
            return existing;
        }

        var tenant = tenantId.HasValue ? await context.Tenants.FindAsync(tenantId.Value) : null;
        var settings = UserPrivacySettings.CreateDefault(userId, tenant);

        context.UserPrivacySettings.Add(settings);
        await context.SaveChangesAsync();

        await LogPrivacyChangeAsync(userId, "PrivacySettings", null, "Created", userId, "Default settings created", tenantId);

        logger.LogInformation("Created default privacy settings for user {UserId} in tenant {TenantId}", userId, tenantId);
        return settings;
    }

    public async Task<UserPrivacySettings> UpdatePrivacySettingsAsync(Guid userId, UpdatePrivacySettingsRequest request, Guid? tenantId = null) {
        var settings = await GetUserPrivacySettingsAsync(userId, tenantId);
        var changes = new List<(string Setting, string? OldValue, string? NewValue)>();

        // Update visibility settings
        if (request.NameVisibility.HasValue && settings.NameVisibility != request.NameVisibility.Value) {
            changes.Add(("NameVisibility", settings.NameVisibility.ToString(), request.NameVisibility.Value.ToString()));
            settings.NameVisibility = request.NameVisibility.Value;
        }

        if (request.EmailVisibility.HasValue && settings.EmailVisibility != request.EmailVisibility.Value) {
            changes.Add(("EmailVisibility", settings.EmailVisibility.ToString(), request.EmailVisibility.Value.ToString()));
            settings.EmailVisibility = request.EmailVisibility.Value;
        }

        if (request.PhoneVisibility.HasValue && settings.PhoneVisibility != request.PhoneVisibility.Value) {
            changes.Add(("PhoneVisibility", settings.PhoneVisibility.ToString(), request.PhoneVisibility.Value.ToString()));
            settings.PhoneVisibility = request.PhoneVisibility.Value;
        }

        if (request.AvatarVisibility.HasValue && settings.AvatarVisibility != request.AvatarVisibility.Value) {
            changes.Add(("AvatarVisibility", settings.AvatarVisibility.ToString(), request.AvatarVisibility.Value.ToString()));
            settings.AvatarVisibility = request.AvatarVisibility.Value;
        }

        if (request.BioVisibility.HasValue && settings.BioVisibility != request.BioVisibility.Value) {
            changes.Add(("BioVisibility", settings.BioVisibility.ToString(), request.BioVisibility.Value.ToString()));
            settings.BioVisibility = request.BioVisibility.Value;
        }

        // Update activity settings
        if (request.LastSeenVisibility.HasValue && settings.LastSeenVisibility != request.LastSeenVisibility.Value) {
            changes.Add(("LastSeenVisibility", settings.LastSeenVisibility.ToString(), request.LastSeenVisibility.Value.ToString()));
            settings.LastSeenVisibility = request.LastSeenVisibility.Value;
        }

        if (request.OnlineStatusVisibility.HasValue && settings.OnlineStatusVisibility != request.OnlineStatusVisibility.Value) {
            changes.Add(("OnlineStatusVisibility", settings.OnlineStatusVisibility.ToString(), request.OnlineStatusVisibility.Value.ToString()));
            settings.OnlineStatusVisibility = request.OnlineStatusVisibility.Value;
        }

        // Update content settings
        if (request.PostsVisibility.HasValue && settings.PostsVisibility != request.PostsVisibility.Value) {
            changes.Add(("PostsVisibility", settings.PostsVisibility.ToString(), request.PostsVisibility.Value.ToString()));
            settings.PostsVisibility = request.PostsVisibility.Value;
        }

        if (request.ProjectsVisibility.HasValue && settings.ProjectsVisibility != request.ProjectsVisibility.Value) {
            changes.Add(("ProjectsVisibility", settings.ProjectsVisibility.ToString(), request.ProjectsVisibility.Value.ToString()));
            settings.ProjectsVisibility = request.ProjectsVisibility.Value;
        }

        // Update communication settings
        if (request.DirectMessagesAllowed.HasValue && settings.DirectMessagesAllowed != request.DirectMessagesAllowed.Value) {
            changes.Add(("DirectMessagesAllowed", settings.DirectMessagesAllowed.ToString(), request.DirectMessagesAllowed.Value.ToString()));
            settings.DirectMessagesAllowed = request.DirectMessagesAllowed.Value;
        }

        // Update boolean settings
        if (request.ShowInSearch.HasValue && settings.ShowInSearch != request.ShowInSearch.Value) {
            changes.Add(("ShowInSearch", settings.ShowInSearch.ToString(), request.ShowInSearch.Value.ToString()));
            settings.ShowInSearch = request.ShowInSearch.Value;
        }

        if (request.ShowInDirectory.HasValue && settings.ShowInDirectory != request.ShowInDirectory.Value) {
            changes.Add(("ShowInDirectory", settings.ShowInDirectory.ToString(), request.ShowInDirectory.Value.ToString()));
            settings.ShowInDirectory = request.ShowInDirectory.Value;
        }

        if (request.AllowAnalytics.HasValue && settings.AllowAnalytics != request.AllowAnalytics.Value) {
            changes.Add(("AllowAnalytics", settings.AllowAnalytics.ToString(), request.AllowAnalytics.Value.ToString()));
            settings.AllowAnalytics = request.AllowAnalytics.Value;
        }

        // Update custom settings if provided
        if (request.CustomSettings != null) {
            var oldCustom = settings.CustomSettings;
            var newCustom = JsonSerializer.Serialize(request.CustomSettings);
            if (oldCustom != newCustom) {
                changes.Add(("CustomSettings", oldCustom, newCustom));
                settings.CustomSettings = newCustom;
            }
        }

        if (changes.Any()) {
            settings.Touch();
            await context.SaveChangesAsync();

            // Log all changes
            foreach (var (setting, oldValue, newValue) in changes) {
                await LogPrivacyChangeAsync(userId, setting, oldValue, newValue, request.ChangedByUserId, request.Reason, tenantId);
            }

            logger.LogInformation("Updated {ChangeCount} privacy settings for user {UserId}", changes.Count, userId);
        }

        return settings;
    }

    public async Task<bool> DeletePrivacySettingsAsync(Guid userId, Guid? tenantId = null) {
        var query = context.UserPrivacySettings
          .Where(ups => ups.UserId == userId);

        query = tenantIsolationService.ApplyTenantFilter(query, tenantId);

        var settings = await query.FirstOrDefaultAsync();
        if (settings == null) return false;

        context.UserPrivacySettings.Remove(settings);
        await context.SaveChangesAsync();

        await LogPrivacyChangeAsync(userId, "PrivacySettings", "Exists", "Deleted", userId, "Privacy settings deleted", tenantId);

        logger.LogInformation("Deleted privacy settings for user {UserId}", userId);
        return true;
    }

    public async Task<bool> CanViewFieldAsync(Guid viewerUserId, Guid targetUserId, string fieldName, Guid? tenantId = null) {
        // User can always view their own information
        if (viewerUserId == targetUserId) return true;

        var targetSettings = await GetUserPrivacySettingsAsync(targetUserId, tenantId);
        var fieldVisibility = GetFieldVisibilityLevel(targetSettings, fieldName);

        return await CheckVisibilityPermissionAsync(viewerUserId, targetUserId, fieldVisibility, tenantId);
    }

    public async Task<bool> CanContactUserAsync(Guid senderUserId, Guid recipientUserId, ContactMethod method, Guid? tenantId = null) {
        if (senderUserId == recipientUserId) return true;

        var recipientSettings = await GetUserPrivacySettingsAsync(recipientUserId, tenantId);

        var requiredLevel = method switch {
            ContactMethod.DirectMessage => recipientSettings.DirectMessagesAllowed,
            ContactMethod.Mention => recipientSettings.MentionsAllowed,
            ContactMethod.Invitation => recipientSettings.InvitationsAllowed,
            ContactMethod.Friend => PrivacyLevel.TenantMembers, // Default for friend requests
            ContactMethod.Follow => PrivacyLevel.Public, // Default for following
            _ => PrivacyLevel.Private
        };

        return await CheckVisibilityPermissionAsync(senderUserId, recipientUserId, requiredLevel, tenantId);
    }

    public async Task<PrivacyLevel> GetFieldVisibilityAsync(Guid userId, string fieldName, Guid? tenantId = null) {
        var settings = await GetUserPrivacySettingsAsync(userId, tenantId);
        return GetFieldVisibilityLevel(settings, fieldName);
    }

    public async Task<UserPrivacySettings> ApplyPrivacyTemplateAsync(Guid userId, PrivacyTemplate template, Guid? tenantId = null) {
        var tenant = tenantId.HasValue ? await context.Tenants.FindAsync(tenantId.Value) : null;

        var settings = template switch {
            PrivacyTemplate.Public => UserPrivacySettings.CreatePublicProfile(userId, tenant),
            PrivacyTemplate.Private => UserPrivacySettings.CreatePrivateProfile(userId, tenant),
            PrivacyTemplate.TenantOnly => UserPrivacySettings.CreateDefault(userId, tenant), // TenantMembers is default
            _ => UserPrivacySettings.CreateDefault(userId, tenant)
        };

        // Remove existing settings if any
        var existing = await context.UserPrivacySettings
          .FirstOrDefaultAsync(ups => ups.UserId == userId &&
                                     (tenantId == null ? ups.Tenant == null : ups.Tenant!.Id == tenantId));

        if (existing != null) {
            context.UserPrivacySettings.Remove(existing);
        }

        context.UserPrivacySettings.Add(settings);
        await context.SaveChangesAsync();

        await LogPrivacyChangeAsync(userId, "PrivacyTemplate", existing?.ToString(), template.ToString(), userId, $"Applied {template} template", tenantId);

        logger.LogInformation("Applied privacy template {Template} for user {UserId}", template, userId);
        return settings;
    }

    public async Task<IEnumerable<UserPrivacySettings>> GetBulkPrivacySettingsAsync(IEnumerable<Guid> userIds, Guid? tenantId = null) {
        var query = context.UserPrivacySettings
          .Where(ups => userIds.Contains(ups.UserId));

        query = tenantIsolationService.ApplyTenantFilter(query, tenantId);

        return await query.ToListAsync();
    }

    public async Task<Dictionary<Guid, bool>> CheckBulkFieldVisibilityAsync(Guid viewerUserId, IEnumerable<Guid> targetUserIds, string fieldName, Guid? tenantId = null) {
        var result = new Dictionary<Guid, bool>();
        var settingsBatch = await GetBulkPrivacySettingsAsync(targetUserIds, tenantId);
        var settingsDict = settingsBatch.ToDictionary(s => s.UserId);

        foreach (var targetUserId in targetUserIds) {
            if (viewerUserId == targetUserId) {
                result[targetUserId] = true;
                continue;
            }

            if (settingsDict.TryGetValue(targetUserId, out var settings)) {
                var fieldVisibility = GetFieldVisibilityLevel(settings, fieldName);
                result[targetUserId] = await CheckVisibilityPermissionAsync(viewerUserId, targetUserId, fieldVisibility, tenantId);
            }
            else {
                // Default to most restrictive if no settings found
                result[targetUserId] = false;
            }
        }

        return result;
    }

    public async Task LogPrivacyChangeAsync(Guid userId, string settingName, string? oldValue, string? newValue, Guid? changedByUserId = null, string? reason = null, Guid? tenantId = null) {
        var tenant = tenantId.HasValue ? await context.Tenants.FindAsync(tenantId.Value) : null;
        var httpContext = httpContextAccessor.HttpContext;

        var auditLog = new UserPrivacyAuditLog {
            UserId = userId,
            Tenant = tenant,
            ChangeType = "PrivacySettingChange",
            SettingName = settingName,
            OldValue = oldValue,
            NewValue = newValue,
            ChangedByUserId = changedByUserId ?? userId,
            IpAddress = httpContext?.Connection.RemoteIpAddress?.ToString(),
            UserAgent = httpContext?.Request.Headers.UserAgent.ToString(),
            Reason = reason
        };

        context.UserPrivacyAuditLog.Add(auditLog);
        await context.SaveChangesAsync();
    }

    public async Task<IEnumerable<UserPrivacyAuditLog>> GetPrivacyAuditLogAsync(Guid userId, Guid? tenantId = null, int limit = 100) {
        var query = context.UserPrivacyAuditLog
          .Where(pal => pal.UserId == userId)
          .OrderByDescending(pal => pal.CreatedAt)
          .Take(limit);

        query = tenantIsolationService.ApplyTenantFilter(query, tenantId);

        return await query.ToListAsync();
    }

    private static PrivacyLevel GetFieldVisibilityLevel(UserPrivacySettings settings, string fieldName) {
        return fieldName.ToLowerInvariant() switch {
            "name" => settings.NameVisibility,
            "email" => settings.EmailVisibility,
            "phone" => settings.PhoneVisibility,
            "avatar" => settings.AvatarVisibility,
            "bio" => settings.BioVisibility,
            "lastseen" => settings.LastSeenVisibility,
            "onlinestatus" => settings.OnlineStatusVisibility,
            "activityfeed" => settings.ActivityFeedVisibility,
            "posts" => settings.PostsVisibility,
            "comments" => settings.CommentsVisibility,
            "achievements" => settings.AchievementsVisibility,
            "projects" => settings.ProjectsVisibility,
            "friends" => settings.FriendsListVisibility,
            "followers" => settings.FollowersVisibility,
            "following" => settings.FollowingVisibility,
            "statistics" => settings.StatisticsVisibility,
            "gaminghistory" => settings.GamingHistoryVisibility,
            _ => PrivacyLevel.Private // Default to most restrictive
        };
    }

    private async Task<bool> CheckVisibilityPermissionAsync(Guid viewerUserId, Guid targetUserId, PrivacyLevel requiredLevel, Guid? tenantId = null) {
        return requiredLevel switch {
            PrivacyLevel.Public => true,
            PrivacyLevel.TenantMembers => await AreInSameTenantAsync(viewerUserId, targetUserId, tenantId),
            PrivacyLevel.Friends => await AreFriendsAsync(viewerUserId, targetUserId, tenantId),
            PrivacyLevel.Private => false,
            _ => false
        };
    }

    private async Task<bool> AreInSameTenantAsync(Guid user1Id, Guid user2Id, Guid? tenantId = null) {
        if (!tenantId.HasValue) return false;

        // Check if both users have permissions in the same tenant
        var bothInTenant = await context.TenantPermissions
          .Where(tp => tp.UserId == user1Id || tp.UserId == user2Id)
          .Where(tp => tp.TenantId == tenantId.Value)
          .Select(tp => tp.UserId)
          .Distinct()
          .CountAsync();

        return bothInTenant == 2;
    }

    private async Task<bool> AreFriendsAsync(Guid user1Id, Guid user2Id, Guid? tenantId = null) {
        // This would need to be implemented based on your friendship/connection system
        // For now, return false as a placeholder
        await Task.CompletedTask;
        return false;
    }
}

// Request DTOs
public class UpdatePrivacySettingsRequest {
    // Profile visibility
    public PrivacyLevel? NameVisibility { get; set; }
    public PrivacyLevel? EmailVisibility { get; set; }
    public PrivacyLevel? PhoneVisibility { get; set; }
    public PrivacyLevel? AvatarVisibility { get; set; }
    public PrivacyLevel? BioVisibility { get; set; }

    // Activity visibility
    public PrivacyLevel? LastSeenVisibility { get; set; }
    public PrivacyLevel? OnlineStatusVisibility { get; set; }
    public PrivacyLevel? ActivityFeedVisibility { get; set; }

    // Content visibility
    public PrivacyLevel? PostsVisibility { get; set; }
    public PrivacyLevel? CommentsVisibility { get; set; }
    public PrivacyLevel? AchievementsVisibility { get; set; }
    public PrivacyLevel? ProjectsVisibility { get; set; }

    // Social visibility
    public PrivacyLevel? FriendsListVisibility { get; set; }
    public PrivacyLevel? FollowersVisibility { get; set; }
    public PrivacyLevel? FollowingVisibility { get; set; }

    // Statistics visibility
    public PrivacyLevel? StatisticsVisibility { get; set; }
    public PrivacyLevel? GamingHistoryVisibility { get; set; }

    // Communication settings
    public PrivacyLevel? DirectMessagesAllowed { get; set; }
    public PrivacyLevel? MentionsAllowed { get; set; }
    public PrivacyLevel? InvitationsAllowed { get; set; }

    // Boolean settings
    public bool? ShowInSearch { get; set; }
    public bool? ShowInDirectory { get; set; }
    public bool? ShowReadReceipts { get; set; }
    public bool? ShowTypingIndicators { get; set; }
    public bool? AllowAnalytics { get; set; }
    public bool? AllowPersonalization { get; set; }
    public bool? AllowThirdPartyIntegrations { get; set; }

    // Extensibility
    public Dictionary<string, object>? CustomSettings { get; set; }

    // Audit fields
    public Guid? ChangedByUserId { get; set; }
    public string? Reason { get; set; }
}