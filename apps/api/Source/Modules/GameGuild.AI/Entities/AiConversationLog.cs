namespace GameGuild.AI;

public sealed class AiConversationLog : EntityBase
{
    public Guid? UserId { get; set; }
    public string RequestKind { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string RequestText { get; set; } = string.Empty;
    public string? SystemPrompt { get; set; }
    public string? ResponseText { get; set; }
    public string Outcome { get; set; } = string.Empty;
    public string? OutcomeCode { get; set; }
    public string? OutcomeReason { get; set; }
    public string? FinishReason { get; set; }
    public int? InputTokens { get; set; }
    public int? OutputTokens { get; set; }
    public int? TotalTokens { get; set; }
    public DateTime OccurredAt { get; set; } = SystemClock.UtcNow;
}
