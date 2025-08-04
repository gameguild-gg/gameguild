using System.ComponentModel.DataAnnotations;

namespace GameGuild.Common.Configuration;

/// <summary>
/// Configuration options for Cloudflare Dynamic DNS service.
/// </summary>
public class CloudflareDynamicDnsOptions {
    public const string SectionName = "CloudflareDynamicDns";

    /// <summary>
    /// Cloudflare API token with Zone:Edit permissions.
    /// </summary>
    public string? ApiToken { get; set; }

    /// <summary>
    /// Zone ID for the domain in Cloudflare.
    /// </summary>
    public string? ZoneId { get; set; }

    /// <summary>
    /// Interval in minutes to check and update IP address (default: 5 minutes).
    /// </summary>
    [Range(1, 1440)] // Between 1 minute and 24 hours
    public int IntervalMinutes { get; set; } = 5;

    /// <summary>
    /// List of DNS records to update with the external IP.
    /// </summary>
    public List<DnsRecordConfiguration> DnsRecords { get; set; } = new();

    /// <summary>
    /// List of external IP detection services with failover support.
    /// </summary>
    public List<ExternalIpServiceConfiguration> ExternalIpServices { get; set; } = new() {
    new() { Url = "https://api.ipify.org", Name = "ipify", ResponseFormat = ExternalIpResponseFormat.PlainText },
    new() { Url = "https://checkip.amazonaws.com", Name = "AWS", ResponseFormat = ExternalIpResponseFormat.PlainText },
    new() { Url = "https://icanhazip.com", Name = "icanhazip", ResponseFormat = ExternalIpResponseFormat.PlainText },
    new() { Url = "https://ipecho.net/plain", Name = "ipecho", ResponseFormat = ExternalIpResponseFormat.PlainText },
    new() { Url = "https://httpbin.org/ip", Name = "httpbin", ResponseFormat = ExternalIpResponseFormat.Json, JsonPath = "origin" },
    new() { Url = "https://jsonip.com", Name = "jsonip", ResponseFormat = ExternalIpResponseFormat.Json, JsonPath = "ip" }
  };

    /// <summary>
    /// Maximum number of retry attempts across all services (default: 3).
    /// </summary>
    [Range(1, 10)]
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Timeout in seconds for HTTP requests.
    /// </summary>
    [Range(5, 300)]
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Whether the service is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Validates the configuration.
    /// </summary>
    public bool IsValid() {
        return !string.IsNullOrWhiteSpace(ApiToken) &&
               !string.IsNullOrWhiteSpace(ZoneId) &&
               DnsRecords.Any() &&
               DnsRecords.All(r => r.IsValid());
    }

    /// <summary>
    /// Gets validation error messages.
    /// </summary>
    public IEnumerable<string> GetValidationErrors() {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(ApiToken))
            errors.Add("CloudflareApiToken is required");

        if (string.IsNullOrWhiteSpace(ZoneId))
            errors.Add("CloudflareZoneId is required");

        if (!DnsRecords.Any())
            errors.Add("At least one DNS record must be configured in CloudflareDnsRecords");

        foreach (var (record, index) in DnsRecords.Select((r, i) => (r, i))) {
            if (!record.IsValid()) {
                errors.Add($"DNS record at index {index} is invalid: {string.Join(", ", record.GetValidationErrors())}");
            }
        }

        return errors;
    }
}

/// <summary>
/// Configuration for a single DNS record to update.
/// </summary>
public class DnsRecordConfiguration {
    /// <summary>
    /// DNS record type (A, AAAA, etc.).
    /// </summary>
    [Required]
    public string Type { get; set; } = "A";

    /// <summary>
    /// DNS record name (e.g., "api", "@", "subdomain").
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// TTL for the DNS record (default: 300 seconds).
    /// </summary>
    [Range(60, 86400)] // Between 1 minute and 24 hours
    public int Ttl { get; set; } = 300;

    /// <summary>
    /// Whether this record is proxied through Cloudflare (default: true).
    /// </summary>
    public bool Proxied { get; set; } = true;

    /// <summary>
    /// Validates the DNS record configuration.
    /// </summary>
    public bool IsValid() {
        return !string.IsNullOrWhiteSpace(Type) &&
               !string.IsNullOrWhiteSpace(Name) &&
               Ttl is >= 60 and <= 86400;
    }

    /// <summary>
    /// Gets validation error messages.
    /// </summary>
    public IEnumerable<string> GetValidationErrors() {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Type))
            errors.Add("Type is required");

        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("Name is required");

        if (Ttl is < 60 or > 86400)
            errors.Add("TTL must be between 60 and 86400 seconds");

        return errors;
    }
}

/// <summary>
/// External IP service response format.
/// </summary>
public enum ExternalIpResponseFormat {
    /// <summary>
    /// Plain text response containing only the IP address.
    /// </summary>
    PlainText,

    /// <summary>
    /// JSON response containing the IP address in a specific field.
    /// </summary>
    Json
}

/// <summary>
/// Configuration for an external IP detection service.
/// </summary>
public class ExternalIpServiceConfiguration {
    /// <summary>
    /// Service name for logging purposes.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Service URL.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Response format (PlainText or Json).
    /// </summary>
    public ExternalIpResponseFormat ResponseFormat { get; set; } = ExternalIpResponseFormat.PlainText;

    /// <summary>
    /// JSON path to extract IP address (only used for Json format).
    /// </summary>
    public string? JsonPath { get; set; }

    /// <summary>
    /// Timeout in seconds for this specific service (default: 10 seconds).
    /// </summary>
    [Range(1, 120)]
    public int TimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Whether this service is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Validates the service configuration.
    /// </summary>
    public bool IsValid() {
        if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Url))
            return false;

        if (ResponseFormat == ExternalIpResponseFormat.Json && string.IsNullOrWhiteSpace(JsonPath))
            return false;

        return Uri.TryCreate(Url, UriKind.Absolute, out _);
    }

    /// <summary>
    /// Gets validation error messages.
    /// </summary>
    public IEnumerable<string> GetValidationErrors() {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("Name is required");

        if (string.IsNullOrWhiteSpace(Url))
            errors.Add("Url is required");
        else if (!Uri.TryCreate(Url, UriKind.Absolute, out _))
            errors.Add("Url must be a valid absolute URI");

        if (ResponseFormat == ExternalIpResponseFormat.Json && string.IsNullOrWhiteSpace(JsonPath))
            errors.Add("JsonPath is required for Json response format");

        if (TimeoutSeconds is < 1 or > 120)
            errors.Add("TimeoutSeconds must be between 1 and 120");

        return errors;
    }
}
