using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using GameGuild.API.IntegrationTests.Infrastructure;
using GameGuild.Compliance.Audit;

namespace GameGuild.API.IntegrationTests;

/// <summary>
/// Comprehensive event coverage manifest (issue #181): the <c>/api/audit/events/comprehensive</c>
/// route must serve the module-by-module coverage sweep over the real API host — anonymous callers
/// are rejected, and an authenticated caller receives a complete manifest (every mutating command
/// carries a use-case event contract and exactly one handler, and every mutating endpoint dispatches
/// through the journaled CQRS pipeline). The versioned twin route and the issue #180 audit trail
/// manifest must agree.
/// </summary>
[Collection(ApiPostgreSqlCollection.Name)]
public sealed class ComprehensiveEventCoverageHttpTests(ApiPostgreSqlFixture fixture)
{
    [Fact]
    public async Task Anonymous_callers_are_rejected()
    {
        using var client = fixture.Factory.CreateClient();
        using var response = await client.GetAsync("/api/audit/events/comprehensive");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("/api/audit/events/comprehensive")]
    [InlineData("/v1/audit/events/comprehensive")]
    public async Task Authenticated_callers_receive_the_complete_module_manifest(string route)
    {
        using var client = fixture.CreateAuthenticatedClient(Guid.NewGuid(), Guid.NewGuid());
        using var response = await client.GetAsync(route);
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);

        var report = await response.Content.ReadFromJsonAsync<ComprehensiveEventCoverageReport>();
        report.Should().NotBeNull();
        report!.IsComplete.Should().BeTrue("the composed module catalog reports no coverage gaps: {0}", string.Join("; ", report.Gaps));
        report.Gaps.Should().BeEmpty();
        report.ModuleCount.Should().BeGreaterThan(5);
        report.TotalMutatingCommands.Should().BePositive();
        report.TotalCommandContracts.Should().Be(report.TotalMutatingCommands,
            "every mutating command in the catalog carries a use-case event contract");
        report.TotalMutationEndpoints.Should().BePositive();
        report.Modules.Should().OnlyContain(module => module.IsFullyCovered);
        report.Modules.Should().OnlyContain(module => module.MutationEndpointsWithoutCommandDispatch.Count == 0,
            "a mutating endpoint outside the CQRS pipeline would bypass the journaled operation event");
    }

    [Fact]
    public async Task Manifest_includes_the_domain_modules_previously_flagged_as_uncovered()
    {
        using var client = fixture.CreateAuthenticatedClient(Guid.NewGuid(), Guid.NewGuid());
        using var response = await client.GetAsync("/api/audit/events/comprehensive");
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);

        var report = await response.Content.ReadFromJsonAsync<ComprehensiveEventCoverageReport>();
        var manifestModules = report!.Modules.Select(module => module.ModuleName).ToList();
        manifestModules.Should().Contain(
        [
            "Content.Pages",
            "Learning.Courses",
            "Learning.Enrollments",
            "Learning.Certificates",
            "Commerce.Billing",
            "Commerce.Payments",
            "Identity.Authentication",
            "Identity.Authorization",
        ], "the modules found without audit emission in earlier passes must appear in the manifest, each fully covered");
        report.Modules.Should().OnlyContain(module => module.IsFullyCovered);
    }

    [Fact]
    public async Task Audit_trail_coverage_manifest_agrees_that_coverage_is_complete()
    {
        using var client = fixture.CreateAuthenticatedClient(Guid.NewGuid(), Guid.NewGuid());
        using var response = await client.GetAsync("/api/audit/comprehensive-coverage");
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);

        var report = await response.Content.ReadFromJsonAsync<ComprehensiveAuditTrailCoverageReport>();
        report.Should().NotBeNull();
        report!.IsComplete.Should().BeTrue();
        report.Families.Should().OnlyContain(family => family.IsCovered);
        report.Families.Should().OnlyContain(family => family.ActionTypes.Count > 0);
        report.Families.Should().OnlyContain(family => family.Evidence.Count > 0);
    }
}
