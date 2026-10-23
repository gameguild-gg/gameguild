using FluentAssertions;
using GameGuild.Compliance.Audit;
using Xunit;

namespace GameGuild.Tests.Audit.Unit.Services;

/// <summary>
/// Comprehensive event coverage (issues #180/#181): the sweep reflects every loaded GameGuild
/// module, verifies each mutating command carries a use-case event contract and exactly one
/// handler, and the audit trail manifest reports coverage for every issue #180 family with action
/// types recognized by the platform taxonomy or a module emitter.
/// </summary>
public sealed class EventCoverageSweepTests
{
    private static readonly Lazy<ComprehensiveEventCoverageReport> Sweep =
        new(() => new EventCoverageSweepService().SweepEventCoverage());

    [Fact]
    public void SweepEnumeratesTheLoadedModuleCatalog()
    {
        var report = Sweep.Value;

        report.ModuleCount.Should().BeGreaterThan(5, "the test host loads the full module graph through the API project reference");
        report.Modules.Should().OnlyContain(module => module.AssemblyName.StartsWith("GameGuild.", StringComparison.Ordinal));
    }

    [Fact]
    public void EveryLoadedModuleReportsFullEventCoverage()
    {
        var report = Sweep.Value;

        report.Gaps.Should().BeEmpty(
            "GGARCH001/004/005 fail the build on a mutating command without a contract, without a handler, or with direct controller persistence; the sweep re-verifies the same invariants over the assemblies actually loaded");
        report.IsComplete.Should().BeTrue();
        report.Modules.Should().OnlyContain(module => module.IsFullyCovered);
    }

    [Fact]
    public void MutatingCommandsAndMutationEndpointsAreCounted()
    {
        var report = Sweep.Value;

        report.TotalMutatingCommands.Should().BePositive();
        report.TotalCommandContracts.Should().Be(report.TotalMutatingCommands);
        report.TotalMutationEndpoints.Should().BePositive();
    }

    [Fact]
    public void AuditTrailManifestCoversEveryIssue180Family()
    {
        var report = new EventCoverageSweepService().BuildAuditTrailCoverage();

        report.Families.Select(family => family.Family).Should().Contain(
        [
            "Authentication",
            "Authorization decisions",
            "Session management",
            "Identity provider integration",
            "User lifecycle",
            "Tenant lifecycle",
            "Role and permission lifecycle",
            "Financial transactions",
            "Content management",
            "Program management",
            "Certificate lifecycle",
        ]);
        report.Families.Should().OnlyContain(family => family.ActionTypes.Count > 0);
        report.Families.Should().OnlyContain(family => family.Evidence.Count > 0);
    }

    [Fact]
    public void AuditTrailManifestIsCompleteOverTheLoadedModuleCatalog()
    {
        var report = new EventCoverageSweepService().BuildAuditTrailCoverage();

        report.Families.Should().OnlyContain(family => family.IsCovered,
            "every manifest family must be backed by recognized action types and a gap-free module sweep");
        report.IsComplete.Should().BeTrue();
    }

    [Fact]
    public void ManifestActionTypesAreRecognized()
    {
        // Every action type a family promises must be known to the platform taxonomy or the
        // module-emitted literal catalog — the manifest cannot promise unclassified events.
        foreach (var (_, _, actionTypes, _) in EventCoverageSweepService.AuditTrailFamilies)
        {
            actionTypes.Should().NotBeEmpty();
            actionTypes.Should().OnlyContain(actionType =>
                EventCoverageSweepService.KnownModuleEmittedActionTypes.Contains(actionType)
                || SecurityEventTaxonomy.Classify(actionType, AuditCategory.General, success: true).Description != "Unclassified audit event.",
                "unrecognized action type would make the family unprovable");
        }
    }

    [Fact]
    public void IdpAndLifecycleActionTypesAreClassifiedByTheSecurityEventTaxonomy()
    {
        // Identity-provider integration (issue #180): federated sign-in events must flow through
        // the security event pipeline with a severity, not just be persisted as raw rows.
        SecurityEventTaxonomy.Classify(AuditActionTypes.ExternalLoginSucceeded, AuditCategory.Security, success: true)
            .Kind.Should().Be(SecurityEventKind.Authentication);
        SecurityEventTaxonomy.Classify(AuditActionTypes.ExternalLoginFailed, AuditCategory.Security, success: false)
            .Severity.Should().Be(AuditRiskLevel.High);
        SecurityEventTaxonomy.Classify(AuditActionTypes.FederatedSignInMfaDenied, AuditCategory.Security, success: false)
            .Severity.Should().Be(AuditRiskLevel.High);
    }
}
