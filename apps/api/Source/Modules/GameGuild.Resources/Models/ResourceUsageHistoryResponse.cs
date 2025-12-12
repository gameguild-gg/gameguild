namespace GameGuild.Resources.Models;

/// <summary>
///     Resource usage history response
/// </summary>
public class ResourceUsageHistoryResponse
{
    public string Period { get; set; } = string.Empty;

    public Dictionary<string, int> Usage { get; set; } = new Dictionary<string, int>();
}
