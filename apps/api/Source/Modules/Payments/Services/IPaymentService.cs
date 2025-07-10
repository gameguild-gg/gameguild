using GameGuild.Common;
using GameGuild.Modules.Payments.Models;

namespace GameGuild.Modules.Payments.Services;

/// <summary>
/// Service interface for managing payments and financial transactions
/// </summary>
public interface IPaymentService
{
    // Payment Methods
    Task<IEnumerable<UserFinancialMethod>> GetUserPaymentMethodsAsync(Guid userId);
    Task<UserFinancialMethod?> GetPaymentMethodByIdAsync(Guid id, Guid userId);
    Task<UserFinancialMethod> CreatePaymentMethodAsync(Guid userId, CreatePaymentMethodDto createDto);
    Task<bool> DeletePaymentMethodAsync(Guid id, Guid userId);
    
    // Transactions
    Task<IEnumerable<FinancialTransaction>> GetUserTransactionsAsync(Guid userId, int skip = 0, int take = 50, TransactionType? type = null, TransactionStatus? status = null);
    Task<FinancialTransaction?> GetTransactionByIdAsync(Guid id, Guid userId);
    Task<FinancialTransaction> CreateTransactionAsync(Guid userId, CreateTransactionDto createDto);
    Task<FinancialTransaction?> ProcessPaymentAsync(Guid transactionId, Guid userId, ProcessPaymentDto processDto);
    
    // Admin functions
    Task<IEnumerable<FinancialTransaction>> GetAllTransactionsAsync(int skip = 0, int take = 50, TransactionType? type = null, TransactionStatus? status = null);
    Task<PaymentStatisticsDto> GetPaymentStatisticsAsync(DateTime? fromDate = null, DateTime? toDate = null);
    
    // Payment processing
    Task<bool> RefundTransactionAsync(Guid transactionId, decimal? amount = null);
    Task<FinancialTransaction?> UpdateTransactionStatusAsync(Guid transactionId, TransactionStatus status, string? reason = null);
}

/// <summary>
/// DTO for creating a payment method
/// </summary>
public class CreatePaymentMethodDto
{
    public string Type { get; set; } = string.Empty; // "card", "paypal", "crypto", etc.
    public string ExternalId { get; set; } = string.Empty; // External payment provider ID
    public string? DisplayName { get; set; }
    public string? LastFourDigits { get; set; }
    public string? Brand { get; set; } // "visa", "mastercard", etc.
    public int? ExpiryMonth { get; set; }
    public int? ExpiryYear { get; set; }
    public bool IsDefault { get; set; } = false;
}

/// <summary>
/// DTO for creating a transaction
/// </summary>
public class CreateTransactionDto
{
    public Guid? ToUserId { get; set; }
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public Guid? PaymentMethodId { get; set; }
    public string? Description { get; set; }
    public string? Metadata { get; set; } // JSON string for additional data
}

/// <summary>
/// DTO for processing a payment
/// </summary>
public class ProcessPaymentDto
{
    public Guid PaymentMethodId { get; set; }
    public string? ExternalTransactionId { get; set; }
    public string? PaymentIntentId { get; set; } // For Stripe integration
    public string? Metadata { get; set; }
}

/// <summary>
/// DTO for payment statistics
/// </summary>
public class PaymentStatisticsDto
{
    public decimal TotalRevenue { get; set; }
    public int TotalTransactions { get; set; }
    public int SuccessfulTransactions { get; set; }
    public int FailedTransactions { get; set; }
    public decimal AverageTransactionAmount { get; set; }
    public Dictionary<string, decimal> RevenueByMethod { get; set; } = new();
    public Dictionary<string, int> TransactionsByStatus { get; set; } = new();
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
