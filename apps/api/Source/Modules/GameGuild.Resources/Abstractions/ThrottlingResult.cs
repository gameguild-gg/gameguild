using GameGuild.Resources.Entities;

namespace GameGuild.Resources.Abstractions;

/// <summary>
///     Result of applying a throttling policy
/// </summary>
public class ThrottlingResult
{
    public bool IsAllowed { get; set; }

    public int DelayMs { get; set; }

    public string? Reason { get; set; }

    public ThrottlingStrategy AppliedStrategy { get; set; }
}
