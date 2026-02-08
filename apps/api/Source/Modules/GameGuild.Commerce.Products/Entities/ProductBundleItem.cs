using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Products;

/// <summary>
///     Represents a product included in a bundle with type-safe referential integrity.
///     Replaces the JSON string BundleItems field for better data integrity and queryability.
/// </summary>
[Table("product_bundle_items")]
[Index(nameof(BundleProductId), nameof(IncludedProductId), IsUnique = true)]
[Index(nameof(BundleProductId))]
[Index(nameof(IncludedProductId))]
public class ProductBundleItem : EntityBase
{
    /// <summary>
    ///     Private constructor for EF Core
    /// </summary>
    private ProductBundleItem() { }

    /// <summary>
    ///     The product that IS the bundle (parent)
    /// </summary>
    [Required]
    public Guid BundleProductId { get; private set; }

    /// <summary>
    ///     Navigation property to the bundle product
    /// </summary>
    [ForeignKey(nameof(BundleProductId))]
    public virtual Product BundleProduct { get; private set; } = null!;

    /// <summary>
    ///     The product included in the bundle (child)
    /// </summary>
    [Required]
    public Guid IncludedProductId { get; private set; }

    /// <summary>
    ///     Navigation property to the included product
    /// </summary>
    [ForeignKey(nameof(IncludedProductId))]
    public virtual Product IncludedProduct { get; private set; } = null!;

    /// <summary>
    ///     Quantity of this product included in the bundle
    /// </summary>
    public int Quantity { get; private set; } = 1;

    /// <summary>
    ///     Display order within the bundle
    /// </summary>
    public int DisplayOrder { get; private set; }

    /// <summary>
    ///     Whether this item is required for the bundle or optional
    /// </summary>
    public bool IsRequired { get; private set; } = true;

    /// <summary>
    ///     Optional discount percentage for this item when purchased as part of bundle
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal? BundleDiscountPercentage { get; private set; }

    /// <summary>
    ///     Creates a new bundle item relationship
    /// </summary>
    /// <param name="bundleProductId">The bundle product ID</param>
    /// <param name="includedProductId">The product to include in the bundle</param>
    /// <param name="quantity">Quantity included</param>
    /// <param name="displayOrder">Display order</param>
    /// <param name="isRequired">Whether required in bundle</param>
    /// <param name="tenantId">Tenant ID</param>
    /// <returns>New bundle item</returns>
    /// <exception cref="ArgumentException">If product IDs are invalid or the same</exception>
    public static ProductBundleItem Create(
        Guid bundleProductId,
        Guid includedProductId,
        int quantity = 1,
        int displayOrder = 0,
        bool isRequired = true,
        Guid? tenantId = null)
    {
        if (bundleProductId == Guid.Empty)
            throw new ArgumentException("Bundle product ID is required", nameof(bundleProductId));

        if (includedProductId == Guid.Empty)
            throw new ArgumentException("Included product ID is required", nameof(includedProductId));

        if (bundleProductId == includedProductId)
            throw new ArgumentException("A product cannot include itself in a bundle", nameof(includedProductId));

        if (quantity < 1)
            throw new ArgumentException("Quantity must be at least 1", nameof(quantity));

        return new ProductBundleItem
        {
            Id = Guid.NewGuid(),
            BundleProductId = bundleProductId,
            IncludedProductId = includedProductId,
            Quantity = quantity,
            DisplayOrder = displayOrder,
            IsRequired = isRequired,
            TenantId = tenantId
        };
    }

    /// <summary>
    ///     Updates the quantity of this item in the bundle
    /// </summary>
    public void SetQuantity(int quantity)
    {
        if (quantity < 1)
            throw new ArgumentException("Quantity must be at least 1", nameof(quantity));

        Quantity = quantity;
        Touch();
    }

    /// <summary>
    ///     Updates the display order
    /// </summary>
    public void SetDisplayOrder(int order)
    {
        DisplayOrder = order;
        Touch();
    }

    /// <summary>
    ///     Sets whether this item is required in the bundle
    /// </summary>
    public void SetRequired(bool isRequired)
    {
        IsRequired = isRequired;
        Touch();
    }

    /// <summary>
    ///     Sets a bundle-specific discount for this item
    /// </summary>
    public void SetBundleDiscount(decimal? discountPercentage)
    {
        if (discountPercentage.HasValue && (discountPercentage.Value < 0 || discountPercentage.Value > 100))
            throw new ArgumentOutOfRangeException(nameof(discountPercentage), "Discount must be between 0 and 100");

        BundleDiscountPercentage = discountPercentage;
        Touch();
    }
}
