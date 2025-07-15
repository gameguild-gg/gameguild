using MediatR;
using Microsoft.EntityFrameworkCore;
using GameGuild.Database;
using GameGuild.Modules.Payments.Commands;
using GameGuild.Modules.Payments.Models;
using GameGuild.Common.Interfaces;

namespace GameGuild.Modules.Payments.Handlers;

/// <summary>
/// Handler for creating payment intents
/// </summary>
public class CreatePaymentCommandHandler : IRequestHandler<CreatePaymentCommand, CreatePaymentResult>
{
    private readonly ApplicationDbContext _context;
    private readonly IUserContext _userContext;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<CreatePaymentCommandHandler> _logger;

    public CreatePaymentCommandHandler(
        ApplicationDbContext context,
        IUserContext userContext,
        ITenantContext tenantContext,
        ILogger<CreatePaymentCommandHandler> logger)
    {
        _context = context;
        _userContext = userContext;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<CreatePaymentResult> Handle(CreatePaymentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate user authorization
            if (_userContext.UserId != request.UserId && !_userContext.IsInRole("Admin"))
            {
                return new CreatePaymentResult
                {
                    Success = false,
                    ErrorMessage = "Unauthorized to create payment for this user"
                };
            }

            // Validate product exists if specified
            if (request.ProductId.HasValue)
            {
                var productExists = await _context.Products
                    .AnyAsync(p => p.Id == request.ProductId.Value, cancellationToken);

                if (!productExists)
                {
                    return new CreatePaymentResult
                    {
                        Success = false,
                        ErrorMessage = "Product not found"
                    };
                }
            }

            // Create payment entity
            var payment = new Payment
            {
                UserId = request.UserId,
                ProductId = request.ProductId,
                Amount = request.Amount,
                Currency = request.Currency,
                Method = request.Method,
                Status = PaymentStatus.Pending,
                Metadata = System.Text.Json.JsonSerializer.Serialize(request.Metadata ?? new Dictionary<string, object>())
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Payment intent created: {PaymentId} for user {UserId}", payment.Id, request.UserId);

            return new CreatePaymentResult
            {
                Success = true,
                Payment = payment,
                ClientSecret = $"pi_{payment.Id}_secret" // Placeholder for actual payment provider integration
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment for user {UserId}", request.UserId);
            return new CreatePaymentResult
            {
                Success = false,
                ErrorMessage = "Failed to create payment"
            };
        }
    }
}

/// <summary>
/// Handler for processing payments
/// </summary>
public class ProcessPaymentCommandHandler : IRequestHandler<ProcessPaymentCommand, ProcessPaymentResult>
{
    private readonly ApplicationDbContext _context;
    private readonly IMediator _mediator;
    private readonly ILogger<ProcessPaymentCommandHandler> _logger;

    public ProcessPaymentCommandHandler(
        ApplicationDbContext context,
        IMediator mediator,
        ILogger<ProcessPaymentCommandHandler> logger)
    {
        _context = context;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<ProcessPaymentResult> Handle(ProcessPaymentCommand request, CancellationToken cancellationToken)
    {
        try
        {        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.Id == request.PaymentId, cancellationToken);

            if (payment == null)
            {
                return new ProcessPaymentResult
                {
                    Success = false,
                    ErrorMessage = "Payment not found"
                };
            }

            // Update payment status
            payment.Status = PaymentStatus.Completed;
            payment.ProviderTransactionId = request.ProviderTransactionId;
            payment.ProcessedAt = DateTime.UtcNow;

            if (request.ProviderMetadata != null)
            {
                payment.Metadata = System.Text.Json.JsonSerializer.Serialize(request.ProviderMetadata);
            }

            await _context.SaveChangesAsync(cancellationToken);

            // Trigger auto-enrollment if product has associated programs
            bool autoEnrollTriggered = false;
            if (payment.ProductId.HasValue)
            {
                // Get programs associated with the product
                var productPrograms = await _context.ProductPrograms
                    .Include(pp => pp.Program)
                    .Where(pp => pp.ProductId == payment.ProductId.Value && !pp.Program.IsDeleted)
                    .ToListAsync(cancellationToken);

                if (productPrograms.Any() && !string.IsNullOrEmpty(payment.UserId))
                {
                    foreach (var productProgram in productPrograms)
                    {
                        // Check if user is not already enrolled
                        var existingEnrollment = await _context.ProgramUsers
                            .AnyAsync(pu => pu.ProgramId == productProgram.ProgramId && 
                                          pu.UserId == payment.UserId && 
                                          pu.IsActive, cancellationToken);

                        if (!existingEnrollment && productProgram.Program.IsEnrollmentOpen)
                        {
                            var enrollment = new GameGuild.Modules.Programs.Models.ProgramUser
                            {
                                ProgramId = productProgram.ProgramId,
                                UserId = payment.UserId,
                                EnrollmentDate = DateTime.UtcNow,
                                IsActive = true,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            };

                            _context.ProgramUsers.Add(enrollment);
                            autoEnrollTriggered = true;
                            
                            _logger.LogInformation("Auto-enrolled user {UserId} in program {ProgramId} via payment {PaymentId}", 
                                payment.UserId, productProgram.ProgramId, payment.Id);
                        }
                    }
                    
                    // Save the auto-enrollments
                    if (autoEnrollTriggered)
                    {
                        await _context.SaveChangesAsync(cancellationToken);
                    }
                }
            }

            _logger.LogInformation("Payment processed: {PaymentId}", payment.Id);

            return new ProcessPaymentResult
            {
                Success = true,
                Payment = payment,
                AutoEnrollTriggered = autoEnrollTriggered
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment {PaymentId}", request.PaymentId);
            return new ProcessPaymentResult
            {
                Success = false,
                ErrorMessage = "Failed to process payment"
            };
        }
    }
}

/// <summary>
/// Handler for refunding payments
/// </summary>
public class RefundPaymentCommandHandler : IRequestHandler<RefundPaymentCommand, RefundPaymentResult>
{
    private readonly ApplicationDbContext _context;
    private readonly IUserContext _userContext;
    private readonly ILogger<RefundPaymentCommandHandler> _logger;

    public RefundPaymentCommandHandler(
        ApplicationDbContext context,
        IUserContext userContext,
        ILogger<RefundPaymentCommandHandler> logger)
    {
        _context = context;
        _userContext = userContext;
        _logger = logger;
    }

    public async Task<RefundPaymentResult> Handle(RefundPaymentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var payment = await _context.Payments
                .Include(p => p.Refunds)
                .FirstOrDefaultAsync(p => p.Id == request.PaymentId, cancellationToken);

            if (payment == null)
            {
                return new RefundPaymentResult
                {
                    Success = false,
                    ErrorMessage = "Payment not found"
                };
            }

            // Check authorization
            if (!_userContext.IsInRole("Admin") && payment.UserId != _userContext.UserId)
            {
                return new RefundPaymentResult
                {
                    Success = false,
                    ErrorMessage = "Unauthorized to refund this payment"
                };
            }

            // Calculate refund amount
            var totalRefunded = payment.Refunds.Where(r => r.Status == RefundStatus.Succeeded).Sum(r => r.RefundAmount);
            var refundAmount = request.RefundAmount ?? (payment.Amount - totalRefunded);

            if (refundAmount <= 0 || totalRefunded + refundAmount > payment.Amount)
            {
                return new RefundPaymentResult
                {
                    Success = false,
                    ErrorMessage = "Invalid refund amount"
                };
            }

            // Create refund record
            var refund = new PaymentRefund
            {
                PaymentId = payment.Id,
                ExternalRefundId = $"rfnd_{Guid.NewGuid():N}",
                RefundAmount = refundAmount,
                Reason = request.Reason,
                Status = RefundStatus.Succeeded,
                ProcessedAt = DateTime.UtcNow
            };

            _context.PaymentRefunds.Add(refund);

            // Update payment status
            var newTotalRefunded = totalRefunded + refundAmount;
            payment.Status = newTotalRefunded >= payment.Amount ? PaymentStatus.Refunded : PaymentStatus.PartiallyRefunded;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Payment refunded: {PaymentId}, Amount: {RefundAmount}", payment.Id, refundAmount);

            return new RefundPaymentResult
            {
                Success = true,
                Refund = refund
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refunding payment {PaymentId}", request.PaymentId);
            return new RefundPaymentResult
            {
                Success = false,
                ErrorMessage = "Failed to process refund"
            };
        }
    }
}
