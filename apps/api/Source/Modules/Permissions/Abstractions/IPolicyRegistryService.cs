using GameGuild.Modules.Permissions.Entities;
using GameGuild.Shared;

namespace GameGuild.Modules.Permissions.Abstractions;

public interface IPolicyRegistryService
{
    Task<Result<PolicyBundle>> CreateBundleAsync(PolicyBundle bundle, CancellationToken cancellationToken = default);
    Task<Result<PolicyBundle>> UpdateBundleAsync(PolicyBundle bundle, CancellationToken cancellationToken = default);
    Task<Result<PolicyBundle>> SignBundleAsync(Guid bundleId, string privateKey, Guid signedBy, CancellationToken cancellationToken = default);
    Task<Result<bool>> VerifyBundleSignatureAsync(Guid bundleId, string publicKey, CancellationToken cancellationToken = default);
    Task<Result<PolicyBundle>> ApproveBundleAsync(Guid bundleId, Guid approvedBy, CancellationToken cancellationToken = default);
    Task<Result<PolicyBundleDeployment>> DeployBundleAsync(Guid bundleId, Guid? tenantId, string environment, Guid deployedBy, CancellationToken cancellationToken = default);
    Task<Result> ActivateDeploymentAsync(Guid deploymentId, CancellationToken cancellationToken = default);
    Task<Result> RollbackDeploymentAsync(Guid deploymentId, string reason, Guid rolledBackBy, CancellationToken cancellationToken = default);
    Task<Result<PolicyBundle>> GetBundleAsync(Guid bundleId, CancellationToken cancellationToken = default);
    Task<Result<List<PolicyBundle>>> ListBundlesAsync(PolicyBundleType? type, PolicyBundleStatus? status, CancellationToken cancellationToken = default);
    Task<Result<List<PolicyBundleDeployment>>> GetDeploymentsAsync(Guid bundleId, CancellationToken cancellationToken = default);
    Task<Result<RegistryStatistics>> GetStatisticsAsync(CancellationToken cancellationToken = default);
}

public class RegistryStatistics
{
    public int TotalBundles { get; set; }
    public int ActiveBundles { get; set; }
    public int TotalDeployments { get; set; }
    public int ActiveDeployments { get; set; }
    public Dictionary<PolicyBundleType, int> BundlesByType { get; set; } = new();
}
