using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GameGuild.Common.Entities;
using GameGuild.Common.Enums;


namespace GameGuild.Modules.Payment.Models;

[Table("user_financial_methods")]
[Index(nameof(UserId))]
[Index(nameof(Type))]
[Index(nameof(Status))]
[Index(nameof(IsDefault))]
[Index(nameof(ExternalId))]
public class UserFinancialMethod : BaseEntity {
  private Guid _userId;

  private PaymentMethodType _type;

  private string _name = string.Empty;

  private string? _externalId;

  private string? _lastFour;

  private string? _expiryMonth;

  private string? _expiryYear;

  private string? _brand;

  private PaymentMethodStatus _status = PaymentMethodStatus.Active;

  private bool _isDefault = false;

  private User.Models.User _user = null!;

  public Guid UserId {
    get => _userId;
    set => _userId = value;
  }

  public PaymentMethodType Type {
    get => _type;
    set => _type = value;
  }

  [Required]
  [MaxLength(255)]
  public string Name {
    get => _name;
    set => _name = value;
  }

  /// <summary>
  /// External ID from payment provider (Stripe, PayPal, etc.)
  /// </summary>
  [MaxLength(255)]
  public string? ExternalId {
    get => _externalId;
    set => _externalId = value;
  }

  /// <summary>
  /// Last 4 digits of card number or identifier for the method
  /// </summary>
  [MaxLength(10)]
  public string? LastFour {
    get => _lastFour;
    set => _lastFour = value;
  }

  /// <summary>
  /// Expiration month for cards (MM format)
  /// </summary>
  [MaxLength(2)]
  public string? ExpiryMonth {
    get => _expiryMonth;
    set => _expiryMonth = value;
  }

  /// <summary>
  /// Expiration year for cards (YYYY format)
  /// </summary>
  [MaxLength(4)]
  public string? ExpiryYear {
    get => _expiryYear;
    set => _expiryYear = value;
  }

  /// <summary>
  /// Card brand (Visa, Mastercard, etc.) or wallet type
  /// </summary>
  [MaxLength(50)]
  public string? Brand {
    get => _brand;
    set => _brand = value;
  }

  public PaymentMethodStatus Status {
    get => _status;
    set => _status = value;
  }

  /// <summary>
  /// Whether this is the default payment method for the user
  /// </summary>
  public bool IsDefault {
    get => _isDefault;
    set => _isDefault = value;
  }

  // Navigation properties
  [ForeignKey(nameof(UserId))]
  public virtual User.Models.User User {
    get => _user;
    set => _user = value;
  }
}

public class UserFinancialMethodConfiguration : IEntityTypeConfiguration<UserFinancialMethod> {
  public void Configure(EntityTypeBuilder<UserFinancialMethod> builder) {
    // Configure relationship with User (can't be done with annotations)
    builder.HasOne(ufm => ufm.User).WithMany().HasForeignKey(ufm => ufm.UserId).OnDelete(DeleteBehavior.Cascade);
  }
}
