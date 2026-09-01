using FluentAssertions;
using GameGuild.API.Deployment;

namespace GameGuild.API.UnitTests.Controllers;

public sealed class ReleaseIdentityTests
{
    [Fact]
    public void FromEnvironment_ShouldReadImmutableBuildAndRuntimeMetadata()
    {
        var environment = new Dictionary<string, string?>
        {
            ["VERSION"] = "2.4.0",
            ["RELEASE_SHA"] = "release-sha",
            ["SOURCE_TREE"] = "tree-sha",
            ["IMAGE_DIGEST"] = "sha256:image",
            ["BUILT_AT"] = "2026-09-01T12:00:00Z",
            ["DEPLOYED_AT"] = "2026-09-01T12:05:00Z"
        };

        var identity = ReleaseIdentity.FromEnvironment(name => environment.GetValueOrDefault(name));

        identity.Version.Should().Be("2.4.0");
        identity.ReleaseSha.Should().Be("release-sha");
        identity.SourceTree.Should().Be("tree-sha");
        identity.ImageDigest.Should().Be("sha256:image");
        identity.BuiltAt.Should().Be("2026-09-01T12:00:00Z");
        identity.DeployedAt.Should().Be("2026-09-01T12:05:00Z");
    }

    [Fact]
    public void FromEnvironment_ShouldUseExplicitUnknownValuesWhenMetadataIsAbsent()
    {
        var identity = ReleaseIdentity.FromEnvironment(_ => null);

        identity.Version.Should().Be("Unknown");
        identity.ReleaseSha.Should().Be("Unknown");
        identity.SourceTree.Should().Be("Unknown");
        identity.ImageDigest.Should().Be("Unknown");
        identity.BuiltAt.Should().Be("Unknown");
        identity.DeployedAt.Should().Be("Unknown");
    }

    [Fact]
    public void FromEnvironment_ShouldUseImageVersionWhenRuntimeOverrideIsAbsent()
    {
        var environment = new Dictionary<string, string?> { ["GAMEGUILD_VERSION"] = "4.3.0" };

        var identity = ReleaseIdentity.FromEnvironment(name => environment.GetValueOrDefault(name));

        identity.Version.Should().Be("4.3.0");
    }
}
