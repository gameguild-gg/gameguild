using System.IO.Compression;
using GameGuild.Modules.Audit.Entities;
using GameGuild.Modules.Audit.Enums;
using System.Text.Json;
using GameGuild.CQRS;

namespace GameGuild.Modules.Audit.Services;

/// <summary>
/// Service for creating compliance evidence packages for regulatory submissions
/// </summary>
public class CompliancePackagingService : ICompliancePackagingService {
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
        ILogger<CompliancePackagingService> logger) {
        _repository = repository;
        _auditLogRepository = auditLogRepository;
        _anomalyRepository = anomalyRepository;
        _signingService = signingService;
        _logger = logger;
    }

    public async Task<Result<ComplianceEvidencePackage>> CreatePackageAsync(
        Guid tenantId,
        string packageName,
        ComplianceFramework framework,
        DateTime periodStart,
        DateTime periodEnd,
        string preparedBy,
        CancellationToken cancellationToken = default) {
        var package = ComplianceEvidencePackage.Create(
            tenantId,
            packageName,
            framework,
            "1.0", // Default version
            periodStart,
            periodEnd,
            preparedBy); await _repository.AddAsync(package, cancellationToken);

        _logger.LogInformation(
            "Created compliance package {PackageId} for framework {Framework}",
            package.Id,
            framework);

        return Result<ComplianceEvidencePackage>.Success(package);
    }

    public async Task<Result> AddAuditLogsToPackageAsync(
        Guid packageId,
        IEnumerable<Guid> auditLogIds,
        CancellationToken cancellationToken = default) {
        var package = await _repository.GetByIdAsync(packageId, cancellationToken);
        if (package is null)
            return Result.Failure("Package not found");

        var auditLogs = await _auditLogRepository
            .AsQueryable()
            .Where(x => auditLogIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        // Mark audit logs as part of evidence
        foreach (var log in auditLogs) {
            log.MarkAsEvidence(packageId);
            await _auditLogRepository.UpdateAsync(log, cancellationToken);
        }

        package.SetPackageContents(auditLogs.Count, 0, 0, CalculatePackageSize(auditLogs));
        await _repository.UpdateAsync(package, cancellationToken);

        _logger.LogInformation(
            "Added {Count} audit logs to package {PackageId}",
            auditLogs.Count,
            packageId);

        return Result.Success();
    }

    public async Task<Result> AddAnomaliesAsync(
        Guid packageId,
        IEnumerable<Guid> anomalyIds,
        CancellationToken cancellationToken = default) {
        var package = await _repository.GetByIdAsync(packageId, cancellationToken);
        if (package is null)
            return Result.Failure("Package not found");

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

        return Result.Success();
    }

    public async Task<Result> SignPackageAsync(
        Guid packageId,
        CancellationToken cancellationToken = default) {
        var package = await _repository.GetByIdAsync(packageId, cancellationToken);
        if (package is null)
            return Result.Failure("Package not found");

        var packageHash = ComputePackageHash(package);
        var signature = await _signingService.SignData(packageHash, cancellationToken);

        package.Sign(packageHash, signature);
        await _repository.UpdateAsync(package, cancellationToken);

        _logger.LogInformation("Signed compliance package {PackageId}", packageId);
        return Result.Success();
    }

    public async Task<Result> ReviewPackageAsync(
        Guid packageId,
        string reviewedBy,
        string? notes = null,
        CancellationToken cancellationToken = default) {
        var package = await _repository.GetByIdAsync(packageId, cancellationToken);
        if (package is null)
            return Result.Failure("Package not found");

        var reviewerId = Guid.TryParse(reviewedBy, out var userId) ? userId : Guid.Empty;
        package.MarkAsReviewed(reviewerId);
        await _repository.UpdateAsync(package, cancellationToken);

        _logger.LogInformation("Package {PackageId} reviewed by {UserId}", packageId, reviewedBy);
        return Result.Success();
    }

    public async Task<Result> ApprovePackageAsync(
        Guid packageId,
        string approvedBy,
        CancellationToken cancellationToken = default) {
        var package = await _repository.GetByIdAsync(packageId, cancellationToken);
        if (package is null)
            return Result.Failure("Package not found");

        package.Approve(approvedBy);
        await _repository.UpdateAsync(package, cancellationToken);

        _logger.LogInformation("Package {PackageId} approved by {UserId}", packageId, approvedBy);
        return Result.Success();
    }

    public async Task<Result<Stream>> ExportPackageAsync(
        Guid packageId,
        CancellationToken cancellationToken = default) {
        var package = await _repository.GetByIdAsync(packageId, cancellationToken);
        if (package is null)
            return Result<Stream>.Failure("Package not found");

        // Export as JSON with compression
        var json = JsonSerializer.Serialize(package, new JsonSerializerOptions { WriteIndented = true });
        var outputStream = new MemoryStream();
        using (var gzipStream = new GZipStream(outputStream, CompressionMode.Compress, leaveOpen: true))
        using (var writer = new StreamWriter(gzipStream)) {
            await writer.WriteAsync(json);
        }

        outputStream.Position = 0;
        _logger.LogInformation("Exported compliance package {PackageId}", packageId);
        return Result<Stream>.Success(outputStream);
    }

    public async Task<Result> DeliverPackageAsync(
        Guid packageId,
        string deliveryMethod,
        string deliveredTo,
        CancellationToken cancellationToken = default) {
        var package = await _repository.GetByIdAsync(packageId, cancellationToken);
        if (package is null)
            return Result.Failure("Package not found");

        package.MarkAsDelivered(deliveredTo, deliveryMethod, null);
        await _repository.UpdateAsync(package, cancellationToken);

        _logger.LogInformation(
            "Package {PackageId} delivered to {DeliveredTo} via {Method}",
            packageId,
            deliveredTo,
            deliveryMethod);

        return Result.Success();
    }

    private string ComputePackageHash(ComplianceEvidencePackage package) {
        var content = new {
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

    private long CalculatePackageSize<T>(List<T> items) {
        var json = JsonSerializer.Serialize(items);
        return System.Text.Encoding.UTF8.GetByteCount(json);
    }
}
