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
public sealed class EventCoverageSweepServiceTests
{
    private static readonly Lazy<ComprehensiveEventCoverageReport> Sweep =
        new(() => new EventCoverageSweepService().SweepEventCoverage());

    /// <summary>
    /// Every module project composed into the API application (issue #181's "all system modules"):
    /// the API project's module references plus the module projects that reach the application
    /// transitively. The sweep manifest must enumerate all of them — without this pin the sweep
    /// could stay green over a silently shrunk catalog.
    /// </summary>
    public static readonly string[] ComposedSystemModules =
    [
        "AI", "Analytics", "Announcements.Contracts", "Assets",
        "Commerce", "Commerce.Billing", "Commerce.Orders", "Commerce.Payments",
        "Commerce.Products", "Commerce.Subscriptions", "Compliance.Audit", "Compliance.Consent",
        "Compliance.FERPA", "Compliance.FinancialCrime", "Compliance.KYC", "Content.Pages",
        "Features", "Finance.Contracts", "Finance.Economy", "Finance.Economy.AdRewards",
        "Finance.Economy.AiCredits", "Finance.Economy.Bounties", "Finance.Economy.Marketplace", "Finance.Economy.Payouts",
        "Finance.Economy.Treasury", "GameJams", "Gamification.Achievements", "Identity.Authentication",
        "Identity.Authorization", "Identity.Context", "Identity.Provisioning", "Identity.Tenants",
        "Identity.Users", "LaunchPad", "Learning", "Learning.Assessments",
        "Learning.Assessments.QuizAdapter", "Learning.Certificates", "Learning.Cohorts", "Learning.Courses",
        "Learning.Enrollments", "Learning.Experience.Discovery", "Learning.Experience.LearningPaths", "Learning.Experience.Recommendations",
        "Learning.Experience.Social", "Learning.Lti", "Learning.TestingLab", "Learning.Workspaces",
        "Localization", "Monitoring.SLA", "Notifications", "ProjectWork",
        "Projects", "Resources", "Resources.Contents", "SharedKernel",
        "Social.Announcements", "Social.Assets", "Social.Blog", "Social.Feed",
        "Social.Follows", "Social.Groups", "Social.Posts", "Social.Profiles",
        "Social.Reactions", "Tags", "Teams", "TestingLab",
        "TrustSafety",
    ];

    [Fact]
    public void SweepEnumeratesTheLoadedModuleCatalog()
    {
        var report = Sweep.Value;

        report.ModuleCount.Should().BeGreaterThan(5, "the test host loads the full module graph through the API project reference");
        report.Modules.Should().OnlyContain(module => module.AssemblyName.StartsWith("GameGuild.", StringComparison.Ordinal));
    }

    [Fact]
    public void ManifestIncludesEveryComposedSystemModule()
    {
        var report = Sweep.Value;

        var manifestModules = report.Modules.Select(module => module.ModuleName).ToHashSet(StringComparer.Ordinal);
        var missing = ComposedSystemModules.Where(module => !manifestModules.Contains(module)).ToList();
        missing.Should().BeEmpty(
            "issue #181 requires coverage across ALL system modules; a composed module missing from the manifest means the sweep (and therefore the completeness proof) is blind to it. Missing: {0}",
            string.Join(", ", missing));
    }

    [Fact]
    public void EveryMutationEndpointDispatchesThroughTheCqrsPipeline()
    {
        var report = Sweep.Value;

        report.TotalMutationEndpoints.Should().BePositive("the composed catalog exposes mutating endpoints");
        report.Modules.Should().OnlyContain(module => module.MutationEndpointsWithoutCommandDispatch.Count == 0,
            "a mutating endpoint whose controller dispatches no command (ISender/IMediator) would let a user interaction bypass the journaled UseCaseOperationBehavior pipeline");
        report.Modules.SelectMany(module => module.MutationEndpointsWithoutCommandDispatch).Should().BeEmpty();
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
