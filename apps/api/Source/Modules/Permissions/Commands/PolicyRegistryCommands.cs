using GameGuild.CQRS;
using GameGuild.Modules.Permissions.Entities;

namespace GameGuild.Modules.Permissions.Commands;

// Create Bundle Command
public record CreatePolicyBundleCommand(
    string Name,
    string? Description,
    string Version,
    PolicyBundleType BundleType,
    string PolicyData,
    string? Metadata,
    Guid? TenantId,
    bool IsGlobal,
    DateTime? EffectiveFrom,
    DateTime? EffectiveUntil,
    Guid CreatedBy
) : IRequest<Result<PolicyBundle>>;

// Update Bundle Command
public record UpdatePolicyBundleCommand(
    Guid BundleId,
    string Name,
    string? Description,
    string PolicyData,
    string? Metadata,
    DateTime? EffectiveFrom,
    DateTime? EffectiveUntil
) : IRequest<Result<PolicyBundle>>;

// Sign Bundle Command
public record SignPolicyBundleCommand(
    Guid BundleId,
    string PrivateKey,
    Guid SignedBy
) : IRequest<Result<PolicyBundle>>;

// Verify Signature Query
public record VerifyPolicyBundleSignatureQuery(
    Guid BundleId,
    string PublicKey
) : IRequest<Result<bool>>;

// Approve Bundle Command
public record ApprovePolicyBundleCommand(
    Guid BundleId,
    Guid ApprovedBy
) : IRequest<Result<PolicyBundle>>;

// Deploy Bundle Command
public record DeployPolicyBundleCommand(
    Guid BundleId,
    Guid? TenantId,
    string Environment,
    Guid DeployedBy
) : IRequest<Result<PolicyBundleDeployment>>;

// Activate Deployment Command
public record ActivatePolicyDeploymentCommand(
    Guid DeploymentId
) : IRequest<Result>;

// Rollback Deployment Command
public record RollbackPolicyDeploymentCommand(
    Guid DeploymentId,
    string Reason,
    Guid RolledBackBy
) : IRequest<Result>;

// Get Bundle Query
public record GetPolicyBundleQuery(
    Guid BundleId
) : IRequest<Result<PolicyBundle>>;

// List Bundles Query
public record ListPolicyBundlesQuery(
    PolicyBundleType? Type,
    PolicyBundleStatus? Status
) : IRequest<Result<List<PolicyBundle>>>;

// Get Deployments Query
public record GetPolicyDeploymentsQuery(
    Guid BundleId
) : IRequest<Result<List<PolicyBundleDeployment>>>;

// Get Statistics Query
public record GetPolicyRegistryStatisticsQuery() : IRequest<Result<RegistryStatistics>>;
