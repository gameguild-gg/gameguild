using GameGuild.Common.Domain.Enums;
using GameGuild.Modules.Payments.Models;
using GameGuild.Data;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Modules.Payments.Services;

/// <summary>
/// Service implementation for managing payments and financial transactions
/// </summary>
public class PaymentService(ApplicationDbContext context) : IPaymentService
{
    public async Task<IEnumerable<UserFinancialMethod>> GetUserPaymentMethodsAsync(Guid userId)
    {
        return await context.UserFinancialMethods
            .Where(pm => pm.UserId == userId && !pm.IsDeleted)
            .OrderByDescending(pm => pm.IsDefault)
            .ThenByDescending(pm => pm.CreatedAt)
            .ToListAsync();
    }

    public async Task<UserFinancialMethod?> GetPaymentMethodByIdAsync(Guid id, Guid userId)
    {
        return await context.UserFinancialMethods
            .FirstOrDefaultAsync(pm => pm.Id == id && pm.UserId == userId && !pm.IsDeleted);
    }

    public async Task<UserFinancialMethod> CreatePaymentMethodAsync(Guid userId, CreatePaymentMethodDto createDto)
    {
        // If this is set as default, unset all other default methods
        if (createDto.IsDefault)
        {
            var existingDefaults = await context.UserFinancialMethods
                .Where(pm => pm.UserId == userId && pm.IsDefault)
                .ToListAsync();
            
            foreach (var method in existingDefaults)
            {
                method.IsDefault = false;
            }
        }

        var paymentMethod = new UserFinancialMethod
        {
            UserId = userId,
            Type = Enum.Parse<PaymentMethodType>(createDto.Type, true),
            ExternalId = createDto.ExternalId,
            DisplayName = createDto.DisplayName,
            LastFour = createDto.LastFourDigits,
            Brand = createDto.Brand,
            ExpiryMonth = createDto.ExpiryMonth?.ToString(),
            ExpiryYear = createDto.ExpiryYear?.ToString(),
            IsDefault = createDto.IsDefault,
            IsActive = true
        };

        context.UserFinancialMethods.Add(paymentMethod);
        await context.SaveChangesAsync();
        return paymentMethod;
    }

    public async Task<bool> DeletePaymentMethodAsync(Guid id, Guid userId)
    {
        var paymentMethod = await context.UserFinancialMethods
            .FirstOrDefaultAsync(pm => pm.Id == id && pm.UserId == userId);

        if (paymentMethod == null)
        {
            return false;
        }

        // Soft delete
        paymentMethod.DeletedAt = DateTime.UtcNow;
        
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<FinancialTransaction>> GetUserTransactionsAsync(Guid userId, int skip = 0, int take = 50, TransactionType? type = null, TransactionStatus? status = null)
    {
        var query = context.FinancialTransactions
            .Where(t => t.FromUserId == userId || t.ToUserId == userId)
            .AsQueryable();

        if (type.HasValue)
        {
            query = query.Where(t => t.Type == type.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status.Value);
        }

        return await query
            .Skip(skip)
            .Take(take)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<FinancialTransaction?> GetTransactionByIdAsync(Guid id, Guid userId)
    {
        return await context.FinancialTransactions
            .FirstOrDefaultAsync(t => t.Id == id && (t.FromUserId == userId || t.ToUserId == userId));
    }

    public async Task<FinancialTransaction> CreateTransactionAsync(Guid userId, CreateTransactionDto createDto)
    {
        var transaction = new FinancialTransaction
        {
            FromUserId = userId,
            ToUserId = createDto.ToUserId,
            Type = createDto.Type,
            Amount = createDto.Amount,
            Currency = createDto.Currency,
            Status = TransactionStatus.Pending,
            PaymentMethodId = createDto.PaymentMethodId,
            Description = createDto.Description,
            Metadata = createDto.Metadata
        };

        context.FinancialTransactions.Add(transaction);
        await context.SaveChangesAsync();
        return transaction;
    }

    public async Task<FinancialTransaction?> ProcessPaymentAsync(Guid transactionId, Guid userId, ProcessPaymentDto processDto)
    {
        var transaction = await context.FinancialTransactions
            .FirstOrDefaultAsync(t => t.Id == transactionId && t.FromUserId == userId);

        if (transaction == null)
        {
            return null;
        }

        transaction.PaymentMethodId = processDto.PaymentMethodId;
        transaction.ExternalTransactionId = processDto.ExternalTransactionId;
        transaction.Status = TransactionStatus.Processing;
        transaction.ProcessedAt = DateTime.UtcNow;
        
        // Add payment intent ID to metadata if provided
        if (!string.IsNullOrEmpty(processDto.PaymentIntentId))
        {
            var metadata = string.IsNullOrEmpty(transaction.Metadata) ? "{}" : transaction.Metadata;
            // You might want to use a proper JSON library here
            transaction.Metadata = metadata.TrimEnd('}') + $",\"paymentIntentId\":\"{processDto.PaymentIntentId}\"}}";
        }

        await context.SaveChangesAsync();
        return transaction;
    }

    public async Task<IEnumerable<FinancialTransaction>> GetAllTransactionsAsync(int skip = 0, int take = 50, TransactionType? type = null, TransactionStatus? status = null)
    {
        var query = context.FinancialTransactions.AsQueryable();

        if (type.HasValue)
        {
            query = query.Where(t => t.Type == type.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status.Value);
        }

        return await query
            .Skip(skip)
            .Take(take)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<PaymentStatisticsDto> GetPaymentStatisticsAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = context.FinancialTransactions.AsQueryable();

        if (fromDate.HasValue)
        {
            query = query.Where(t => t.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(t => t.CreatedAt <= toDate.Value);
        }

        var transactions = await query.ToListAsync();

        var successfulTransactions = transactions.Where(t => t.Status == TransactionStatus.Completed).ToList();

        return new PaymentStatisticsDto
        {
            TotalRevenue = successfulTransactions.Sum(t => t.Amount),
            TotalTransactions = transactions.Count,
            SuccessfulTransactions = successfulTransactions.Count,
            FailedTransactions = transactions.Count(t => t.Status == TransactionStatus.Failed),
            AverageTransactionAmount = successfulTransactions.Any() ? successfulTransactions.Average(t => t.Amount) : 0,
            FromDate = fromDate,
            ToDate = toDate
        };
    }

    public async Task<bool> RefundTransactionAsync(Guid transactionId, decimal? amount = null)
    {
        var transaction = await context.FinancialTransactions
            .FirstOrDefaultAsync(t => t.Id == transactionId);

        if (transaction == null || transaction.Status != TransactionStatus.Completed)
        {
            return false;
        }

        var refundAmount = amount ?? transaction.Amount;

        var refundTransaction = new FinancialTransaction
        {
            FromUserId = transaction.ToUserId,
            ToUserId = transaction.FromUserId,
            Type = TransactionType.Refund,
            Amount = refundAmount,
            Currency = transaction.Currency,
            Status = TransactionStatus.Completed,
            Description = $"Refund for transaction {transaction.Id}",
            ProcessedAt = DateTime.UtcNow
        };

        context.FinancialTransactions.Add(refundTransaction);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<FinancialTransaction?> UpdateTransactionStatusAsync(Guid transactionId, TransactionStatus status, string? reason = null)
    {
        var transaction = await context.FinancialTransactions
            .FirstOrDefaultAsync(t => t.Id == transactionId);

        if (transaction == null)
        {
            return null;
        }

        transaction.Status = status;
        
        if (status == TransactionStatus.Completed)
        {
            transaction.ProcessedAt = DateTime.UtcNow;
        }

        if (!string.IsNullOrEmpty(reason))
        {
            transaction.FailureReason = reason;
        }

        await context.SaveChangesAsync();
        return transaction;
    }
}
