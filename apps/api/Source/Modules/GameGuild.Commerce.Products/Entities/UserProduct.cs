using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Identity.Users;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Junction entity representing the relationship between a User and a Product
/// </summary>
[Table("user_products")]
[Index(nameof(UserId), nameof(ProductId), IsUnique = true)]
[Index(nameof(UserId))]
[Index(nameof(ProductId))]
[Index(nameof(AccessStatus))]
[Index(nameof(AcquisitionType))]
[Index(nameof(AccessEndDate))]
[Index(nameof(SubscriptionId))]
public class UserProduct : EntityBase
{
    /// <summary>Default constructor</summary>
    public UserProduct() { }

    /// <summary>Constructor for partial initialization</summary>
    /// <param name="partial">Partial user product data</param>
    public UserProduct(object partial) : base(partial) { }

    /// <summary>Foreign key to the User entity</summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>Navigation property to the User entity</summary>
    public virtual User User { get; set; } = null!;

    /// <summary>Foreign key to the Product entity</summary>
    [Required]
    public Guid ProductId { get; set; }

    /// <summary>Navigation property to the Product entity (may be null if not loaded)</summary>
    public virtual Product? Product { get; set; }

    /// <summary>Foreign key to the Subscription entity (optional)</summary>
    public Guid? SubscriptionId { get; set; }

    // Note: Subscription navigation property removed to avoid circular dependency
    // Access via SubscriptionId when needed

    /// <summary>How the user acquired this product</summary>
    public ProductAcquisitionType AcquisitionType { get; set; }

    /// <summary>Current access status for this product</summary>
    public ProductAccessStatus AccessStatus { get; set; } = ProductAccessStatus.Active;

    /// <summary>Amount the user paid for this product</summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal PricePaid { get; set; }

    /// <summary>Currency code for the price paid</summary>
    [MaxLength(3)]
    public string Currency { get; set; } = "USD";

    /// <summary>When the user's access to this product starts</summary>
    public DateTime? AccessStartDate { get; set; }

    /// <summary>When the user's access to this product ends</summary>
    public DateTime? AccessEndDate { get; set; }

    /// <summary>User who gifted this product (if acquisition type is Gift)</summary>
    public Guid? GiftedByUserId { get; set; }

    /// <summary>Navigation property to the user who gifted this product</summary>
    public virtual User? GiftedByUser { get; set; }

    /// <summary>Foreign key to the Order that created this entitlement</summary>
    public Guid? OrderId { get; set; }

    /// <summary>Current subscription status (for subscription products)</summary>
    public EntitlementSubscriptionStatus? SubscriptionStatus { get; set; }

    /// <summary>External subscription provider reference</summary>
    [MaxLength(200)]
    public string? SubscriptionProviderReference { get; set; }

    /// <summary>When the current subscription period started</summary>
    public DateTime? CurrentPeriodStart { get; set; }

    /// <summary>When the current subscription period ends</summary>
    public DateTime? CurrentPeriodEnd { get; set; }

    /// <summary>Whether subscription is set to cancel at period end</summary>
    public bool CancelAtPeriodEnd { get; set; }

    /// <summary>Reason for revocation if access was revoked</summary>
    [MaxLength(500)]
    public string? RevocationReason { get; set; }

    /// <summary>Check if the user currently has active access to this product</summary>
    public bool HasActiveAccess()
    {
        if (AccessStatus != ProductAccessStatus.Active) return false;

        var now = SystemClock.UtcNow;
        if (AccessStartDate.HasValue && AccessStartDate.Value > now)
            return false;

        if (AccessEndDate.HasValue && AccessEndDate.Value <= now)
            return false;

        return true;
    }

    /// <summary>Grant access to the product</summary>
    public void GrantAccess(DateTime? endDate = null, decimal? pricePaid = null, string? currency = null, ProductAcquisitionType? acquisitionType = null)
    {
        AccessStatus = ProductAccessStatus.Active;
        AccessStartDate ??= SystemClock.UtcNow;
        AccessEndDate = endDate;
        if (pricePaid.HasValue) PricePaid = pricePaid.Value;
        if (!string.IsNullOrEmpty(currency)) Currency = currency;
        if (acquisitionType.HasValue) AcquisitionType = acquisitionType.Value;
        Touch();
    }

    /// <summary>Revoke access to the product</summary>
    public void RevokeAccess(string? reason = null)
    {
        AccessStatus = ProductAccessStatus.Revoked;
        AccessEndDate = SystemClock.UtcNow;
        RevocationReason = reason;
        if (SubscriptionStatus.HasValue)
        {
            SubscriptionStatus = EntitlementSubscriptionStatus.Cancelled;
        }
        Touch();
    }

    /// <summary>Create a new UserProduct entitlement</summary>
    public static UserProduct Create(
        Guid userId,
        Guid productId,
        ProductAcquisitionType acquisitionType,
        decimal pricePaid = 0,
        string currency = "USD",
        DateTime? expiresAt = null,
        Guid? tenantId = null)
    {
        return new UserProduct
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ProductId = productId,
            AcquisitionType = acquisitionType,
            AccessStatus = ProductAccessStatus.Active,
            PricePaid = pricePaid,
            Currency = currency,
            AccessStartDate = SystemClock.UtcNow,
            AccessEndDate = expiresAt,
            TenantId = tenantId
        };
    }
}
