using System.Net.Mail;

namespace GameGuild;

/// <summary> Represents an email address value object with validation </summary>
public record EmailAddress
{
    // Private parameterless constructor for EF Core
    private EmailAddress() { Value = string.Empty; }

    public EmailAddress(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email address cannot be null or empty.", nameof(email));

        // Trim whitespace first, then validate
        string trimmedEmail = email.Trim();
        if (!IsValidEmail(trimmedEmail)) throw new ArgumentException("Invalid email address format.", nameof(email));

        Value = trimmedEmail.ToLowerInvariant();
    }

    public string Value { get; init; }

    public static implicit operator string(EmailAddress email) { return email.Value; }

    public static implicit operator EmailAddress(string email) { return new EmailAddress(email); }

    private static bool IsValidEmail(string email)
    {
        try
        {
            // Basic format validation first
            if (string.IsNullOrWhiteSpace(email)) return false;

            // Check for obvious issues that MailAddress might accept
            if (email.Contains(' ') ||
                email.StartsWith('@') ||
                email.EndsWith('@') ||
                email.Contains("..") ||
                email.EndsWith('.') ||
                !email.Contains('@') ||
                email.Count(c => c == '@') != 1)
            {
                return false;
            }

            // Split into local and domain parts
            var parts = email.Split('@');
            if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
            {
                return false;
            }

            var localPart = parts[0];
            var domainPart = parts[1];

            // Domain must contain at least one dot and not start/end with dot
            if (!domainPart.Contains('.') || domainPart.StartsWith('.') || domainPart.EndsWith('.'))
            {
                return false;
            }

            // Use MailAddress for final validation
            var addr = new MailAddress(email);
            return addr.Address == email;
        }
        catch { return false; }
    }

    public override string ToString() { return Value; }
}
