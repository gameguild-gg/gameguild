using HotChocolate;
using HotChocolate.Types;
using MediatR;
using GameGuild.Modules.Payments.Commands;
using GameGuild.Modules.Payments.Models;
using GameGuild.Common.Interfaces;

namespace GameGuild.Modules.Payments.GraphQL;

/// <summary>
/// GraphQL mutations for payment operations
/// </summary>
[ExtendObjectType("Mutation")]
public class PaymentMutations
{
    /// <summary>
    /// Create a new payment intent
    /// </summary>
    public async Task<CreatePaymentPayload> CreatePaymentAsync(
        CreatePaymentInput input,
        [Service] IMediator mediator,
        [Service] IUserContext userContext,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new CreatePaymentCommand
            {
                UserId = input.UserId ?? userContext.UserId ?? Guid.Empty,
                ProductId = input.ProductId,
                Amount = input.Amount,
                Currency = input.Currency ?? "USD",
                Method = input.Method,
                Description = input.Description,
                TenantId = input.TenantId,
                Metadata = input.Metadata
            };

            var result = await mediator.Send(command, cancellationToken);

            return new CreatePaymentPayload
            {
                Success = result.Success,
                Payment = result.Payment,
                ErrorMessage = result.ErrorMessage,
                ClientSecret = result.ClientSecret
            };
        }
        catch (Exception ex)
        {
            return new CreatePaymentPayload
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Process a payment (mark as succeeded)
    /// </summary>
    public async Task<ProcessPaymentPayload> ProcessPaymentAsync(
        ProcessPaymentInput input,
        [Service] IMediator mediator,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new ProcessPaymentCommand
            {
                PaymentId = input.PaymentId,
                ProviderTransactionId = input.ProviderTransactionId,
                ProviderMetadata = input.ProviderMetadata
            };

            var result = await mediator.Send(command, cancellationToken);

            return new ProcessPaymentPayload
            {
                Success = result.Success,
                Payment = result.Payment,
                ErrorMessage = result.ErrorMessage,
                AutoEnrollTriggered = result.AutoEnrollTriggered
            };
        }
        catch (Exception ex)
        {
            return new ProcessPaymentPayload
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Refund a payment
    /// </summary>
    public async Task<RefundPaymentPayload> RefundPaymentAsync(
        RefundPaymentInput input,
        [Service] IMediator mediator,
        [Service] IUserContext userContext,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new RefundPaymentCommand
            {
                PaymentId = input.PaymentId,
                RefundAmount = input.RefundAmount,
                Reason = input.Reason ?? "Customer requested refund",
                RefundedBy = userContext.UserId ?? Guid.Empty
            };

            var result = await mediator.Send(command, cancellationToken);

            return new RefundPaymentPayload
            {
                Success = result.Success,
                Refund = result.Refund,
                ErrorMessage = result.ErrorMessage
            };
        }
        catch (Exception ex)
        {
            return new RefundPaymentPayload
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Cancel a payment
    /// </summary>
    public async Task<CancelPaymentPayload> CancelPaymentAsync(
        CancelPaymentInput input,
        [Service] IMediator mediator,
        [Service] IUserContext userContext,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new CancelPaymentCommand
            {
                PaymentId = input.PaymentId,
                Reason = input.Reason ?? "Payment cancelled by user",
                CancelledBy = userContext.UserId ?? Guid.Empty
            };

            var result = await mediator.Send(command, cancellationToken);

            return new CancelPaymentPayload
            {
                Success = result.Success,
                Payment = result.Payment,
                ErrorMessage = result.ErrorMessage
            };
        }
        catch (Exception ex)
        {
            return new CancelPaymentPayload
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}

/// <summary>
/// Input types for payment mutations
/// </summary>
public record CreatePaymentInput
{
    public Guid? UserId { get; init; }
    public Guid? ProductId { get; init; }
    public decimal Amount { get; init; }
    public string? Currency { get; init; }
    public PaymentMethod Method { get; init; }
    public string? Description { get; init; }
    public Guid? TenantId { get; init; }
    public IDictionary<string, object>? Metadata { get; init; }
}

public record ProcessPaymentInput
{
    public Guid PaymentId { get; init; }
    public string ProviderTransactionId { get; init; } = string.Empty;
    public IDictionary<string, object>? ProviderMetadata { get; init; }
}

public record RefundPaymentInput
{
    public Guid PaymentId { get; init; }
    public decimal? RefundAmount { get; init; }
    public string? Reason { get; init; }
}

public record CancelPaymentInput
{
    public Guid PaymentId { get; init; }
    public string? Reason { get; init; }
}

/// <summary>
/// Payload types for payment mutations
/// </summary>
public record CreatePaymentPayload
{
    public bool Success { get; init; }
    public Payment? Payment { get; init; }
    public string? ErrorMessage { get; init; }
    public string? ClientSecret { get; init; }
}

public record ProcessPaymentPayload
{
    public bool Success { get; init; }
    public Payment? Payment { get; init; }
    public string? ErrorMessage { get; init; }
    public bool AutoEnrollTriggered { get; init; }
}

public record RefundPaymentPayload
{
    public bool Success { get; init; }
    public PaymentRefund? Refund { get; init; }
    public string? ErrorMessage { get; init; }
}

public record CancelPaymentPayload
{
    public bool Success { get; init; }
    public Payment? Payment { get; init; }
    public string? ErrorMessage { get; init; }
}
