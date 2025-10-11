using System.Text.Json;
using Microsoft.Extensions.Logging;
using GameGuild.Database;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Modules.Users.Services;

/// <summary>
/// Service for GDPR compliance operations (data export, right to be forgotten)
/// </summary>
public interface IGdprService
{
    Task<PersonalDataExport> CreateExportRequestAsync(Guid userId, string format, bool includeMetadata, CancellationToken cancellationToken = default);
    Task<string> GeneratePersonalDataExportAsync(Guid userId, string format, bool includeMetadata, CancellationToken cancellationToken = default);
    Task<bool> DeleteUserDataAsync(Guid userId, string reason, bool anonymize, CancellationToken cancellationToken = default);
    Task<PersonalDataExport?> GetExportStatusAsync(Guid exportId, CancellationToken cancellationToken = default);
}

public sealed class GdprService : IGdprService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GdprService> _logger;
    private readonly string _exportBasePath;

    public GdprService(
        ApplicationDbContext context,
        ILogger<GdprService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _exportBasePath = configuration["Gdpr:ExportPath"] ?? System.IO.Path.Combine(System.IO.Path.GetTempPath(), "gdpr-exports");

        // Ensure export directory exists
        Directory.CreateDirectory(_exportBasePath);
    }

    public async Task<PersonalDataExport> CreateExportRequestAsync(
        Guid userId,
        string format,
        bool includeMetadata,
        CancellationToken cancellationToken = default)
    {
        var export = new PersonalDataExport
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Format = format,
            Status = DataExportStatus.Pending,
            RequestedAt = DateTime.UtcNow
        };

        _context.Set<PersonalDataExport>().Add(export);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created GDPR data export request {ExportId} for user {UserId}", export.Id, userId);

        // Trigger async processing
        _ = Task.Run(async () =>
        {
            try
            {
                var filePath = await GeneratePersonalDataExportAsync(userId, format, includeMetadata, cancellationToken);
                var fileInfo = new FileInfo(filePath);

                export.MarkCompleted(filePath, fileInfo.Length, 0); // Entity count will be set during generation
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate GDPR export for user {UserId}", userId);
                export.MarkFailed(ex.Message);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }, cancellationToken);

        return export;
    }

    public async Task<string> GeneratePersonalDataExportAsync(
        Guid userId,
        string format,
        bool includeMetadata,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating personal data export for user {UserId} in format {Format}", userId, format);

        // Aggregate all personal data
        var userData = await AggregateUserDataAsync(userId, cancellationToken);

        // Generate export file
        var fileName = $"user_{userId}_export_{DateTime.UtcNow:yyyyMMddHHmmss}.json";
        var filePath = System.IO.Path.Combine(_exportBasePath, fileName);

        var exportData = new
        {
            ExportDate = DateTime.UtcNow,
            UserId = userId,
            Metadata = includeMetadata ? new
            {
                ExportFormat = format,
                ExportVersion = "1.0",
                ComplianceStandard = "GDPR",
                DataRetentionPolicy = "30 days"
            } : null,
            PersonalData = userData
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(exportData, options), cancellationToken);

        _logger.LogInformation("Generated personal data export file {FilePath} for user {UserId}", filePath, userId);

        return filePath;
    }

    private async Task<Dictionary<string, object>> AggregateUserDataAsync(Guid userId, CancellationToken cancellationToken)
    {
        var data = new Dictionary<string, object>();

        // User profile data
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user != null)
        {
            data["Profile"] = new
            {
                user.Id,
                user.Username,
                Email = user.Email,
                user.GivenName,
                user.FamilyName,
                user.Name,
                user.Balance,
                user.AvailableBalance,
                user.IsActive,
                user.LastSeenAt,
                user.CreatedAt,
                user.UpdatedAt
            };
        }

        // Credentials (without sensitive data)
        var credentials = await _context.Set<Credentials.Credential>()
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .Select(c => new
            {
                c.Id,
                c.Type,
                c.IsActive,
                c.CreatedAt,
                LastUsed = c.LastUsedAt
            })
            .ToListAsync(cancellationToken);

        if (credentials.Any())
            data["Credentials"] = credentials;

        // Session tracking
        var sessions = await _context.Set<Authentication.SessionTracking>()
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .Take(100) // Last 100 sessions
            .Select(s => new
            {
                s.Id,
                s.IpAddress,
                s.UserAgent,
                s.DeviceFingerprint,
                s.CreatedAt,
                s.LastSeenAt,
                s.IsActive
            })
            .ToListAsync(cancellationToken);

        if (sessions.Any())
            data["Sessions"] = sessions;

        // Roles and permissions
        var userRoles = await _context.Set<UserRole>()
            .AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .Include(ur => ur.Role)
            .Select(ur => new
            {
                RoleName = ur.Role!.Name,
                ur.AssignedAt,
                ur.ExpiresAt
            })
            .ToListAsync(cancellationToken);

        if (userRoles.Any())
            data["Roles"] = userRoles;

        _logger.LogInformation("Aggregated {SectionCount} data sections for user {UserId}", data.Count, userId);

        return data;
    }

    public async Task<bool> DeleteUserDataAsync(
        Guid userId,
        string reason,
        bool anonymize,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Processing {Action} request for user {UserId}. Reason: {Reason}",
            anonymize ? "anonymization" : "deletion", userId, reason);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found for GDPR deletion", userId);
            return false;
        }

        if (anonymize)
        {
            // Anonymize user data (GDPR Article 17 exemptions)
            user.GivenName = $"Anonymized_{Guid.NewGuid():N}";
            user.FamilyName = "User";
            user.Name = user.GivenName;
            user.Username = $"anon_{Guid.NewGuid():N}";
            user.Email = $"anonymized_{Guid.NewGuid():N}@deleted.local";
            user.IsActive = false;
            user.LastSeenAt = DateTime.UtcNow;

            _logger.LogInformation("Anonymized user {UserId} data", userId);
        }
        else
        {
            // Hard delete: Remove all related data

            // Delete credentials
            var credentials = await _context.Set<Credentials.Credential>()
                .Where(c => c.UserId == userId)
                .ToListAsync(cancellationToken);
            _context.Set<Credentials.Credential>().RemoveRange(credentials);

            // Delete sessions
            var sessions = await _context.Set<Authentication.SessionTracking>()
                .Where(s => s.UserId == userId)
                .ToListAsync(cancellationToken);
            _context.Set<Authentication.SessionTracking>().RemoveRange(sessions);

            // Delete user roles
            var userRoles = await _context.Set<UserRole>()
                .Where(ur => ur.UserId == userId)
                .ToListAsync(cancellationToken);
            _context.Set<UserRole>().RemoveRange(userRoles);

            // Delete user
            _context.Users.Remove(user);

            _logger.LogWarning("Hard deleted user {UserId} and all related data", userId);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<PersonalDataExport?> GetExportStatusAsync(Guid exportId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<PersonalDataExport>()
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == exportId, cancellationToken);
    }
}
