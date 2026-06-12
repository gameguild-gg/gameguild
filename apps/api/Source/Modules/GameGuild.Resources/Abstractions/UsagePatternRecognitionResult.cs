namespace GameGuild.Resources;

public sealed record UsagePatternRecognitionResult(string Pattern, double Confidence, string? Metadata = null);
