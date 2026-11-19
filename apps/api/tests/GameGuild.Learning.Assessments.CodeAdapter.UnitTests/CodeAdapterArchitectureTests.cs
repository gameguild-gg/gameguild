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
    public void Module_RegistersTheCanonicalVersionExactlyOnce()
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

        adapters.Select(adapter => adapter.Version).Should().Equal(CodeAssessmentContracts.Version);
        handlers.Select(handler => handler.Version).Should().Equal(CodeAssessmentContracts.Version);
        adapters.Should().ContainSingle(adapter => adapter.IsCurrentForAuthoring);
    }

    [Fact]
    public void CanonicalFixture_MatchesTheRegisteredToolchainIdentity()
    {
        using var document = ReadFixture("GameGuild.Coding.Fixtures.coding-contract-cases.json");
        var toolchain = document.RootElement.GetProperty("authoring").GetProperty("toolchain");
        var identity = CodeToolchainIdentity.Current;

        toolchain.GetProperty("version").GetString().Should().Be(identity.ArtifactVersion);
        toolchain.GetProperty("runtimeAbi").GetString().Should().Be(identity.RuntimeAbi);
        toolchain.GetProperty("artifactDigest").GetString().Should().Be("sha256:" + identity.ToolchainLockHash);
    }

    [Fact]
    public void CanonicalContractRejectsAnyOtherAdapterVersion()
    {
        using var document = ReadFixture("GameGuild.Coding.Fixtures.coding-contract-cases.json");
        var authoring = document.RootElement.GetProperty("authoring");

        authoring.GetProperty("schemaVersion").GetInt32().Should().Be(1);
        authoring.GetProperty("type").GetString().Should().Be(CodeAssessmentContracts.ContentType);
        CodeAssessmentContracts.IsSupportedVersion("1").Should().BeTrue();
        CodeAssessmentContracts.IsSupportedVersion("2").Should().BeFalse();
    }

    private static JsonDocument ReadFixture(string name)
    {
        var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"Embedded fixture {name} was not found.");
        return JsonDocument.Parse(stream);
    }
}
