namespace GameGuild.Configuration;

/// <summary> Configuration for an external IP detection service. </summary>
public class ExternalIpServiceConfiguration
{
    /// <summary> Service name for logging purposes. </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary> Service URL. </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary> Response format (PlainText or Json). </summary>
    public ExternalIpResponseFormat ResponseFormat { get; set; } = ExternalIpResponseFormat.PlainText;

    /// <summary> JSON path to extract IP address (only used for Json format). </summary>
    public string? JsonPath { get; set; }

    /// <summary> Timeout in seconds for this specific service (default: 10 seconds). </summary>
    [Range(1, 120)]
    public int TimeoutSeconds { get; set; } = 10;

    /// <summary> Whether this service is enabled. </summary>
    public bool Enabled { get; set; } = true;

    /// <summary> Validates the service configuration. </summary>
    public bool IsValid()
    {
        if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Url)) return false;

        if (ResponseFormat == ExternalIpResponseFormat.Json && string.IsNullOrWhiteSpace(JsonPath)) return false;

        return Uri.TryCreate(Url, UriKind.Absolute, out _);
    }

    /// <summary> Gets validation error messages. </summary>
    public IEnumerable<string> GetValidationErrors()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Name)) errors.Add("Name is required");

        if (string.IsNullOrWhiteSpace(Url))
            errors.Add("Url is required");
        else if (!Uri.TryCreate(Url, UriKind.Absolute, out _)) errors.Add("Url must be a valid absolute URI");

        if (ResponseFormat == ExternalIpResponseFormat.Json && string.IsNullOrWhiteSpace(JsonPath)) errors.Add("JsonPath is required for Json response format");

        if (TimeoutSeconds is < 1 or > 120) errors.Add("TimeoutSeconds must be between 1 and 120");

        return errors;
    }
}
