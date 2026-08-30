using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Represents a persisted authorization policy definition.
/// </summary>
[Table("PolicyDefinitions")]
[Index(nameof(PolicyName), nameof(TenantId), IsUnique = true)]
public class PolicyDefinitionEntity : EntityBase
{
    /// <summary>
    ///     Gets or sets the unique name of the policy.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string PolicyName { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the optional tenant ID for tenant-scoped policies.
    ///     Null indicates a global/base policy.
    /// </summary>
    public new Guid? TenantId { get; set; }

    /// <summary>
    ///     Gets or sets whether the policy requires an authenticated user.
    /// </summary>
    public bool RequireAuthentication { get; set; } = true;

    /// <summary>
    ///     Gets or sets the authentication schemes required by this policy (JSON array).
    /// </summary>
    [MaxLength(1000)]
    public string AuthenticationSchemesJson { get; set; } = "[]";

    /// <summary>
    ///     Gets or sets the permissions required by this policy (JSON array).
    /// </summary>
    [MaxLength(2000)]
    public string RequiredPermissionsJson { get; set; } = "[]";

    /// <summary>
    ///     Gets or sets the roles required by this policy (JSON array).
    /// </summary>
    [MaxLength(1000)]
    public string RequiredRolesJson { get; set; } = "[]";



    /// <summary>
    ///     Gets or sets whether Access Control List-based access should be checked (DAC).
    /// </summary>
    public bool RequireAccessControlListAccess { get; set; }

    /// <summary>
    ///     Gets or sets the resource type for DAC checks.
    /// </summary>
    [MaxLength(100)]
    public string? ResourceType { get; set; }

    /// <summary>
    ///     Gets or sets the minimum access level required.
    /// </summary>
    [MaxLength(50)]
    public string? MinimumAccessLevel { get; set; }

    /// <summary>
    ///     Gets or sets whether this policy is tenant-scoped.
    /// </summary>
    public bool IsTenantScoped { get; set; }

    /// <summary>
    ///     Gets or sets the policy version for cache invalidation.
    /// </summary>
    public long PolicyVersion { get; set; } = 1;

    /// <summary>
    ///     Gets or sets whether this policy is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    ///     Gets or sets an optional description of the policy.
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    ///     Gets or sets the rules that make up this policy (JSON array).
    /// </summary>
    [MaxLength(8000)]
    public string? RulesJson { get; set; }

    /// <summary>
    ///     Gets or sets whether to use the new rule-based evaluation.
    /// </summary>
    public bool UseRuleBasedEvaluation { get; set; }
}
