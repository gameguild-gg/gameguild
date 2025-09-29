namespace GameGuild.Core.Middleware;

/// <summary>
/// Request information captured for logging.
/// </summary>
internal class RequestInfo {
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string QueryString { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long? ContentLength { get; set; }
    public Dictionary<string, string>? Headers { get; set; }
    public string? Body { get; set; }
}