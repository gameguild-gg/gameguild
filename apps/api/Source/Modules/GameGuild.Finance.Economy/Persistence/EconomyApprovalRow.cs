namespace GameGuild.Finance.Economy.Persistence;

/// <summary>
///     Discriminator for the unified Economy dual-control approval family
///     (#1092 Tier 1.3). The payout-request dual control is NOT part of this
///     family — it lives in PayoutRequestReviewAuditEvent rows plus
///     FirstApprovalActorId and stays untouched.
/// </summary>
public enum EconomyApprovalKind
{
    /// <summary>Capability-policy activation approvals
    /// (formerly <c>economy_capability_policy_approvals</c>).</summary>
    CapabilityPolicy = 1,

    /// <summary>Kill-switch release approvals
    /// (formerly <c>economy_kill_switch_release_approvals</c>).</summary>
    KillSwitchRelease = 2,

    /// <summary>Projection-generation approvals
    /// (formerly <c>economy_projection_generation_approvals</c>).</summary>
    ProjectionGeneration = 3,
}

/// <summary>
///     Unified Economy dual-control approval row (#1092 Tier 1.3: three
///     identical five-column approval tables merged behind the
///     <see cref="EconomyApprovalKind"/> discriminator). Every kind carries
///     the same evidence shape — actor, reauthentication hash, approval
///     time — with the subject identified by <see cref="SubjectId"/> for the
///     two guid-keyed kinds and by <see cref="Generation"/> for projection
///     generations.
/// </summary>
public sealed class EconomyApprovalRow
{
    public Guid Id { get; set; }
    public EconomyApprovalKind ApprovalKind { get; set; }
    public Guid? SubjectId { get; set; }
    public long? Generation { get; set; }
    public Guid ActorId { get; set; }
    public string ReauthenticationHash { get; set; } = string.Empty;
    public DateTimeOffset ApprovedAt { get; set; }
}
