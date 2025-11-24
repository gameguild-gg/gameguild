using GameGuild.Permissions.Domain.Entities;
using GameGuild.Permissions.Domain.Models;

namespace GameGuild.Permissions.Domain.Abstractions;

/// <summary>
///     Service interface for managing policy bundles in a central registry.
///     Provides bundle creation, signing, approval, deployment, and rollback capabilities.
/// </summary>
public interface IPolicyRegistryService
{
    // ==================== BUNDLE MANAGEMENT ====================

    /// <summary>
    ///     Creates a new policy bundle.
    /// </summary>
    /// <param name="bundle">The policy bundle to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created bundle.</returns>
    Task<PolicyBundle> CreateBundleAsync(PolicyBundle bundle, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Updates an existing policy bundle (must be in Draft status).
    /// </summary>
    /// <param name="bundle">The policy bundle with updates.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated bundle.</returns>
    Task<PolicyBundle> UpdateBundleAsync(PolicyBundle bundle, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Digitally signs a policy bundle to ensure integrity.
    /// </summary>
    /// <param name="bundleId">The bundle ID.</param>
    /// <param name="privateKey">The private key for signing.</param>
    /// <param name="signedBy">The user performing the signing.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The signed bundle.</returns>
    Task<PolicyBundle> SignBundleAsync(Guid bundleId, string privateKey, Guid signedBy, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Verifies the digital signature of a policy bundle.
    /// </summary>
    /// <param name="bundleId">The bundle ID.</param>
    /// <param name="publicKey">The public key for verification.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if signature is valid, false otherwise.</returns>
    Task<bool> VerifyBundleSignatureAsync(Guid bundleId, string publicKey, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Approves a policy bundle for deployment.
    /// </summary>
    /// <param name="bundleId">The bundle ID.</param>
    /// <param name="approvedBy">The user approving the bundle.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The approved bundle.</returns>
    Task<PolicyBundle> ApproveBundleAsync(Guid bundleId, Guid approvedBy, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets a policy bundle by ID.
    /// </summary>
    /// <param name="bundleId">The bundle ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The bundle, or null if not found.</returns>
    Task<PolicyBundle?> GetBundleAsync(Guid bundleId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Lists policy bundles with optional filtering.
    /// </summary>
    /// <param name="type">Optional bundle type filter.</param>
    /// <param name="status">Optional status filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of matching bundles.</returns>
    Task<List<PolicyBundle>> ListBundlesAsync(PolicyBundleType? type = null, PolicyBundleStatus? status = null, CancellationToken cancellationToken = default);

    // ==================== DEPLOYMENT MANAGEMENT ====================

    /// <summary>
    ///     Deploys a policy bundle to an environment.
    /// </summary>
    /// <param name="bundleId">The bundle ID.</param>
    /// <param name="tenantId">The target tenant (null for global).</param>
    /// <param name="environment">The target environment (e.g., "Production", "Staging").</param>
    /// <param name="deployedBy">The user performing the deployment.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The deployment record.</returns>
    Task<PolicyBundleDeployment> DeployBundleAsync(Guid bundleId, Guid? tenantId, string environment, Guid deployedBy, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Activates a deployed bundle (makes it effective).
    /// </summary>
    /// <param name="deploymentId">The deployment ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ActivateDeploymentAsync(Guid deploymentId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Rolls back a deployment.
    /// </summary>
    /// <param name="deploymentId">The deployment ID.</param>
    /// <param name="reason">The reason for rollback.</param>
    /// <param name="rolledBackBy">The user performing the rollback.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RollbackDeploymentAsync(Guid deploymentId, string reason, Guid rolledBackBy, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets all deployments for a specific bundle.
    /// </summary>
    /// <param name="bundleId">The bundle ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of deployments.</returns>
    Task<List<PolicyBundleDeployment>> GetDeploymentsAsync(Guid bundleId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets statistics about the policy registry.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Registry statistics.</returns>
    Task<RegistryStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);
}
