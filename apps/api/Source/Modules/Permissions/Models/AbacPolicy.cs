namespace GameGuild.Modules.Permissions;

/// <summary>
/// Attribute-Based Access Control (ABAC) Policy Entity
/// Enables fine-grained access control based on attribute expressions
/// </summary>
[Table("AbacPolicies")]
[Index(nameof(Name), IsUnique = true, Name = "IX_AbacPolicies_Name")]
[Index(nameof(TenantId), Name = "IX_AbacPolicies_TenantId")]
[Index(nameof(ResourceType), Name = "IX_AbacPolicies_ResourceType")]
[Index(nameof(IsActive), Name = "IX_AbacPolicies_IsActive")]
public class AbacPolicy : EntityBase
{
    /// <summary>
    /// Unique policy name
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable description of the policy
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Tenant this policy applies to (null for global policies)
    /// </summary>
    public override Guid? TenantId { get; set; }

    /// <summary>
    /// Navigation property to Tenant
    /// </summary>
    [GraphQLIgnore]
    public virtual Tenant? Tenant { get; set; }

    /// <summary>
    /// Resource type this policy applies to (e.g., "Project", "Course", "Post")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>
    /// Permission type this policy grants or denies
    /// </summary>
    public PermissionType Permission { get; set; }

    /// <summary>
    /// Effect of the policy (Allow or Deny)
    /// </summary>
    public PolicyEffect Effect { get; set; } = PolicyEffect.Allow;

    /// <summary>
    /// Attribute expression in JSON format
    /// Example: {"user.role": "admin", "resource.status": "published", "context.time": "business_hours"}
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string AttributeExpression { get; set; } = "{}";

    /// <summary>
    /// Condition expression for complex logic (C#-like syntax)
    /// Example: "user.Age >= 18 && resource.Sensitivity == 'low' && context.IPAddress.StartsWith('192.168')"
    /// </summary>
    [Column(TypeName = "text")]
    public string? ConditionExpression { get; set; }

    /// <summary>
    /// Priority for policy evaluation (higher values evaluated first)
    /// </summary>
    public int Priority { get; set; } = 100;

    /// <summary>
    /// Whether this policy is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Optional expiration date for the policy
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Check if the policy is expired
    /// </summary>
    [GraphQLIgnore]
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow;

    /// <summary>
    /// Check if the policy is currently valid (active and not expired)
    /// </summary>
    [GraphQLIgnore]
    public bool IsValid => IsActive && !IsExpired && !IsDeleted;
}

/// <summary>
/// Policy effect (Allow or Deny)
/// </summary>
public enum PolicyEffect
{
    Allow = 0,
    Deny = 1
}
