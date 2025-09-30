namespace GameGuild.Configuration;

/// <summary> External IP service response format. </summary>
public enum ExternalIpResponseFormat
{
    /// <summary> Plain text response containing only the IP address. </summary>
    PlainText,

    /// <summary> JSON response containing the IP address in a specific field. </summary>
    Json,
}
