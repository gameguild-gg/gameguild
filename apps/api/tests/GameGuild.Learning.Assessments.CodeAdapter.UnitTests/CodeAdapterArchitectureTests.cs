using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using GameGuild.Learning.Assessments.Grading.Abstractions;
using GameGuild.Learning.Assessments.Grading.Capabilities;
using GameGuild.Learning.Assessments.Grading.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GameGuild.Learning.Assessments.CodeAdapter.Tests;

public sealed class CodeAdapterArchitectureTests
{
    [Fact]
    public void CoreAssembly_DoesNotReferenceOrContainCodeAdapterImplementations()
    {
        var coreAssembly = typeof(IAssessmentTypeAdapter).Assembly;

        coreAssembly.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Should().NotContain("GameGuild.Learning.Assessments.CodeAdapter");
        coreAssembly.GetTypes()
            .Where(type => type.Namespace?.Contains(".Grading.Code", StringComparison.Ordinal) == true)
            .Should().BeEmpty();
    }

    [Fact]
    public void Module_RegistersEachFrozenVersionExactlyOnce()
    {
        var services = new ServiceCollection();
        services.AddAssessmentsModule();
        services.AddCodeGradingAdapter();
        using var provider = services.BuildServiceProvider();

        var adapters = provider.GetServices<IAssessmentTypeAdapter>()
            .Where(adapter => adapter.ContentType == CodeAssessmentContracts.ContentType)
            .OrderBy(adapter => adapter.Version)
            .ToArray();
        var handlers = provider.GetServices<IReviewStageHandler>()
            .Where(handler => handler.Key == CodeAssessmentContracts.HandlerKey)
            .OrderBy(handler => handler.Version)
            .ToArray();

        adapters.Select(adapter => adapter.Version).Should().Equal("1", "2", "3");
        handlers.Select(handler => handler.Version).Should().Equal("1", "2", "3");
        adapters.Should().ContainSingle(adapter => adapter.IsCurrentForAuthoring && adapter.Version == "3");
    }

    [Fact]
    public void FrozenBackendFixture_MatchesRegisteredToolchainIdentities()
    {
        using var document = ReadFixture("GameGuild.Coding.Fixtures.code-adapter-cases-v1.json");
        var frozen = document.RootElement.GetProperty("frozenBackendAdapters").EnumerateArray().ToArray();

        frozen.Select(value => value.GetProperty("version").GetString()).Should().Equal("1", "2", "3");
        foreach (var entry in frozen)
        {
            var version = entry.GetProperty("version").GetString()!;
            var identity = CodeToolchainIdentity.ForAdapterVersion(version);
            entry.GetProperty("runtimeAbi").GetString().Should().Be(identity.RuntimeAbi);
            entry.GetProperty("toolchainLockHash").GetString().Should().Be(identity.ToolchainLockHash);
            if (entry.TryGetProperty("artifactVersion", out var artifactVersion))
            {
                artifactVersion.GetString().Should().Be(identity.ArtifactVersion);
            }
            else
            {
                entry.GetProperty("artifactVersions").EnumerateArray()
                    .Select(value => value.GetString())
                    .Should().Contain(identity.ArtifactVersion);
            }
        }
    }

    [Fact]
    public void VersionFourFixture_RemainsIndependentFromFrozenBackendSnapshots()
    {
        using var document = ReadFixture("GameGuild.Coding.Fixtures.coding-contract-cases-v1.json");
        var authoring = document.RootElement.GetProperty("authoring");

        authoring.GetProperty("schemaVersion").GetInt32().Should().Be(1);
        authoring.GetProperty("type").GetString().Should().Be(CodeAssessmentContracts.ContentType);
        CodeAssessmentContracts.IsSupportedVersion("4").Should().BeFalse(
            "the TypeScript v4 contract must not reinterpret frozen backend snapshots");
    }

    private static JsonDocument ReadFixture(string name)
    {
        var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"Embedded fixture {name} was not found.");
        return JsonDocument.Parse(stream);
    }
}
