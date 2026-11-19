namespace GameGuild.API.Core.Integration;

internal sealed class GradingAIReviewOptions
{
    public const string SectionName = "Grading:AIReview";
    public const string DefaultProviderKey = "gameguild-ai";
    public const string SupportedPolicyVersion = "v1";

    public bool Enabled { get; set; }
    public string ProviderKey { get; set; } = DefaultProviderKey;
    public string PolicyVersion { get; set; } = SupportedPolicyVersion;
    public string? Backend { get; set; }
    public string? Model { get; set; }
    public double Temperature { get; set; }
    public int MaxTokens { get; set; } = 2048;
}
