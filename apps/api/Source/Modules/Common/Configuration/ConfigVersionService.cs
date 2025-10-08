using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.Common.Configuration;

/// <summary>
/// Implementation of configuration versioning and drift detection service.
/// </summary>
public sealed class ConfigVersionService : IConfigVersionService
{
    private readonly ILogger<ConfigVersionService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IConfigSnapshotRepository _repository;
    private readonly IConfigDiffEngine _diffEngine;

    public ConfigVersionService(
        ILogger<ConfigVersionService> logger,
        IConfiguration configuration,
        IConfigSnapshotRepository repository,
        IConfigDiffEngine diffEngine)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _diffEngine = diffEngine ?? throw new ArgumentNullException(nameof(diffEngine));
    }

    public async Task<ConfigSnapshot> CreateSnapshotAsync(string label = "", CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating configuration snapshot: {Label}", label);

            // Capture current configuration
            var config = CaptureCurrentConfiguration();

            // Calculate hash
            var hash = CalculateConfigHash(config);

            var snapshot = new ConfigSnapshot
            {
                Label = string.IsNullOrEmpty(label) ? $"Snapshot {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}" : label,
                ConfigurationHash = hash,
                Configuration = config
            };

            await _repository.SaveSnapshotAsync(snapshot, cancellationToken);

            _logger.LogInformation("Configuration snapshot created: {SnapshotId} with hash {Hash}", snapshot.Id, hash);

            return snapshot;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create configuration snapshot");
            throw;
        }
    }

    public async Task<ConfigSnapshot?> GetSnapshotAsync(Guid snapshotId, CancellationToken cancellationToken = default)
    {
        return await _repository.GetSnapshotAsync(snapshotId, cancellationToken);
    }

    public async Task<List<ConfigSnapshot>> GetHistoryAsync(int limit = 50, CancellationToken cancellationToken = default)
    {
        return await _repository.GetHistoryAsync(limit, cancellationToken);
    }

    public async Task<ConfigDiff> CompareSnapshotsAsync(Guid fromSnapshotId, Guid toSnapshotId, CancellationToken cancellationToken = default)
    {
        var fromSnapshot = await _repository.GetSnapshotAsync(fromSnapshotId, cancellationToken)
            ?? throw new InvalidOperationException($"Snapshot {fromSnapshotId} not found");

        var toSnapshot = await _repository.GetSnapshotAsync(toSnapshotId, cancellationToken)
            ?? throw new InvalidOperationException($"Snapshot {toSnapshotId} not found");

        var changes = _diffEngine.Compare(fromSnapshot.Configuration, toSnapshot.Configuration);

        return new ConfigDiff
        {
            FromSnapshot = fromSnapshot,
            ToSnapshot = toSnapshot,
            Changes = changes
        };
    }

    public async Task<ConfigDrift> DetectDriftAsync(Guid baselineSnapshotId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Detecting configuration drift from baseline {BaselineId}", baselineSnapshotId);

            // Get baseline snapshot
            var baselineSnapshot = await _repository.GetSnapshotAsync(baselineSnapshotId, cancellationToken)
                ?? throw new InvalidOperationException($"Baseline snapshot {baselineSnapshotId} not found");

            // Create current snapshot
            var currentSnapshot = await CreateSnapshotAsync("Drift Detection", cancellationToken);

            // Compare
            var diff = await CompareSnapshotsAsync(baselineSnapshot.Id, currentSnapshot.Id, cancellationToken);

            // Determine severity
            var severity = CalculateDriftSeverity(diff);

            var drift = new ConfigDrift
            {
                BaselineSnapshot = baselineSnapshot,
                CurrentSnapshot = currentSnapshot,
                Diff = diff,
                Severity = severity
            };

            if (drift.HasDrift)
            {
                _logger.LogWarning(
                    "Configuration drift detected: {Changes} changes with severity {Severity}",
                    diff.TotalChanges, severity);
            }
            else
            {
                _logger.LogInformation("No configuration drift detected");
            }

            return drift;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to detect configuration drift");
            throw;
        }
    }

    public async Task RollbackToSnapshotAsync(Guid snapshotId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.GetSnapshotAsync(snapshotId, cancellationToken)
            ?? throw new InvalidOperationException($"Snapshot {snapshotId} not found");

        _logger.LogWarning("Rolling back configuration to snapshot {SnapshotId} ({Label})", snapshotId, snapshot.Label);

        // Note: Actual rollback would require restart or dynamic config reload
        throw new NotImplementedException("Configuration rollback requires application restart");
    }

    private Dictionary<string, object> CaptureCurrentConfiguration()
    {
        var config = new Dictionary<string, object>();

        // Recursively capture all configuration values
        CaptureSection(_configuration, "", config);

        return config;
    }

    private void CaptureSection(IConfiguration section, string prefix, Dictionary<string, object> result)
    {
        foreach (var child in section.GetChildren())
        {
            var key = string.IsNullOrEmpty(prefix) ? child.Key : $"{prefix}:{child.Key}";

            if (child.Value != null)
            {
                result[key] = child.Value;
            }

            CaptureSection(child, key, result);
        }
    }

    private static string CalculateConfigHash(Dictionary<string, object> config)
    {
        // Sort keys for consistent hashing
        var sorted = config.OrderBy(kv => kv.Key).ToList();

        var json = JsonSerializer.Serialize(sorted);
        var bytes = Encoding.UTF8.GetBytes(json);

        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    private static DriftSeverity CalculateDriftSeverity(ConfigDiff diff)
    {
        if (!diff.HasChanges)
            return DriftSeverity.None;

        // Check for critical configuration changes
        var criticalKeys = new[] { "ConnectionStrings", "Authentication", "Security" };
        var hasCriticalChanges = diff.Changes.Any(c =>
            criticalKeys.Any(k => c.Path.StartsWith(k, StringComparison.OrdinalIgnoreCase)));

        if (hasCriticalChanges)
            return DriftSeverity.Critical;

        if (diff.TotalChanges > 10)
            return DriftSeverity.High;

        if (diff.TotalChanges > 5)
            return DriftSeverity.Medium;

        return DriftSeverity.Low;
    }
}

/// <summary>
/// Repository for storing configuration snapshots.
/// </summary>
public interface IConfigSnapshotRepository
{
    Task SaveSnapshotAsync(ConfigSnapshot snapshot, CancellationToken cancellationToken = default);
    Task<ConfigSnapshot?> GetSnapshotAsync(Guid snapshotId, CancellationToken cancellationToken = default);
    Task<List<ConfigSnapshot>> GetHistoryAsync(int limit, CancellationToken cancellationToken = default);
}

/// <summary>
/// Engine for computing configuration differences.
/// </summary>
public interface IConfigDiffEngine
{
    List<ConfigChange> Compare(Dictionary<string, object> from, Dictionary<string, object> to);
}
