using System.IO.Compression;
using GameGuild.Modules.Audit.Entities;
using System.Text.Json;

namespace GameGuild.Modules.Audit.Services;

/// <summary>
/// Service for creating compliance evidence packages for regulatory submissions
/// </summary>
public class CompliancePackagingService : ICompliancePackagingService
{
    private readonly IRepository<ComplianceEvidencePackage, Guid> _repository;
    private readonly IRepository<TamperEvidentAuditLog, Guid> _auditLogRepository;
    private readonly IRepository<AuditAnomaly, Guid> _anomalyRepository;
    private readonly ICryptographicSigningService _signingService;
    private readonly ILogger<CompliancePackagingService> _logger;

    public CompliancePackagingService(
        IRepository<ComplianceEvidencePackage, Guid> repository,
        IRepository<TamperEvidentAuditLog, Guid> auditLogRepository,
        IRepository<AuditAnomaly, Guid> anomalyRepository,
        ICryptographicSigningService signingService,
        ILogger<CompliancePackagingService> logger)
    {
        _repository = repository;
        _auditLogRepository = auditLogRepository;
        _anomalyRepository = anomalyRepository;
        _signingService = signingService;
        _logger = logger;
    }

    public async Task<ComplianceEvidencePackage> CreatePackageAsync(
        Guid tenantId,
        string packageName,
        ComplianceFramework framework,
        DateTime periodStart,
        DateTime periodEnd,
        string version,
        Guid preparedBy,
        CancellationToken cancellationToken = default)
    {
        var package = ComplianceEvidencePackage.Create(
            tenantId,
            packageName,
            framework,
            version,
            periodStart,
            periodEnd,
            preparedBy);

        await _repository.AddAsync(package, cancellationToken);

        _logger.LogInformation(
            "Created compliance package {PackageId} for framework {Framework}",
            package.Id,
            framework);

        return package;
    }

    public async Task AddAuditLogsToPackageAsync(
        Guid packageId,
        List<Guid> auditLogIds,
        CancellationToken cancellationToken = default)
    {
        var package = await _repository.GetByIdAsync(packageId, cancellationToken);
        if (package == null)
            throw new InvalidOperationException($"Package {packageId} not found");

        var auditLogs = await _auditLogRepository
            .AsQueryable()
            .Where(x => auditLogIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        // Mark audit logs as part of evidence
        foreach (var log in auditLogs)
        {
            log.MarkAsEvidence(packageId);
            await _auditLogRepository.UpdateAsync(log, cancellationToken);
        }

        package.SetPackageContents(auditLogs.Count, 0, 0, CalculatePackageSize(auditLogs));
        await _repository.UpdateAsync(package, cancellationToken);

        _logger.LogInformation(
            "Added {Count} audit logs to package {PackageId}",
            auditLogs.Count,
            packageId);
    }

    public async Task AddAnomaliesAsync(
        Guid packageId,
        List<Guid> anomalyIds,
        CancellationToken cancellationToken = default)
    {
        var package = await _repository.GetByIdAsync(packageId, cancellationToken);
        if (package == null)
            throw new InvalidOperationException($"Package {packageId} not found");

        var anomalies = await _anomalyRepository
            .AsQueryable()
            .Where(x => anomalyIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        package.SetPackageContents(
            package.TotalAuditLogs,
            anomalies.Count,
            package.TotalAccessLogs,
            package.PackageSizeBytes + CalculatePackageSize(anomalies));

        await _repository.UpdateAsync(package, cancellationToken);

        _logger.LogInformation(
            "Added {Count} anomalies to package {PackageId}",
            anomalies.Count,
            packageId);
    }

    public async Task SignPackageAsync(
        Guid packageId,
        CancellationToken cancellationToken = default)
    {
        var package = await _repository.GetByIdAsync(packageId, cancellationToken);
        if (package == null)
            throw new InvalidOperationException($"Package {packageId} not found");

        var packageHash = ComputePackageHash(package);
        var signature = await _signingService.SignData(packageHash, cancellationToken);

        package.Sign(packageHash, signature);
        await _repository.UpdateAsync(package, cancellationToken);

        _logger.LogInformation("Signed compliance package {PackageId}", packageId);
    }

    public async Task ReviewPackageAsync(
        Guid packageId,
        Guid reviewedBy,
        CancellationToken cancellationToken = default)
    {
        var package = await _repository.GetByIdAsync(packageId, cancellationToken);
        if (package == null)
            throw new InvalidOperationException($"Package {packageId} not found");

        package.MarkAsReviewed(reviewedBy);
        await _repository.UpdateAsync(package, cancellationToken);

        _logger.LogInformation("Package {PackageId} reviewed by {UserId}", packageId, reviewedBy);
    }

    public async Task ApprovePackageAsync(
        Guid packageId,
        Guid approvedBy,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        var package = await _repository.GetByIdAsync(packageId, cancellationToken);
        if (package == null)
            throw new InvalidOperationException($"Package {packageId} not found");

        package.Approve(approvedBy, notes);
        await _repository.UpdateAsync(package, cancellationToken);

        _logger.LogInformation("Package {PackageId} approved by {UserId}", packageId, approvedBy);
    }

    public async Task<byte[]> ExportPackageAsync(
        Guid packageId,
        CancellationToken cancellationToken = default)
    {
        var package = await _repository.GetByIdAsync(packageId, cancellationToken);
        if (package == null)
            throw new InvalidOperationException($"Package {packageId} not found");

        // Export as JSON with compression
        var json = JsonSerializer.Serialize(package, new JsonSerializerOptions { WriteIndented = true });
        using var outputStream = new MemoryStream();
        using (var gzipStream = new GZipStream(outputStream, CompressionMode.Compress))
        using (var writer = new StreamWriter(gzipStream))
        {
            await writer.WriteAsync(json);
        }

        _logger.LogInformation("Exported compliance package {PackageId}", packageId);
        return outputStream.ToArray();
    }

    public async Task DeliverPackageAsync(
        Guid packageId,
        string deliveredTo,
        string deliveryMethod,
        string? trackingId = null,
        CancellationToken cancellationToken = default)
    {
        var package = await _repository.GetByIdAsync(packageId, cancellationToken);
        if (package == null)
            throw new InvalidOperationException($"Package {packageId} not found");

        package.MarkAsDelivered(deliveredTo, deliveryMethod, trackingId);
        await _repository.UpdateAsync(package, cancellationToken);

        _logger.LogInformation(
            "Package {PackageId} delivered to {DeliveredTo} via {Method}",
            packageId,
            deliveredTo,
            deliveryMethod);
    }

    private string ComputePackageHash(ComplianceEvidencePackage package)
    {
        var content = new
        {
            package.Id,
            package.PackageName,
            package.Framework,
            package.PeriodStart,
            package.PeriodEnd,
            package.TotalAuditLogs,
            package.TotalAnomalies,
            package.TotalAccessLogs
        };

        var json = JsonSerializer.Serialize(content);
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(json));
        return Convert.ToBase64String(hashBytes);
    }

    private long CalculatePackageSize<T>(List<T> items)
    {
        var json = JsonSerializer.Serialize(items);
        return System.Text.Encoding.UTF8.GetByteCount(json);
    }
}
