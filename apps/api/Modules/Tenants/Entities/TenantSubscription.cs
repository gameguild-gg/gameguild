namespace GameGuild.Modules.Tenants;

/// <summary>
///     Represents a tenant's subscription plan and billing status.
///     Tracks subscription lifecycle, plan details, and payment information.
/// </summary>
[Table("tenant_subscriptions")]
[Index(nameof(TenantId), IsUnique = true)]
[Index(nameof(PlanId))]
[Index(nameof(Status))]
[Index(nameof(ExpiresAt))]
public class TenantSubscription : EntityBase, ITenantable
{
    /// <summary>
    ///     Default constructor
    /// </summary>
    public TenantSubscription() { }

    /// <summary>
    ///     Constructor for partial initialization
    /// </summary>
    /// <param name="partial">Partial tenant subscription data</param>
    public TenantSubscription(object partial) : base(partial) { }

    /// <summary>
    /// The tenant this subscription belongs to
    /// </summary>
    [Required]
    public new Guid? TenantId { get; set; }

    /// <summary>
    ///     Navigation property to the tenant
    /// </summary>
    [ForeignKey(nameof(TenantId))]
    public override Tenant? Tenant { get; set; }

    /// <summary>
    ///     ID of the subscription plan
    /// </summary>
    [Required]
    public Guid PlanId { get; set; }

    /// <summary>
    ///     Name of the plan (e.g., "Free", "Basic", "Pro", "Enterprise")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string PlanName { get; set; } = string.Empty;

    /// <summary>
    ///     Subscription status (Active, Expired, Cancelled, Suspended)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Active";

    /// <summary>
    ///     When the subscription starts
    /// </summary>
    public DateTime StartsAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     When the subscription expires (null for lifetime/unlimited)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    ///     Whether the subscription auto-renews
    /// </summary>
    public bool AutoRenew { get; set; } = true;

    /// <summary>
    ///     Billing interval (Monthly, Yearly, Lifetime)
    /// </summary>
    [MaxLength(50)]
    public string? BillingInterval { get; set; }

    /// <summary>
    ///     Monthly/Annual cost of the subscription
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal Cost { get; set; } = 0;

    /// <summary>
    ///     Currency code (USD, EUR, etc.)
    /// </summary>
    [MaxLength(3)]
    public string Currency { get; set; } = "USD";

    /// <summary>
    ///     External payment provider ID (Stripe, PayPal, etc.)
    /// </summary>
    [MaxLength(255)]
    public string? PaymentProviderId { get; set; }

    /// <summary>
    ///     Last payment date
    /// </summary>
    public DateTime? LastPaymentDate { get; set; }

    /// <summary>
    ///     Next billing date
    /// </summary>
    public DateTime? NextBillingDate { get; set; }

    /// <summary>
    ///     Additional subscription metadata (JSON)
    /// </summary>
    [Column(TypeName = "jsonb")]
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    ///     Activate the subscription
    /// </summary>
    public void Activate()
    {
        Status = "Active";
        Touch();
    }

    /// <summary>
    ///     Cancel the subscription
    /// </summary>
    public void Cancel()
    {
        Status = "Cancelled";
        AutoRenew = false;
        Touch();
    }

    /// <summary>
    ///     Suspend the subscription
    /// </summary>
    public void Suspend()
    {
        Status = "Suspended";
        Touch();
    }

    /// <summary>
    ///     Expire the subscription
    /// </summary>
    public void Expire()
    {
        Status = "Expired";
        Touch();
    }

    /// <summary>
    ///     Renew the subscription
    /// </summary>
    /// <param name="newExpiryDate">New expiration date</param>
    public void Renew(DateTime newExpiryDate)
    {
        Status = "Active";
        ExpiresAt = newExpiryDate;
        LastPaymentDate = DateTime.UtcNow;
        Touch();
    }

    /// <summary>
    ///     Update the plan
    /// </summary>
    /// <param name="newPlanId">New plan ID</param>
    /// <param name="newPlanName">New plan name</param>
    /// <param name="newCost">New cost</param>
    public void UpdatePlan(Guid newPlanId, string newPlanName, decimal newCost)
    {
        PlanId = newPlanId;
        PlanName = newPlanName;
        Cost = newCost;
        Touch();
    }

    /// <summary>
    ///     Checks if the subscription is currently active and not expired
    /// </summary>
    /// <returns>True if active and not expired</returns>
    public bool IsValid()
    {
        return Status == "Active" && (ExpiresAt == null || ExpiresAt > DateTime.UtcNow);
    }

    /// <summary>
    ///     Checks if the subscription is expiring soon (within specified days)
    /// </summary>
    /// <param name="days">Number of days threshold</param>
    /// <returns>True if expiring within the specified days</returns>
    public bool IsExpiringSoon(int days = 7)
    {
        if (ExpiresAt == null) return false;
        return ExpiresAt.Value <= DateTime.UtcNow.AddDays(days);
    }
}
