namespace GameGuild;

/// <summary>
///     Represents a phone number value object with validation
/// </summary>
public record PhoneNumber {
  public PhoneNumber(string phoneNumber, string? countryCode = null) {
    if (string.IsNullOrWhiteSpace(phoneNumber)) throw new ArgumentException("Phone number cannot be null or empty.", nameof(phoneNumber));

    string cleanNumber = CleanPhoneNumber(phoneNumber);

    if (cleanNumber.StartsWith("+")) {
      // International format
      if (cleanNumber.Length < 8 || cleanNumber.Length > 15) throw new ArgumentException("Invalid phone number format.", nameof(phoneNumber));

      CountryCode = ExtractCountryCode(cleanNumber);
      NationalNumber = cleanNumber.Substring(CountryCode.Length);
    }
    else {
      // National format - use provided country code or default to US
      CountryCode = countryCode ?? "+1";
      NationalNumber = cleanNumber;

      if (NationalNumber.Length < 7 || NationalNumber.Length > 12) throw new ArgumentException("Invalid phone number format.", nameof(phoneNumber));
    }

    Value = CountryCode + NationalNumber;
  }

  public string Value { get; }

  public string CountryCode { get; }

  public string NationalNumber { get; }

  public static implicit operator string(PhoneNumber phone) { return phone.Value; }

  private static string CleanPhoneNumber(string phoneNumber) {
    // Remove all non-digit characters except + at the beginning
    string cleaned = phoneNumber.Trim();
    var result = "";

    for (var i = 0; i < cleaned.Length; i++) {
      char c = cleaned[i];

      if (char.IsDigit(c) || i == 0 && c == '+') { result += c; }
    }

    return result;
  }

  private static string ExtractCountryCode(string internationalNumber) {
    // Simple country code extraction - in a real app, you'd use a library like libphonenumber
    if (internationalNumber.StartsWith("+1")) return "+1";
    if (internationalNumber.StartsWith("+44")) return "+44";
    if (internationalNumber.StartsWith("+49")) return "+49";
    if (internationalNumber.StartsWith("+33")) return "+33";
    if (internationalNumber.StartsWith("+55")) return "+55";

    // Default to first 3 characters for unknown codes
    return internationalNumber.Length >= 3 ? internationalNumber.Substring(0, 3) : internationalNumber;
  }

  public string GetDisplayFormat() {
    return CountryCode switch {
      "+1" when NationalNumber.Length == 10 => $"({NationalNumber.Substring(0, 3)}) {NationalNumber.Substring(3, 3)}-{NationalNumber.Substring(6)}", "+44" => $"{CountryCode} {NationalNumber}", _ => $"{CountryCode} {NationalNumber}"
    };
  }

  public override string ToString() { return GetDisplayFormat(); }
}
