using GameGuild.Common;

namespace GameGuild.Modules.Payments;

/// <summary>
/// Payment request DTO for gateway processing
/// </summary>
public class PaymentRequest
{
    public required string PaymentMethodId { get; set; }
    public required decimal Amount { get; set; }
    public required string Currency { get; set; }
    public string? Description { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// Payment result from gateway processing
/// </summary>
public class PaymentResult
{
    public bool Success { get; set; }
    public string? TransactionId { get; set; }
    public string? PaymentIntentId { get; set; }
    public string? Error { get; set; }
    public decimal ProcessingFee { get; set; }
    public DateTime ProcessedAt { get; set; }
}

/// <summary>
/// Refund request DTO for gateway processing
/// </summary>
public class RefundRequest
{
    public required string TransactionId { get; set; }
    public required decimal Amount { get; set; }
    public string? Reason { get; set; }
}

/// <summary>
/// Refund result from gateway processing
/// </summary>
public class RefundResult
{
    public bool Success { get; set; }
    public string? RefundId { get; set; }
    public string? Error { get; set; }
    public DateTime ProcessedAt { get; set; }
}

/// <summary>
/// Interface for payment gateway services
/// </summary>
public interface IPaymentGatewayService
{
    /// <summary>
    /// Process a payment through the gateway
    /// </summary>
    Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request);
    
    /// <summary>
    /// Process a refund through the gateway
    /// </summary>
    Task<RefundResult> RefundPaymentAsync(RefundRequest request);
    
    /// <summary>
    /// Get supported payment gateway
    /// </summary>
    PaymentGateway Gateway { get; }
}
