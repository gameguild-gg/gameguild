namespace GameGuild.API.Deployment;

/// <summary>
///     Immutable build metadata plus runtime deployment metadata for the active release.
/// </summary>
public sealed record ReleaseIdentity(
    string Version,
    string ReleaseSha,
    string SourceTree,
    string ImageDigest,
    string BuiltAt,
    string DeployedAt)
{
    private const string Unknown = "Unknown";

    public static ReleaseIdentity FromEnvironment(Func<string, string?>? readEnvironment = null)
    {
        readEnvironment ??= Environment.GetEnvironmentVariable;

        return new ReleaseIdentity(
            ReadFirst(readEnvironment, "VERSION", "GAMEGUILD_VERSION"),
            Read(readEnvironment, "RELEASE_SHA"),
            Read(readEnvironment, "SOURCE_TREE"),
            Read(readEnvironment, "IMAGE_DIGEST"),
            Read(readEnvironment, "BUILT_AT"),
            Read(readEnvironment, "DEPLOYED_AT"));
    }

    private static string Read(Func<string, string?> readEnvironment, string name)
    {
        var value = readEnvironment(name);
        return string.IsNullOrWhiteSpace(value) ? Unknown : value.Trim();
    }

    private static string ReadFirst(Func<string, string?> readEnvironment, params string[] names)
    {
        return names.Select(name => Read(readEnvironment, name)).FirstOrDefault(value => value != Unknown) ?? Unknown;
    }
}
