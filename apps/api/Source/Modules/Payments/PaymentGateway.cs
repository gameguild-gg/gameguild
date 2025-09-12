namespace GameGuild.Common;

/// <summary>
/// Payment gateway provider enumeration
/// </summary>
public enum PaymentGateway {
  Stripe = 0,
  PayPal = 1,
  Square = 2,
  Authorize = 3,
  Braintree = 4,
  Mock = 99, // For testing
}
