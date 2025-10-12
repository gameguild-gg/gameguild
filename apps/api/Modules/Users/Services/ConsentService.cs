using GameGuild.Database;

namespace GameGuild.Modules.Users;

/// <summary>
/// Service for managing user consent and privacy preferences
/// </summary>
public interface IConsentService
{
    // Consent operations
    Task<Result<ConsentRecord>> GrantConsentAsync(Guid userId, string consentType, string? featureId = null,
        string consentVersion = "1.0", DateTime? expiresAt = null, string? ipAddress = null,
        string? userAgent = null, string source = "web", CancellationToken cancellationToken = default);

    Task<Result> RevokeConsentAsync(Guid userId, string consentType, string? featureId = null,
        string? ipAddress = null, string? userAgent = null, CancellationToken cancellationToken = default);

    Task<Result<bool>> HasActiveConsentAsync(Guid userId, string consentType, string? featureId = null,
        CancellationToken cancellationToken = default);

    Task<Result<List<ConsentRecord>>> GetUserConsentsAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Result<ConsentRecord?>> GetConsentAsync(Guid userId, string consentType, string? featureId = null,
        CancellationToken cancellationToken = default);

    Task<Result<int>> RemoveExpiredConsentsAsync(CancellationToken cancellationToken = default);

    // Privacy preferences operations
    Task<Result<PrivacyPreference>> GetPrivacyPreferencesAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Result<PrivacyPreference>> UpdatePrivacyPreferencesAsync(Guid userId,
        DataVisibilityLevel? visibilityLevel = null,
        bool? allowSearchEngineIndexing = null,
        bool? showInPublicDirectory = null,
        bool? allowAnalytics = null,
        bool? allowPersonalization = null,
        bool? allowThirdPartySharing = null,
        bool? allowActivityTracking = null,
        bool? allowLocationTracking = null,
        int? dataRetentionDays = null,
        CancellationToken cancellationToken = default);
}

public sealed class ConsentService : IConsentService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<ConsentService> _logger;
    private readonly ApplicationDbContext _context;

    public ConsentService(
        IUserRepository userRepository,
        ILogger<ConsentService> logger,
        ApplicationDbContext context)
    {
        _userRepository = userRepository;
        _logger = logger;
        _context = context;
    }

    public async Task<Result<ConsentRecord>> GrantConsentAsync(Guid userId, string consentType, string? featureId = null,
        string consentVersion = "1.0", DateTime? expiresAt = null, string? ipAddress = null,
        string? userAgent = null, string source = "web", CancellationToken cancellationToken = default)
    {
        try
        {
            var userExists = await _userRepository.ExistsAsync(userId, cancellationToken);
            if (!userExists)
            {
                return Result<ConsentRecord>.Failure($"User with ID {userId} not found");
            }

            // Check for existing consent
            var existingConsent = await _context.Set<ConsentRecord>()
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ConsentType == consentType && c.FeatureId == featureId,
                    cancellationToken);

            if (existingConsent != null)
            {
                // Log state change
                await LogConsentChangeAsync(userId, consentType, "modified",
                    System.Text.Json.JsonSerializer.Serialize(new { existingConsent.IsGranted, existingConsent.ConsentedAt }),
                    System.Text.Json.JsonSerializer.Serialize(new { IsGranted = true, ConsentedAt = DateTime.UtcNow }),
                    ipAddress, userAgent, cancellationToken);

                // Update existing consent
                existingConsent.Grant(ipAddress, userAgent);
                existingConsent.ConsentVersion = consentVersion;
                existingConsent.ExpiresAt = expiresAt;
                existingConsent.Source = source;

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Updated consent {ConsentType} for user {UserId}", consentType, userId);
                return Result<ConsentRecord>.Success(existingConsent);
            }

            // Create new consent
            var consent = new ConsentRecord
            {
                UserId = userId,
                ConsentType = consentType,
                FeatureId = featureId,
                IsGranted = true,
                ConsentVersion = consentVersion,
                ConsentedAt = DateTime.UtcNow,
                ExpiresAt = expiresAt,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Source = source
            };

            await _context.Set<ConsentRecord>().AddAsync(consent, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            // Log consent grant
            await LogConsentChangeAsync(userId, consentType, "granted", null,
                System.Text.Json.JsonSerializer.Serialize(new { IsGranted = true, consent.ConsentedAt }),
                ipAddress, userAgent, cancellationToken);

            _logger.LogInformation("Granted consent {ConsentType} for user {UserId}", consentType, userId);
            return Result<ConsentRecord>.Success(consent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error granting consent {ConsentType} for user {UserId}", consentType, userId);
            return Result<ConsentRecord>.Failure($"Failed to grant consent: {ex.Message}");
        }
    }

    public async Task<Result> RevokeConsentAsync(Guid userId, string consentType, string? featureId = null,
        string? ipAddress = null, string? userAgent = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var consent = await _context.Set<ConsentRecord>()
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ConsentType == consentType && c.FeatureId == featureId,
                    cancellationToken);

            if (consent == null)
            {
                return Result.Failure($"Consent {consentType} not found for user {userId}");
            }

            // Log state change
            await LogConsentChangeAsync(userId, consentType, "revoked",
                System.Text.Json.JsonSerializer.Serialize(new { consent.IsGranted, consent.ConsentedAt }),
                System.Text.Json.JsonSerializer.Serialize(new { IsGranted = false, RevokedAt = DateTime.UtcNow }),
                ipAddress, userAgent, cancellationToken);

            consent.Revoke();
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Revoked consent {ConsentType} for user {UserId}", consentType, userId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking consent {ConsentType} for user {UserId}", consentType, userId);
            return Result.Failure($"Failed to revoke consent: {ex.Message}");
        }
    }

    public async Task<Result<bool>> HasActiveConsentAsync(Guid userId, string consentType, string? featureId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var consent = await _context.Set<ConsentRecord>()
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ConsentType == consentType && c.FeatureId == featureId,
                    cancellationToken);

            var hasActiveConsent = consent != null && consent.IsActive;
            return Result<bool>.Success(hasActiveConsent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking consent {ConsentType} for user {UserId}", consentType, userId);
            return Result<bool>.Failure($"Failed to check consent: {ex.Message}");
        }
    }

    public async Task<Result<List<ConsentRecord>>> GetUserConsentsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var consents = await _context.Set<ConsentRecord>()
                .Where(c => c.UserId == userId)
                .OrderBy(c => c.ConsentType)
                .ThenBy(c => c.FeatureId)
                .ToListAsync(cancellationToken);

            return Result<List<ConsentRecord>>.Success(consents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving consents for user {UserId}", userId);
            return Result<List<ConsentRecord>>.Failure($"Failed to retrieve consents: {ex.Message}");
        }
    }

    public async Task<Result<ConsentRecord?>> GetConsentAsync(Guid userId, string consentType, string? featureId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var consent = await _context.Set<ConsentRecord>()
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ConsentType == consentType && c.FeatureId == featureId,
                    cancellationToken);

            return Result<ConsentRecord?>.Success(consent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving consent {ConsentType} for user {UserId}", consentType, userId);
            return Result<ConsentRecord?>.Failure($"Failed to retrieve consent: {ex.Message}");
        }
    }

    public async Task<Result<int>> RemoveExpiredConsentsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var expiredConsents = await _context.Set<ConsentRecord>()
                .Where(c => c.ExpiresAt.HasValue && c.ExpiresAt.Value <= DateTime.UtcNow)
                .ToListAsync(cancellationToken);

            if (expiredConsents.Any())
            {
                foreach (var consent in expiredConsents)
                {
                    await LogConsentChangeAsync(consent.UserId, consent.ConsentType, "expired",
                        System.Text.Json.JsonSerializer.Serialize(new { consent.IsGranted, consent.ExpiresAt }),
                        System.Text.Json.JsonSerializer.Serialize(new { IsGranted = false, ExpiredAt = DateTime.UtcNow }),
                        null, null, cancellationToken);
                }

                _context.Set<ConsentRecord>().RemoveRange(expiredConsents);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Removed {Count} expired consents", expiredConsents.Count);
            }

            return Result<int>.Success(expiredConsents.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing expired consents");
            return Result<int>.Failure($"Failed to remove expired consents: {ex.Message}");
        }
    }

    public async Task<Result<PrivacyPreference>> GetPrivacyPreferencesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var preferences = await _context.Set<PrivacyPreference>()
                .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

            if (preferences == null)
            {
                // Create default preferences
                preferences = new PrivacyPreference { UserId = userId };
                await _context.Set<PrivacyPreference>().AddAsync(preferences, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Created default privacy preferences for user {UserId}", userId);
            }

            return Result<PrivacyPreference>.Success(preferences);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving privacy preferences for user {UserId}", userId);
            return Result<PrivacyPreference>.Failure($"Failed to retrieve privacy preferences: {ex.Message}");
        }
    }

    public async Task<Result<PrivacyPreference>> UpdatePrivacyPreferencesAsync(Guid userId,
        DataVisibilityLevel? visibilityLevel = null, bool? allowSearchEngineIndexing = null,
        bool? showInPublicDirectory = null, bool? allowAnalytics = null,
        bool? allowPersonalization = null, bool? allowThirdPartySharing = null,
        bool? allowActivityTracking = null, bool? allowLocationTracking = null,
        int? dataRetentionDays = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var preferencesResult = await GetPrivacyPreferencesAsync(userId, cancellationToken);
            if (!preferencesResult.IsSuccess)
            {
                return Result<PrivacyPreference>.Failure(preferencesResult.Error);
            }

            var preferences = preferencesResult.Value!;

            if (visibilityLevel.HasValue) preferences.VisibilityLevel = visibilityLevel.Value;
            if (allowSearchEngineIndexing.HasValue) preferences.AllowSearchEngineIndexing = allowSearchEngineIndexing.Value;
            if (showInPublicDirectory.HasValue) preferences.ShowInPublicDirectory = showInPublicDirectory.Value;
            if (allowAnalytics.HasValue) preferences.AllowAnalytics = allowAnalytics.Value;
            if (allowPersonalization.HasValue) preferences.AllowPersonalization = allowPersonalization.Value;
            if (allowThirdPartySharing.HasValue) preferences.AllowThirdPartySharing = allowThirdPartySharing.Value;
            if (allowActivityTracking.HasValue) preferences.AllowActivityTracking = allowActivityTracking.Value;
            if (allowLocationTracking.HasValue) preferences.AllowLocationTracking = allowLocationTracking.Value;
            if (dataRetentionDays.HasValue) preferences.DataRetentionDays = dataRetentionDays.Value;

            preferences.MarkAsReviewed();

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated privacy preferences for user {UserId}", userId);
            return Result<PrivacyPreference>.Success(preferences);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating privacy preferences for user {UserId}", userId);
            return Result<PrivacyPreference>.Failure($"Failed to update privacy preferences: {ex.Message}");
        }
    }

    private async Task LogConsentChangeAsync(Guid userId, string consentType, string action,
        string? previousState, string? newState, string? ipAddress, string? userAgent,
        CancellationToken cancellationToken)
    {
        var auditLog = new ConsentAuditLog
        {
            UserId = userId,
            ConsentType = consentType,
            Action = action,
            PreviousState = previousState,
            NewState = newState,
            ChangedAt = DateTime.UtcNow,
            IpAddress = ipAddress,
            UserAgent = userAgent
        };

        await _context.Set<ConsentAuditLog>().AddAsync(auditLog, cancellationToken);
    }
}
