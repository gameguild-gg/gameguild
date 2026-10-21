using Asp.Versioning;
using GameGuild.CQRS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using GameGuild.Configuration.PresentationLayer.RateLimiting;

namespace GameGuild.Compliance.Audit;

/// <summary>
/// Comprehensive audit trail coverage report (issue #180): per-family coverage across authentication,
/// authorization, sessions, identity provider integration, lifecycle management and business functions,
/// with the action types and components that provide each family's coverage.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/audit/comprehensive-coverage")]
[Route("api/audit/comprehensive-coverage")]
[Tags("compliance/audit/coverage")]
[Authorize]
[EnableRateLimiting(RateLimitPolicies.Api)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class ComprehensiveAuditCoverageController(IEventCoverageSweepService sweep) : ControllerBase
{
    /// <summary>Per-family audit trail coverage with evidence; complete when every family reports coverage.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ComprehensiveAuditTrailCoverageReport), StatusCodes.Status200OK)]
    public ActionResult<ComprehensiveAuditTrailCoverageReport> GetCoverage() => Ok(sweep.BuildAuditTrailCoverage());
}

/// <summary>
/// Comprehensive event coverage report (issue #181): the module-by-module event coverage sweep over
/// every loaded GameGuild module — mutating commands, use-case event contracts, handlers and mutation
/// endpoints — with the aggregate gap list. Complete when no module reports a gap.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/audit/events/comprehensive")]
[Route("api/audit/events/comprehensive")]
[Tags("compliance/audit/coverage")]
[Authorize]
[EnableRateLimiting(RateLimitPolicies.Api)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class ComprehensiveEventCoverageController(IEventCoverageSweepService sweep) : ControllerBase
{
    /// <summary>Per-module event coverage manifest; complete when every mutating command carries a contract and a handler.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ComprehensiveEventCoverageReport), StatusCodes.Status200OK)]
    public ActionResult<ComprehensiveEventCoverageReport> GetCoverage() => Ok(sweep.SweepEventCoverage());
}
