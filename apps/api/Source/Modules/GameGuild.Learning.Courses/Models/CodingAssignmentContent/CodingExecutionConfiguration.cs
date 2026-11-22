using System.Text.Json.Serialization;

namespace GameGuild.Learning.Courses;

/// <summary>Frozen technical configuration, stored in the existing coding JSON.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CodingExecutionConfiguration
{
    public const int MaxTimeLimitMs = 30_000;
    public const int MaxOutputLimitBytes = 2_000_000;
    public const string ArtifactVersion = "4.5.2";
    public const string RuntimeAbi = "emception-browser-v1";
    public const string ToolchainLockHash = "405e9f1fdd936ba31e11247fa1f901eaee24c4fcf641e343505df75c3d539b37";

    public required CodingExecutionLimits Limits { get; init; }
    public required CodingExecutionEntryPoint EntryPoint { get; init; }
    public required CodingExecutionToolchain Toolchain { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CodingExecutionLimits
{
    public required int TimeLimitMs { get; init; }
    public required int OutputLimitBytes { get; init; }
    public required int MaxFileCount { get; init; }

    /// <summary>Retained for explicit rejection, never claimed as supported enforcement.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? MemoryLimitBytes { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CodingExecutionEntryPoint
{
    public required string Path { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Symbol { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CodingExecutionToolchain
{
    public required string Key { get; init; }
    public required string Version { get; init; }
    public required string RuntimeAbi { get; init; }
    public required string ArtifactDigest { get; init; }
}
