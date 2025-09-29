namespace GameGuild.Configuration;

/// <summary> Configuration for a single DNS record to update. </summary>
public class DnsRecordConfiguration
{
    /// <summary> DNS record type (A, AAAA, etc.). </summary>
    [Required]
    public string Type { get; set; } = "A";

    /// <summary> DNS record name (e.g., "api", "@", "subdomain"). </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary> TTL for the DNS record (default: 300 seconds). </summary>
    [Range(60, 86400)] // Between 1 minute and 24 hours
    public int Ttl { get; set; } = 300;

    /// <summary> Whether this record is proxied through Cloudflare (default: true). </summary>
    public bool Proxied { get; set; } = true;

    /// <summary> Validates the DNS record configuration. </summary>
    public bool IsValid() { return !string.IsNullOrWhiteSpace(Type) && !string.IsNullOrWhiteSpace(Name) && Ttl is >= 60 and <= 86400; }

    /// <summary> Gets validation error messages. </summary>
    public IEnumerable<string> GetValidationErrors()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Type)) errors.Add("Type is required");

        if (string.IsNullOrWhiteSpace(Name)) errors.Add("Name is required");

        if (Ttl is < 60 or > 86400) errors.Add("TTL must be between 60 and 86400 seconds");

        return errors;
    }
}