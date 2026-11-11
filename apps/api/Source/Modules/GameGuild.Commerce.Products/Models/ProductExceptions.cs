namespace GameGuild.Commerce.Products;

/// <summary>
/// Exception thrown when a product is not found.
/// Derives from <see cref="EntityNotFoundException"/> so the exception middleware
/// answers 404 instead of 500 (issue #1278 not-found→500 class).
/// </summary>
public class ProductNotFoundException : EntityNotFoundException
{
    public ProductNotFoundException(Guid productId)
        : base("Product", productId, $"Product with ID '{productId}' was not found.")
    {
        ProductId = productId;
    }

    public ProductNotFoundException(Guid productId, string message)
        : base("Product", productId, message)
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
/// Exception thrown when a promo code is not found.
/// Derives from <see cref="EntityNotFoundException"/> so the exception middleware
/// answers 404 instead of 500 (issue #1278 not-found→500 class).
/// </summary>
public class PromoCodeNotFoundException : EntityNotFoundException
{
    public PromoCodeNotFoundException(string code)
        : base("PromoCode", Guid.Empty, $"Promo code '{code}' was not found.")
    {
        Code = code;
    }

    public PromoCodeNotFoundException(Guid promoCodeId)
        : base("PromoCode", promoCodeId, $"Promo code with ID '{promoCodeId}' was not found.")
    {
        PromoCodeId = promoCodeId;
    }

    public string? Code { get; }
    public Guid? PromoCodeId { get; }
}

/// <summary>
/// Exception thrown when a promo code is invalid or cannot be applied.
/// Derives from <see cref="DomainException"/> so the exception middleware answers
/// 422 (domain rule violation) instead of 500.
/// </summary>
public class InvalidPromoCodeException : DomainException
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
