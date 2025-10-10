using GameGuild.Database;
using GameGuild.Modules.Permissions.Abstractions;
using GameGuild.Modules.Permissions.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace GameGuild.Modules.Permissions.Services;

public class PolicyRegistryService : IPolicyRegistryService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PolicyRegistryService> _logger;

    public PolicyRegistryService(ApplicationDbContext context, ILogger<PolicyRegistryService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<PolicyBundle>> CreateBundleAsync(PolicyBundle bundle, CancellationToken cancellationToken = default)
    {
        try
        {
            // Calculate content hash
            bundle.ContentHash = CalculateHash(bundle.PolicyData);
            bundle.Status = PolicyBundleStatus.Draft;
            bundle.CreatedAt = DateTime.UtcNow;

            _context.Set<PolicyBundle>().Add(bundle);
            await _context.SaveChangesAsync(cancellationToken);

            // Log the action
            await LogActionAsync(bundle.Id, PolicyRegistryAction.Create, bundle.CreatedBy, true, cancellationToken);

            _logger.LogInformation("Created policy bundle {BundleId} '{Name}' version {Version}", bundle.Id, bundle.Name, bundle.Version);
            return Result<PolicyBundle>.Success(bundle);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create policy bundle");
            await LogActionAsync(null, PolicyRegistryAction.Create, bundle.CreatedBy, false, ex.Message, cancellationToken);
            return Result<PolicyBundle>.Failure($"Failed to create bundle: {ex.Message}");
        }
    }

    public async Task<Result<PolicyBundle>> UpdateBundleAsync(PolicyBundle bundle, CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await _context.Set<PolicyBundle>().FirstOrDefaultAsync(b => b.Id == bundle.Id, cancellationToken);
            if (existing == null)
                return Result<PolicyBundle>.Failure("Bundle not found");

            if (existing.Status == PolicyBundleStatus.Active)
                return Result<PolicyBundle>.Failure("Cannot update active bundle. Create a new version instead.");

            existing.Name = bundle.Name;
            existing.Description = bundle.Description;
            existing.PolicyData = bundle.PolicyData;
            existing.ContentHash = CalculateHash(bundle.PolicyData);
            existing.Metadata = bundle.Metadata;
            existing.EffectiveFrom = bundle.EffectiveFrom;
            existing.EffectiveUntil = bundle.EffectiveUntil;
            existing.UpdatedAt = DateTime.UtcNow;

            // Clear signature if content changed
            existing.DigitalSignature = null;
            existing.SignedBy = null;
            existing.SignedAt = null;

            await _context.SaveChangesAsync(cancellationToken);
            await LogActionAsync(bundle.Id, PolicyRegistryAction.Update, Guid.Empty, true, cancellationToken);

            _logger.LogInformation("Updated policy bundle {BundleId}", bundle.Id);
            return Result<PolicyBundle>.Success(existing);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update policy bundle {BundleId}", bundle.Id);
            await LogActionAsync(bundle.Id, PolicyRegistryAction.Update, Guid.Empty, false, ex.Message, cancellationToken);
            return Result<PolicyBundle>.Failure($"Failed to update bundle: {ex.Message}");
        }
    }

    public async Task<Result<PolicyBundle>> SignBundleAsync(Guid bundleId, string privateKey, Guid signedBy, CancellationToken cancellationToken = default)
    {
        try
        {
            var bundle = await _context.Set<PolicyBundle>().FirstOrDefaultAsync(b => b.Id == bundleId, cancellationToken);
            if (bundle == null)
                return Result<PolicyBundle>.Failure("Bundle not found");

            if (string.IsNullOrEmpty(bundle.ContentHash))
                return Result<PolicyBundle>.Failure("Bundle has no content hash. Calculate hash first.");

            // Sign the content hash
            using var rsa = RSA.Create();
            rsa.ImportFromPem(privateKey);
            var signature = rsa.SignData(
                Encoding.UTF8.GetBytes(bundle.ContentHash),
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            bundle.DigitalSignature = Convert.ToBase64String(signature);
            bundle.SignedBy = signedBy.ToString();
            bundle.SignedAt = DateTime.UtcNow;
            bundle.Status = PolicyBundleStatus.PendingApproval;
            bundle.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
            await LogActionAsync(bundleId, PolicyRegistryAction.Sign, signedBy, true, cancellationToken);

            _logger.LogInformation("Signed policy bundle {BundleId}", bundleId);
            return Result<PolicyBundle>.Success(bundle);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sign policy bundle {BundleId}", bundleId);
            await LogActionAsync(bundleId, PolicyRegistryAction.Sign, signedBy, false, ex.Message, cancellationToken);
            return Result<PolicyBundle>.Failure($"Failed to sign bundle: {ex.Message}");
        }
    }

    public async Task<Result<bool>> VerifyBundleSignatureAsync(Guid bundleId, string publicKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var bundle = await _context.Set<PolicyBundle>().FirstOrDefaultAsync(b => b.Id == bundleId, cancellationToken);
            if (bundle == null)
                return Result<bool>.Failure("Bundle not found");

            if (string.IsNullOrEmpty(bundle.DigitalSignature))
                return Result<bool>.Failure("Bundle is not signed");

            using var rsa = RSA.Create();
            rsa.ImportFromPem(publicKey);
            var signature = Convert.FromBase64String(bundle.DigitalSignature);
            var isValid = rsa.VerifyData(
                Encoding.UTF8.GetBytes(bundle.ContentHash),
                signature,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            await LogActionAsync(bundleId, PolicyRegistryAction.Verify, Guid.Empty, true, $"Verification result: {isValid}", cancellationToken);

            _logger.LogInformation("Verified signature for policy bundle {BundleId}: {IsValid}", bundleId, isValid);
            return Result<bool>.Success(isValid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify signature for policy bundle {BundleId}", bundleId);
            await LogActionAsync(bundleId, PolicyRegistryAction.Verify, Guid.Empty, false, ex.Message, cancellationToken);
            return Result<bool>.Failure($"Failed to verify signature: {ex.Message}");
        }
    }

    public async Task<Result<PolicyBundle>> ApproveBundleAsync(Guid bundleId, Guid approvedBy, CancellationToken cancellationToken = default)
    {
        try
        {
            var bundle = await _context.Set<PolicyBundle>().FirstOrDefaultAsync(b => b.Id == bundleId, cancellationToken);
            if (bundle == null)
                return Result<PolicyBundle>.Failure("Bundle not found");

            if (bundle.Status != PolicyBundleStatus.PendingApproval)
                return Result<PolicyBundle>.Failure($"Bundle must be in PendingApproval status. Current status: {bundle.Status}");

            if (string.IsNullOrEmpty(bundle.DigitalSignature))
                return Result<PolicyBundle>.Failure("Bundle must be signed before approval");

            bundle.Status = PolicyBundleStatus.Approved;
            bundle.ApprovedBy = approvedBy;
            bundle.ApprovedAt = DateTime.UtcNow;
            bundle.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
            await LogActionAsync(bundleId, PolicyRegistryAction.Approve, approvedBy, true, cancellationToken);

            _logger.LogInformation("Approved policy bundle {BundleId}", bundleId);
            return Result<PolicyBundle>.Success(bundle);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to approve policy bundle {BundleId}", bundleId);
            await LogActionAsync(bundleId, PolicyRegistryAction.Approve, approvedBy, false, ex.Message, cancellationToken);
            return Result<PolicyBundle>.Failure($"Failed to approve bundle: {ex.Message}");
        }
    }

    public async Task<Result<PolicyBundleDeployment>> DeployBundleAsync(
        Guid bundleId,
        Guid? tenantId,
        string environment,
        Guid deployedBy,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var bundle = await _context.Set<PolicyBundle>().FirstOrDefaultAsync(b => b.Id == bundleId, cancellationToken);
            if (bundle == null)
                return Result<PolicyBundleDeployment>.Failure("Bundle not found");

            if (bundle.Status != PolicyBundleStatus.Approved)
                return Result<PolicyBundleDeployment>.Failure("Only approved bundles can be deployed");

            var deployment = new PolicyBundleDeployment
            {
                BundleId = bundleId,
                TenantId = tenantId,
                Environment = environment,
                Status = PolicyDeploymentStatus.Deploying,
                DeployedAt = DateTime.UtcNow,
                DeployedBy = deployedBy,
                VerificationPassed = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Set<PolicyBundleDeployment>().Add(deployment);

            // Update bundle metadata
            bundle.DeploymentCount++;
            bundle.LastDeployedAt = DateTime.UtcNow;
            bundle.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
            await LogActionAsync(bundleId, PolicyRegistryAction.Deploy, deployedBy, true, $"Deployed to {environment}", cancellationToken);

            _logger.LogInformation("Deployed policy bundle {BundleId} to {Environment}", bundleId, environment);
            return Result<PolicyBundleDeployment>.Success(deployment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deploy policy bundle {BundleId}", bundleId);
            await LogActionAsync(bundleId, PolicyRegistryAction.Deploy, deployedBy, false, ex.Message, cancellationToken);
            return Result<PolicyBundleDeployment>.Failure($"Failed to deploy bundle: {ex.Message}");
        }
    }

    public async Task<Result> ActivateDeploymentAsync(Guid deploymentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var deployment = await _context.Set<PolicyBundleDeployment>()
                .Include(d => d.Bundle)
                .FirstOrDefaultAsync(d => d.Id == deploymentId, cancellationToken);

            if (deployment == null)
                return Result.Failure("Deployment not found");

            deployment.Status = PolicyDeploymentStatus.Active;
            deployment.ActivatedAt = DateTime.UtcNow;
            deployment.UpdatedAt = DateTime.UtcNow;

            // Mark bundle as active
            if (deployment.Bundle.Status == PolicyBundleStatus.Approved)
            {
                deployment.Bundle.Status = PolicyBundleStatus.Active;
                deployment.Bundle.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);
            await LogActionAsync(deployment.BundleId, PolicyRegistryAction.Activate, Guid.Empty, true, cancellationToken);

            _logger.LogInformation("Activated deployment {DeploymentId} for bundle {BundleId}", deploymentId, deployment.BundleId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to activate deployment {DeploymentId}", deploymentId);
            return Result.Failure($"Failed to activate deployment: {ex.Message}");
        }
    }

    public async Task<Result> RollbackDeploymentAsync(Guid deploymentId, string reason, Guid rolledBackBy, CancellationToken cancellationToken = default)
    {
        try
        {
            var deployment = await _context.Set<PolicyBundleDeployment>()
                .FirstOrDefaultAsync(d => d.Id == deploymentId, cancellationToken);

            if (deployment == null)
                return Result.Failure("Deployment not found");

            deployment.Status = PolicyDeploymentStatus.RolledBack;
            deployment.RolledBackAt = DateTime.UtcNow;
            deployment.RolledBackBy = rolledBackBy;
            deployment.RollbackReason = reason;
            deployment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
            await LogActionAsync(deployment.BundleId, PolicyRegistryAction.Rollback, rolledBackBy, true, reason, cancellationToken);

            _logger.LogInformation("Rolled back deployment {DeploymentId}: {Reason}", deploymentId, reason);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rollback deployment {DeploymentId}", deploymentId);
            await LogActionAsync(null, PolicyRegistryAction.Rollback, rolledBackBy, false, ex.Message, cancellationToken);
            return Result.Failure($"Failed to rollback deployment: {ex.Message}");
        }
    }

    public async Task<Result<PolicyBundle>> GetBundleAsync(Guid bundleId, CancellationToken cancellationToken = default)
    {
        try
        {
            var bundle = await _context.Set<PolicyBundle>()
                .Include(b => b.Deployments)
                .FirstOrDefaultAsync(b => b.Id == bundleId, cancellationToken);

            if (bundle == null)
                return Result<PolicyBundle>.Failure("Bundle not found");

            return Result<PolicyBundle>.Success(bundle);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get policy bundle {BundleId}", bundleId);
            return Result<PolicyBundle>.Failure($"Failed to get bundle: {ex.Message}");
        }
    }

    public async Task<Result<List<PolicyBundle>>> ListBundlesAsync(
        PolicyBundleType? type,
        PolicyBundleStatus? status,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Set<PolicyBundle>().AsQueryable();

            if (type.HasValue)
                query = query.Where(b => b.BundleType == type.Value);

            if (status.HasValue)
                query = query.Where(b => b.Status == status.Value);

            var bundles = await query
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync(cancellationToken);

            return Result<List<PolicyBundle>>.Success(bundles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list policy bundles");
            return Result<List<PolicyBundle>>.Failure($"Failed to list bundles: {ex.Message}");
        }
    }

    public async Task<Result<List<PolicyBundleDeployment>>> GetDeploymentsAsync(Guid bundleId, CancellationToken cancellationToken = default)
    {
        try
        {
            var deployments = await _context.Set<PolicyBundleDeployment>()
                .Where(d => d.BundleId == bundleId)
                .OrderByDescending(d => d.DeployedAt)
                .ToListAsync(cancellationToken);

            return Result<List<PolicyBundleDeployment>>.Success(deployments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get deployments for bundle {BundleId}", bundleId);
            return Result<List<PolicyBundleDeployment>>.Failure($"Failed to get deployments: {ex.Message}");
        }
    }

    public async Task<Result<RegistryStatistics>> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var bundles = await _context.Set<PolicyBundle>().ToListAsync(cancellationToken);
            var deployments = await _context.Set<PolicyBundleDeployment>().ToListAsync(cancellationToken);

            var stats = new RegistryStatistics
            {
                TotalBundles = bundles.Count,
                ActiveBundles = bundles.Count(b => b.Status == PolicyBundleStatus.Active),
                TotalDeployments = deployments.Count,
                ActiveDeployments = deployments.Count(d => d.Status == PolicyDeploymentStatus.Active),
                BundlesByType = bundles.GroupBy(b => b.BundleType).ToDictionary(g => g.Key, g => g.Count())
            };

            return Result<RegistryStatistics>.Success(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get registry statistics");
            return Result<RegistryStatistics>.Failure($"Failed to get statistics: {ex.Message}");
        }
    }

    private static string CalculateHash(string content)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
        return Convert.ToBase64String(hash);
    }

    private async Task LogActionAsync(
        Guid? bundleId,
        PolicyRegistryAction action,
        Guid performedBy,
        bool success,
        string? details = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var log = new PolicyRegistryAuditLog
            {
                BundleId = bundleId,
                Action = action,
                PerformedBy = performedBy,
                PerformedAt = DateTime.UtcNow,
                Success = success,
                Details = details,
                CreatedAt = DateTime.UtcNow
            };
            _context.Set<PolicyRegistryAuditLog>().Add(log);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to log registry action");
        }
    }
}
