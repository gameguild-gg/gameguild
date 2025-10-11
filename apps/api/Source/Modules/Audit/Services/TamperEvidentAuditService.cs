using System.Text.Json;
using GameGuild.Modules.Audit.Entities;


namespace GameGuild.Modules.Audit.Services;

/// <summary>
/// Service for managing tamper-evident audit logs with cryptographic integrity verification
/// </summary>
public class TamperEvidentAuditService : ITamperEvidentAuditService {
    private readonly IRepository<TamperEvidentAuditLog, Guid> _repository;
    private readonly ICryptographicSigningService _signingService;
    private readonly ILogger<TamperEvidentAuditService> _logger;

    public TamperEvidentAuditService(
        IRepository<TamperEvidentAuditLog, Guid> repository,
        ICryptographicSigningService signingService,
        ILogger<TamperEvidentAuditService> logger) {
        _repository = repository;
        _signingService = signingService;
        _logger = logger;
    }

    public async Task<Result<TamperEvidentAuditLog>> CreateAuditLogAsync(
        Guid tenantId,
        Guid? userId,
        string action,
        string entityType,
        Guid? entityId,
        string? beforeSnapshot,
        string? afterSnapshot,
        string changes,
        string riskLevel,
        string ipAddress,
        string userAgent,
        string? country = null,
        string? region = null,
        string? city = null,
        CancellationToken cancellationToken = default) {
        try {
            // Get the last audit log for hash chain
            var lastLog = await _repository
                .AsQueryable()
                .Where(x => x.TenantId == tenantId)
                .OrderByDescending(x => x.SequenceNumber)
                .FirstOrDefaultAsync(cancellationToken);

            var sequenceNumber = (lastLog?.SequenceNumber ?? 0) + 1;
            var previousHash = lastLog?.ChainHash ?? string.Empty;

            // Create the audit log
            var auditLog = TamperEvidentAuditLog.Create(
                tenantId,
                userId,
                action,
                entityType,
                entityId,
                beforeSnapshot,
                afterSnapshot,
                changes,
                riskLevel,
                ipAddress,
                userAgent,
                country,
                region,
                city,
                previousHash,
                sequenceNumber);

            // Set cryptographic hashes
            var contentHash = _signingService.ComputeContentHash(JsonSerializer.Serialize(auditLog));
            var chainHash = _signingService.ComputeChainHash(contentHash, previousHash, sequenceNumber);
            auditLog.SetCryptographicHashes(contentHash, chainHash);

            // Save to repository
            await _repository.AddAsync(auditLog, cancellationToken);

            _logger.LogInformation(
                "Created tamper-evident audit log {AuditLogId} with sequence {SequenceNumber}",
                auditLog.Id,
                sequenceNumber);

            return Result<TamperEvidentAuditLog>.Success(auditLog);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to create audit log");
            return Result<TamperEvidentAuditLog>.Failure($"Failed to create audit log: {ex.Message}");
        }
    }

    public async Task<Result<bool>> VerifyChainIntegrityAsync(Guid tenantId, CancellationToken cancellationToken = default) {
        try {
            var logs = await _repository
                .AsQueryable()
                .Where(x => x.TenantId == tenantId)
                .OrderBy(x => x.SequenceNumber)
                .ToListAsync(cancellationToken);

            if (logs.Count == 0) {
                return Result<bool>.Success(true);
            }

            string? previousHash = null;
            long expectedSequence = 1;

            foreach (var log in logs) {
                // Verify sequence number
                if (log.SequenceNumber != expectedSequence) {
                    return Result<bool>.Failure($"Sequence number mismatch at {log.Id}. Expected {expectedSequence}, found {log.SequenceNumber}");
                }

                // Verify previous hash matches
                if (previousHash != null && log.PreviousHash != previousHash) {
                    return Result<bool>.Failure($"Previous hash mismatch at {log.Id}");
                }

                // Verify content hash
                var computedContentHash = _signingService.ComputeContentHash(JsonSerializer.Serialize(log));
                if (computedContentHash != log.ContentHash) {
                    return Result<bool>.Failure($"Content hash mismatch at {log.Id}");
                }

                // Verify chain hash
                var computedChainHash = _signingService.ComputeChainHash(
                    log.ContentHash,
                    previousHash ?? string.Empty,
                    log.SequenceNumber);

                if (computedChainHash != log.ChainHash) {
                    return Result<bool>.Failure($"Chain hash mismatch at {log.Id}");
                }

                previousHash = log.ChainHash;
                expectedSequence++;
            }

            return Result<bool>.Success(true);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to verify chain integrity for tenant {TenantId}", tenantId);
            return Result<bool>.Failure($"Failed to verify chain integrity: {ex.Message}");
        }
    }

    public async Task<Result<TamperEvidentAuditLog>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default) {
        try {
            var log = await _repository.GetByIdAsync(id, cancellationToken);
            if (log == null) {
                return Result<TamperEvidentAuditLog>.Failure($"Audit log {id} not found");
            }
            return Result<TamperEvidentAuditLog>.Success(log);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to get audit log {Id}", id);
            return Result<TamperEvidentAuditLog>.Failure($"Failed to get audit log: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<TamperEvidentAuditLog>>> GetByTenantAsync(
        Guid tenantId,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default) {
        try {
            var logs = await _repository
                .AsQueryable()
                .Where(x => x.TenantId == tenantId)
                .OrderByDescending(x => x.Timestamp)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);

            return Result<IEnumerable<TamperEvidentAuditLog>>.Success(logs);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to get audit logs for tenant {TenantId}", tenantId);
            return Result<IEnumerable<TamperEvidentAuditLog>>.Failure($"Failed to get audit logs: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<TamperEvidentAuditLog>>> GetUnverifiedAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default) {
        try {
            var logs = await _repository
                .AsQueryable()
                .Where(x => x.TenantId == tenantId && (!x.IsVerified || x.LastVerifiedAt == null))
                .OrderBy(x => x.Timestamp)
                .ToListAsync(cancellationToken);

            return Result<IEnumerable<TamperEvidentAuditLog>>.Success(logs);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to get unverified audit logs for tenant {TenantId}", tenantId);
            return Result<IEnumerable<TamperEvidentAuditLog>>.Failure($"Failed to get unverified audit logs: {ex.Message}");
        }
    }

    public async Task<Result> MarkAsVerifiedAsync(
        Guid id,
        string? notes = null,
        CancellationToken cancellationToken = default) {
        try {
            var log = await _repository.GetByIdAsync(id, cancellationToken);
            if (log == null) {
                return Result.Failure($"Audit log {id} not found");
            }

            log.MarkAsVerified(notes);
            await _repository.UpdateAsync(log, cancellationToken);

            _logger.LogInformation("Marked audit log {AuditLogId} as verified", id);
            return Result.Success();
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to mark audit log {Id} as verified", id);
            return Result.Failure($"Failed to mark as verified: {ex.Message}");
        }
    }

    private async Task<string> GetCurrentSigningKeyIdAsync(CancellationToken cancellationToken) {
        // In a real implementation, this would retrieve the current active signing key ID
        // from a key management service or configuration
        return await Task.FromResult("default-key-2024");
    }
}
