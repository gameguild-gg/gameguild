namespace GameGuild.Commerce.Products;

/// <summary>
/// Exception thrown when a product is not found
/// </summary>
public class ProductNotFoundException : Exception
{
    public ProductNotFoundException(Guid productId)
        : base($"Product with ID '{productId}' was not found.")
    {
        ProductId = productId;
    }

    public ProductNotFoundException(Guid productId, string message)
        : base(message)
    {
        ProductId = productId;
    }

    public Guid ProductId { get; }
}

/// <summary>
/// Exception thrown when there's a concurrency conflict
/// </summary>
public class ConcurrencyException : Exception
{
    public ConcurrencyException(string message) : base(message) { }

    public ConcurrencyException(string message, Exception innerException)
        : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when a promo code is not found
/// </summary>
public class PromoCodeNotFoundException : Exception
{
    public PromoCodeNotFoundException(string code)
        : base($"Promo code '{code}' was not found.")
    {
        Code = code;
    }

    public PromoCodeNotFoundException(Guid promoCodeId)
        : base($"Promo code with ID '{promoCodeId}' was not found.")
    {
        PromoCodeId = promoCodeId;
    }

    public string? Code { get; }
    public Guid? PromoCodeId { get; }
}

/// <summary>
/// Exception thrown when a promo code is invalid or cannot be applied
/// </summary>
public class InvalidPromoCodeException : Exception
{
    public InvalidPromoCodeException(string code, string reason)
        : base($"Promo code '{code}' is invalid: {reason}")
    {
        Code = code;
        Reason = reason;
    }

    public string Code { get; }
    public string Reason { get; }
}
