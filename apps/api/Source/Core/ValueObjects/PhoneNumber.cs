namespace GameGuild;

/// <summary>
///     Represents a phone number value object with validation
/// </summary>
public record PhoneNumber {
  // Private parameterless constructor for EF Core
  private PhoneNumber() {
    Value = string.Empty;
    CountryCode = string.Empty;
    NationalNumber = string.Empty;
  }

  public PhoneNumber(string phoneNumber, string? countryCode = null) {
    if (string.IsNullOrWhiteSpace(phoneNumber)) throw new ArgumentException("Phone number cannot be null or empty.", nameof(phoneNumber));

    var cleanNumber = CleanPhoneNumber(phoneNumber);

    if (cleanNumber.StartsWith("+")) {
      // International format
      if (cleanNumber.Length is < 8 or > 15) throw new ArgumentException("Invalid phone number format.", nameof(phoneNumber));

      CountryCode = ExtractCountryCode(cleanNumber);
      NationalNumber = cleanNumber[CountryCode.Length..];
    }
    else {
      // National format - use provided country code or default to US
      CountryCode = countryCode ?? "+1";
      NationalNumber = cleanNumber;

      if (NationalNumber.Length is < 7 or > 12) throw new ArgumentException("Invalid phone number format.", nameof(phoneNumber));
    }

    Value = CountryCode + NationalNumber;
  }

  public string Value { get; init; }

  public string CountryCode { get; init; }

  public string NationalNumber { get; init; }

  public static implicit operator string(PhoneNumber phone) { return phone.Value; }

  // Factory method for EF Core that doesn't use optional parameters
  public static PhoneNumber FromString(string phoneNumber) {
    return new PhoneNumber(phoneNumber, null);
  }

  private static string CleanPhoneNumber(string phoneNumber) {
    // Remove all non-digit characters except + at the beginning
    var cleaned = phoneNumber.Trim();

    return cleaned.Where((c, i) => char.IsDigit(c) || i == 0 && c == '+').Aggregate("", (current, c) => current + c);
  }

  private static string ExtractCountryCode(string internationalNumber) {
    // Simple country code extraction - in a real app, you'd use a library like libphonenumber
    if (internationalNumber.StartsWith("+1")) return "+1";
    if (internationalNumber.StartsWith("+44")) return "+44";
    if (internationalNumber.StartsWith("+49")) return "+49";
    if (internationalNumber.StartsWith("+33")) return "+33";
    if (internationalNumber.StartsWith("+55")) return "+55";

    // Default to first 3 characters for unknown codes
    return internationalNumber.Length >= 3 ? internationalNumber[..3] : internationalNumber;
  }

  public string GetDisplayFormat() {
    return CountryCode switch {
      "+1" when NationalNumber.Length == 10 => $"({NationalNumber[..3]}) {NationalNumber.Substring(3, 3)}-{NationalNumber[6..]}",
      "+44" => $"{CountryCode} {NationalNumber}",
      _ => $"{CountryCode} {NationalNumber}"
    };
  }

  public override string ToString() { return GetDisplayFormat(); }
}
