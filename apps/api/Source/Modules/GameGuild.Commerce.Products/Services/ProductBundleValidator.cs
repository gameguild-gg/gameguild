namespace GameGuild.Commerce.Products;

/// <summary>
///     Service for validating product bundle configurations.
///     Extracts bundle validation logic from the Product entity (SOLID-1).
/// </summary>
public interface IProductBundleValidator
{
    /// <summary>
    ///     Validates that a product can be added to a bundle.
    /// </summary>
    /// <param name="bundleProduct">The bundle product</param>
    /// <param name="productToInclude">The product to add to the bundle</param>
    /// <returns>Validation result</returns>
    BundleValidationResult ValidateAddToBundle(Product bundleProduct, Product productToInclude);

    /// <summary>
    ///     Validates the entire bundle configuration.
    /// </summary>
    /// <param name="bundleProduct">The bundle product</param>
    /// <param name="includedProducts">Products included in the bundle</param>
    /// <returns>Validation result</returns>
    BundleValidationResult ValidateBundleConfiguration(Product bundleProduct, IEnumerable<Product> includedProducts);

    /// <summary>
    ///     Checks for circular references in bundle hierarchy.
    /// </summary>
    /// <param name="bundleProduct">The bundle being checked</param>
    /// <param name="allProducts">All products to check against</param>
    /// <returns>True if circular reference detected</returns>
    bool HasCircularReference(Product bundleProduct, IEnumerable<Product> allProducts);
}

/// <summary>
///     Result of bundle validation operation.
/// </summary>
public sealed class BundleValidationResult
{
    public bool IsValid { get; init; }
    public string? ErrorMessage { get; init; }
    public BundleValidationErrorCode? ErrorCode { get; init; }

    private BundleValidationResult() { }

    public static BundleValidationResult Success() => new() { IsValid = true };

    public static BundleValidationResult Failure(string errorMessage, BundleValidationErrorCode errorCode) =>
        new() { IsValid = false, ErrorMessage = errorMessage, ErrorCode = errorCode };
}

/// <summary>
///     Error codes for bundle validation failures.
/// </summary>
public enum BundleValidationErrorCode
{
    NotABundle,
    ProductAlreadyInBundle,
    CannotIncludeSelf,
    CircularReference,
    MaxBundleSizeExceeded,
    InvalidProductType,
    ProductNotFound,
    BundleCannotContainBundles
}

/// <summary>
///     Implementation of product bundle validation.
/// </summary>
public sealed class ProductBundleValidator : IProductBundleValidator
{
    /// <summary>
    ///     Maximum number of products allowed in a bundle.
    /// </summary>
    public const int MaxBundleSize = 50;

    /// <inheritdoc />
    public BundleValidationResult ValidateAddToBundle(Product bundleProduct, Product productToInclude)
    {
        if (!bundleProduct.IsBundle)
        {
            return BundleValidationResult.Failure(
                "Cannot add items to a non-bundle product",
                BundleValidationErrorCode.NotABundle);
        }

        if (bundleProduct.Id == productToInclude.Id)
        {
            return BundleValidationResult.Failure(
                "A product cannot include itself in a bundle",
                BundleValidationErrorCode.CannotIncludeSelf);
        }

        if (bundleProduct.BundleItems.Any(bi => bi.IncludedProductId == productToInclude.Id))
        {
            return BundleValidationResult.Failure(
                $"Product '{productToInclude.Name}' is already in this bundle",
                BundleValidationErrorCode.ProductAlreadyInBundle);
        }

        if (productToInclude.IsBundle)
        {
            return BundleValidationResult.Failure(
                "Bundles cannot contain other bundles to prevent nested complexity",
                BundleValidationErrorCode.BundleCannotContainBundles);
        }

        if (bundleProduct.BundleItems.Count >= MaxBundleSize)
        {
            return BundleValidationResult.Failure(
                $"Bundle cannot contain more than {MaxBundleSize} products",
                BundleValidationErrorCode.MaxBundleSizeExceeded);
        }

        return BundleValidationResult.Success();
    }

    /// <inheritdoc />
    public BundleValidationResult ValidateBundleConfiguration(
        Product bundleProduct,
        IEnumerable<Product> includedProducts)
    {
        if (!bundleProduct.IsBundle)
        {
            return BundleValidationResult.Failure(
                "Product is not configured as a bundle",
                BundleValidationErrorCode.NotABundle);
        }

        var products = includedProducts.ToList();

        if (products.Count > MaxBundleSize)
        {
            return BundleValidationResult.Failure(
                $"Bundle cannot contain more than {MaxBundleSize} products",
                BundleValidationErrorCode.MaxBundleSizeExceeded);
        }

        // Check for self-reference
        if (products.Any(p => p.Id == bundleProduct.Id))
        {
            return BundleValidationResult.Failure(
                "Bundle cannot contain itself",
                BundleValidationErrorCode.CannotIncludeSelf);
        }

        // Check for nested bundles
        var nestedBundle = products.FirstOrDefault(p => p.IsBundle);
        if (nestedBundle != null)
        {
            return BundleValidationResult.Failure(
                $"Bundle cannot contain another bundle ('{nestedBundle.Name}')",
                BundleValidationErrorCode.BundleCannotContainBundles);
        }

        return BundleValidationResult.Success();
    }

    /// <inheritdoc />
    public bool HasCircularReference(Product bundleProduct, IEnumerable<Product> allProducts)
    {
        if (!bundleProduct.IsBundle)
            return false;

        var visited = new HashSet<Guid>();
        var productMap = allProducts.ToDictionary(p => p.Id);

        return HasCircularReferenceRecursive(bundleProduct.Id, visited, productMap);
    }

    private static bool HasCircularReferenceRecursive(
        Guid currentId,
        HashSet<Guid> visited,
        Dictionary<Guid, Product> productMap)
    {
        if (!visited.Add(currentId))
            return true; // Already visited = circular reference

        if (!productMap.TryGetValue(currentId, out var product))
            return false;

        if (!product.IsBundle)
            return false;

        foreach (var bundleItem in product.BundleItems)
        {
            if (HasCircularReferenceRecursive(bundleItem.IncludedProductId, visited, productMap))
                return true;
        }

        visited.Remove(currentId); // Backtrack for other paths
        return false;
    }
}
