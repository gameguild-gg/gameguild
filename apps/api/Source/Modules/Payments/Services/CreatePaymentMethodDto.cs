namespace GameGuild.Modules.Payments;

/// <summary>
/// DTO for creating a payment method
/// </summary>
public class CreatePaymentMethodDto {
  public string Type { get; set; } = string.Empty; // "card", "paypal", "crypto", etc.

  public string ExternalId { get; set; } = string.Empty; // External payment provider ID

  public string? DisplayName { get; set; }

  public string? LastFourDigits { get; set; }

  public string? Brand { get; set; } // "visa", "mastercard", etc.

  public int? ExpiryMonth { get; set; }

  public int? ExpiryYear { get; set; }

  public bool IsDefault { get; set; } = false;
}
