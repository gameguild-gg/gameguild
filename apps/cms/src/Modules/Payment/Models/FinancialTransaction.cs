using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GameGuild.Common.Entities;
using GameGuild.Common.Enums;
using GameGuild.Modules.Product.Models;

namespace GameGuild.Modules.Payment.Models;

[Table("financial_transactions")]
[Index(nameof(FromUserId))]
[Index(nameof(ToUserId))]
[Index(nameof(Type))]
[Index(nameof(Status))]
[Index(nameof(ExternalTransactionId))]
[Index(nameof(PaymentMethodId))]
[Index(nameof(ProcessedAt))]
[Index(nameof(Amount))]
public class FinancialTransaction : BaseEntity
{
    private Guid? _fromUserId;

    private Guid? _toUserId;

    private TransactionType _type;

    private decimal _amount;

    private string _currency = "USD";

    private TransactionStatus _status = TransactionStatus.Pending;

    private string? _externalTransactionId;

    private Guid? _paymentMethodId;

    private Guid? _promoCodeId;

    private decimal? _platformFee;

    private decimal? _processorFee;

    private decimal? _netAmount;

    private string? _description;

    private string? _metadata;

    private DateTime? _processedAt;

    private DateTime? _failedAt;

    private string? _errorMessage;

    private User.Models.User? _fromUser;

    private User.Models.User? _toUser;

    private UserFinancialMethod? _paymentMethod;

    private PromoCode? _promoCode;

    private ICollection<PromoCodeUse> _promoCodeUses = new List<Product.Models.PromoCodeUse>();

    /// <summary>
    /// User who initiated the transaction (payer)
    /// </summary>
    public Guid? FromUserId
    {
        get => _fromUserId;
        set => _fromUserId = value;
    }

    /// <summary>
    /// User who receives the transaction (payee)
    /// </summary>
    public Guid? ToUserId
    {
        get => _toUserId;
        set => _toUserId = value;
    }

    public TransactionType Type
    {
        get => _type;
        set => _type = value;
    }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Amount
    {
        get => _amount;
        set => _amount = value;
    }

    [MaxLength(3)]
    public string Currency
    {
        get => _currency;
        set => _currency = value;
    }

    public TransactionStatus Status
    {
        get => _status;
        set => _status = value;
    }

    /// <summary>
    /// External transaction ID from payment provider
    /// </summary>
    [MaxLength(255)]
    public string? ExternalTransactionId
    {
        get => _externalTransactionId;
        set => _externalTransactionId = value;
    }

    /// <summary>
    /// Payment method used for this transaction
    /// </summary>
    public Guid? PaymentMethodId
    {
        get => _paymentMethodId;
        set => _paymentMethodId = value;
    }

    /// <summary>
    /// Promo code applied to this transaction
    /// </summary>
    public Guid? PromoCodeId
    {
        get => _promoCodeId;
        set => _promoCodeId = value;
    }

    /// <summary>
    /// Platform fee charged for this transaction
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal? PlatformFee
    {
        get => _platformFee;
        set => _platformFee = value;
    }

    /// <summary>
    /// Payment processor fee for this transaction
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal? ProcessorFee
    {
        get => _processorFee;
        set => _processorFee = value;
    }

    /// <summary>
    /// Net amount after fees
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal? NetAmount
    {
        get => _netAmount;
        set => _netAmount = value;
    }

    /// <summary>
    /// Description or memo for the transaction
    /// </summary>
    [MaxLength(500)]
    public string? Description
    {
        get => _description;
        set => _description = value;
    }

    /// <summary>
    /// Additional metadata about the transaction
    /// </summary>
    [MaxLength(1000)]
    public string? Metadata
    {
        get => _metadata;
        set => _metadata = value;
    }

    /// <summary>
    /// Date when the transaction was processed
    /// </summary>
    public DateTime? ProcessedAt
    {
        get => _processedAt;
        set => _processedAt = value;
    }

    /// <summary>
    /// Date when the transaction failed (if applicable)
    /// </summary>
    public DateTime? FailedAt
    {
        get => _failedAt;
        set => _failedAt = value;
    }

    /// <summary>
    /// Error message if transaction failed
    /// </summary>
    [MaxLength(500)]
    public string? ErrorMessage
    {
        get => _errorMessage;
        set => _errorMessage = value;
    }

    // Navigation properties
    [ForeignKey(nameof(FromUserId))]
    public virtual User.Models.User? FromUser
    {
        get => _fromUser;
        set => _fromUser = value;
    }

    [ForeignKey(nameof(ToUserId))]
    public virtual User.Models.User? ToUser
    {
        get => _toUser;
        set => _toUser = value;
    }

    [ForeignKey(nameof(PaymentMethodId))]
    public virtual UserFinancialMethod? PaymentMethod
    {
        get => _paymentMethod;
        set => _paymentMethod = value;
    }

    [ForeignKey(nameof(PromoCodeId))]
    public virtual Product.Models.PromoCode? PromoCode
    {
        get => _promoCode;
        set => _promoCode = value;
    }

    public virtual ICollection<Product.Models.PromoCodeUse> PromoCodeUses
    {
        get => _promoCodeUses;
        set => _promoCodeUses = value;
    }
}

public class FinancialTransactionConfiguration : IEntityTypeConfiguration<FinancialTransaction>
{
    public void Configure(EntityTypeBuilder<FinancialTransaction> builder)
    {
        // Configure relationship with FromUser (can't be done with annotations)
        builder.HasOne(ft => ft.FromUser)
            .WithMany()
            .HasForeignKey(ft => ft.FromUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure relationship with ToUser (can't be done with annotations)
        builder.HasOne(ft => ft.ToUser)
            .WithMany()
            .HasForeignKey(ft => ft.ToUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure relationship with PaymentMethod (can't be done with annotations)
        builder.HasOne(ft => ft.PaymentMethod)
            .WithMany()
            .HasForeignKey(ft => ft.PaymentMethodId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure relationship with PromoCode (can't be done with annotations)
        builder.HasOne(ft => ft.PromoCode)
            .WithMany()
            .HasForeignKey(ft => ft.PromoCodeId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
