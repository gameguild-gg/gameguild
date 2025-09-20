using System.Net.Mail;


namespace GameGuild;

/// <summary> Represents an email address value object with validation </summary>
public record EmailAddress {
  // Private parameterless constructor for EF Core
  private EmailAddress() { Value = string.Empty; }

  public EmailAddress(string email) {
    if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email address cannot be null or empty.", nameof(email));

    if (!IsValidEmail(email)) throw new ArgumentException("Invalid email address format.", nameof(email));

    Value = email.ToLowerInvariant().Trim();
  }

  public string Value { get; init; }

  public static implicit operator string(EmailAddress email) { return email.Value; }

  public static implicit operator EmailAddress(string email) { return new EmailAddress(email); }

  private static bool IsValidEmail(string email) {
    try {
      var addr = new MailAddress(email);

      return addr.Address == email;
    }
    catch { return false; }
  }

  public override string ToString() { return Value; }
}
