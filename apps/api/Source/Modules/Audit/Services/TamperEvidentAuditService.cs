using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GameGuild.Modules.Audit.Entities;

namespace GameGuild.Modules.Audit.Services;

/// <summary>
/// Service for managing tamper-evident audit logs with cryptographic integrity verification
/// </summary>
public class TamperEvidentAuditService : ITamperEvidentAuditService
{
    private readonly IRepository<TamperEvidentAuditLog, Guid> _repository;
    private readonly ICryptographicSigningService _signingService;
    private readonly ILogger<TamperEvidentAuditService> _logger;

    public TamperEvidentAuditService(
        IRepository<TamperEvidentAuditLog, Guid> repository,
        ICryptographicSigningService signingService,
        ILogger<TamperEvidentAuditService> logger)
    {
        _repository = repository;
        _signingService = signingService;
        _logger = logger;
    }

    public async Task<TamperEvidentAuditLog> CreateAuditLogAsync(
        Guid? tenantId,
        Guid? userId,
        string action,
        string entityType,
        string? entityId,
        object? beforeSnapshot,
        object? afterSnapshot,
        Dictionary<string, object>? changes,
        AuditRiskLevel riskLevel,
        string? ipAddress,
        string? userAgent,
        string? country,
        string? region,
        string? city,
        CancellationToken cancellationToken = default)
    {
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
            JsonSerializer.Serialize(beforeSnapshot),
            JsonSerializer.Serialize(afterSnapshot),
            changes != null ? JsonSerializer.Serialize(changes) : null,
            riskLevel,
            ipAddress,
            userAgent,
            country,
            region,
            city);

        // Set cryptographic hashes
        var contentHash = _signingService.ComputeContentHash(auditLog);
        var chainHash = _signingService.ComputeChainHash(contentHash, previousHash, sequenceNumber);
        auditLog.SetCryptographicHashes(contentHash, previousHash, chainHash, sequenceNumber);

        // Sign the audit log
        var signature = await _signingService.SignData(chainHash, cancellationToken);
        var keyId = await GetCurrentSigningKeyIdAsync(cancellationToken);
        auditLog.Sign(signature, keyId);

        // Save to repository
        await _repository.AddAsync(auditLog, cancellationToken);

        _logger.LogInformation(
            "Created tamper-evident audit log {AuditLogId} with sequence {SequenceNumber}",
            auditLog.Id,
            sequenceNumber);

        return auditLog;
    }

    public async Task<ChainVerificationResult> VerifyChainIntegrityAsync(
        Guid? tenantId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _repository.AsQueryable()
            .Where(x => x.TenantId == tenantId)
            .OrderBy(x => x.SequenceNumber);

        if (startDate.HasValue)
            query = query.Where(x => x.Timestamp >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(x => x.Timestamp <= endDate.Value);

        var logs = await query.ToListAsync(cancellationToken);

        var result = new ChainVerificationResult
        {
            TotalRecords = logs.Count,
            IsValid = true,
            VerifiedAt = DateTime.UtcNow
        };

        string? previousHash = null;
        long expectedSequence = logs.FirstOrDefault()?.SequenceNumber ?? 1;

        foreach (var log in logs)
        {
            // Verify sequence number
            if (log.SequenceNumber != expectedSequence)
            {
                result.IsValid = false;
                result.ErrorMessage = $"Sequence number mismatch at {log.Id}. Expected {expectedSequence}, got {log.SequenceNumber}";
                break;
            }

            // Verify content hash
            var computedContentHash = _signingService.ComputeContentHash(log);
            if (computedContentHash != log.ContentHash)
            {
                result.IsValid = false;
                result.ErrorMessage = $"Content hash mismatch at {log.Id}";
                break;
            }

            // Verify chain hash
            var computedChainHash = _signingService.ComputeChainHash(
                log.ContentHash,
                previousHash ?? string.Empty,
                log.SequenceNumber);

            if (computedChainHash != log.ChainHash)
            {
                result.IsValid = false;
                result.ErrorMessage = $"Chain hash mismatch at {log.Id}";
                break;
            }

            // Verify digital signature
            var signatureValid = await _signingService.VerifySignature(
                log.ChainHash,
                log.DigitalSignature ?? string.Empty,
                log.SigningKeyId ?? string.Empty,
                cancellationToken);

            if (!signatureValid)
            {
                result.IsValid = false;
                result.ErrorMessage = $"Digital signature invalid at {log.Id}";
                break;
            }

            previousHash = log.ChainHash;
            expectedSequence++;
            result.VerifiedRecords++;
        }

        return result;
    }

    public async Task<TamperEvidentAuditLog?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _repository.GetByIdAsync(id, cancellationToken);
    }

    public async Task<List<TamperEvidentAuditLog>> GetByTenantAsync(
        Guid tenantId,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        return await _repository
            .AsQueryable()
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<TamperEvidentAuditLog>> GetUnverifiedAsync(
        Guid? tenantId = null,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var query = _repository.AsQueryable()
            .Where(x => !x.IsVerified || x.LastVerifiedAt == null);

        if (tenantId.HasValue)
            query = query.Where(x => x.TenantId == tenantId);

        return await query
            .OrderBy(x => x.Timestamp)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkAsVerifiedAsync(
        Guid id,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        var log = await _repository.GetByIdAsync(id, cancellationToken);
        if (log == null)
            throw new InvalidOperationException($"Audit log {id} not found");

        log.MarkAsVerified(notes);
        await _repository.UpdateAsync(log, cancellationToken);

        _logger.LogInformation("Marked audit log {AuditLogId} as verified", id);
    }

    private async Task<string> GetCurrentSigningKeyIdAsync(CancellationToken cancellationToken)
    {
        // In a real implementation, this would retrieve the current active signing key ID
        // from a key management service or configuration
        return await Task.FromResult("default-key-2024");
    }
}
