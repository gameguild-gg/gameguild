namespace GameGuild.Configuration;

/// <summary> Configuration options for Cloudflare Dynamic DNS service. </summary>
public class CloudflareDynamicDnsOptions
{
    public const string SectionName = "CloudflareDynamicDns";

    /// <summary> Cloudflare API token with Zone:Edit permissions. </summary>
    public string? ApiToken { get; set; }

    /// <summary> Zone ID for the domain in Cloudflare. </summary>
    public string? ZoneId { get; set; }

    /// <summary> Interval in minutes to check and update IP address (default: 5 minutes). </summary>
    [Range(1, 1440)] // Between 1 minute and 24 hours
    public int IntervalMinutes { get; set; } = 5;

    /// <summary> List of DNS records to update with the external IP. </summary>
    public List<DnsRecordConfiguration> DnsRecords { get; set; } = [];

    /// <summary> List of external IP detection services with failover support. </summary>
    public List<ExternalIpServiceConfiguration> ExternalIpServices { get; set; } =
    [
        new ExternalIpServiceConfiguration { Url = "https://api.ipify.org", Name = "ipify", ResponseFormat = ExternalIpResponseFormat.PlainText },
        new ExternalIpServiceConfiguration { Url = "https://checkip.amazonaws.com", Name = "AWS", ResponseFormat = ExternalIpResponseFormat.PlainText },
        new ExternalIpServiceConfiguration { Url = "https://icanhazip.com", Name = "icanhazip", ResponseFormat = ExternalIpResponseFormat.PlainText },
        new ExternalIpServiceConfiguration { Url = "https://ipecho.net/plain", Name = "ipecho", ResponseFormat = ExternalIpResponseFormat.PlainText },
        new ExternalIpServiceConfiguration { Url = "https://httpbin.org/ip", Name = "httpbin", ResponseFormat = ExternalIpResponseFormat.Json, JsonPath = "origin" },
        new ExternalIpServiceConfiguration { Url = "https://jsonip.com", Name = "jsonip", ResponseFormat = ExternalIpResponseFormat.Json, JsonPath = "ip" },
    ];

    /// <summary> Maximum number of retry attempts across all services (default: 3). </summary>
    [Range(1, 10)]
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary> Timeout in seconds for HTTP requests. </summary>
    [Range(5, 300)]
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary> Whether the service is enabled. </summary>
    public bool Enabled { get; set; } = true;

    /// <summary> Validates the configuration. </summary>
    public bool IsValid() { return !string.IsNullOrWhiteSpace(ApiToken) && !string.IsNullOrWhiteSpace(ZoneId) && DnsRecords.Any() && DnsRecords.All(r => r.IsValid()); }

    /// <summary> Gets validation error messages. </summary>
    public IEnumerable<string> GetValidationErrors()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(ApiToken)) errors.Add("CloudflareApiToken is required");

        if (string.IsNullOrWhiteSpace(ZoneId)) errors.Add("CloudflareZoneId is required");

        if (!DnsRecords.Any()) errors.Add("At least one DNS record must be configured in CloudflareDnsRecords");

        foreach (var (record, index) in DnsRecords.Select((r, i) => (r, i)))
        {
            if (!record.IsValid()) { errors.Add($"DNS record at index {index} is invalid: {string.Join(", ", record.GetValidationErrors())}"); }
        }

        return errors;
    }
}