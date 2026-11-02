using GameGuild.CQRS;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;

namespace GameGuild.Identity.Authentication;

// ============================================================================
// CONDITIONAL POLICY QUERY HANDLERS (issue #1203)
//
// The read side of ConditionalPolicyCrudController and the analytics routes of
// ConditionalPolicyEvaluationController previously had query records without
// handlers, so the endpoints failed at dispatch time with 500 "Handler not
// found" (docs/authorization/cqrs-wiring-matrix.md §2.2/§2.3). These handlers
// back them with the Authorization module's production policy store
// (IConditionalPolicyRepository), the same store the conditional evaluator
// reads at evaluation time.
//
// Data-source honesty: policy inventory fields (counts, condition-type
// breakdown, conflict analysis) are computed from the policy store.
// Per-policy evaluation telemetry is not recorded anywhere yet — the
// conditional evaluator does not write an evaluation log — so
// evaluation-count, timing and history fields return zero/empty until that
// telemetry exists.
//
// Guards: shared with the ABAC query handlers (AbacPolicyQueryGuards) —
// authenticated actor, non-system actors pinned to their own tenant scope.
// ============================================================================

/// <summary>Concrete conditional statistics envelope.</summary>
public sealed class ConditionalPolicyStatisticsResult : ConditionalPolicyStatisticsDto
{
}

/// <summary>Concrete conditional per-policy usage envelope.</summary>
public sealed class ConditionalPolicyUsageResult : ConditionalPolicyUsageDto
{
}

/// <summary>Concrete conditional evaluation-history envelope.</summary>
public sealed class ConditionalPolicyEvaluationHistoryResult : ConditionalPolicyEvaluationHistoryDto
{
}

/// <summary>Concrete conditional conflicts envelope.</summary>
public sealed class ConditionalPolicyConflictsResult : ConditionalPolicyConflictsDto
{
}

/// <summary>Concrete conditional template-catalog envelope.</summary>
public sealed class ConditionalPolicyTemplateResult : ConditionalPolicyTemplateDto
{
}

/// <summary>Concrete condition-type catalog envelope.</summary>
public sealed class PolicyConditionTypeResult : PolicyConditionTypeDto
{
}

/// <summary>
///     Handler for <see cref="GetConditionalPolicyQuery"/> (issue #1203): loads one
///     conditional policy from the production policy store. Unknown ids fail with 404;
///     reading another tenant's policy fails closed with 403.
/// </summary>
public sealed class GetConditionalPolicyQueryHandler(
    IConditionalPolicyRepository conditionalPolicyRepository,
    IActorContextAccessor actorContextAccessor)
    : IQueryHandler<GetConditionalPolicyQuery, ConditionalPolicy>
{
    private ActorContext Actor => actorContextAccessor.ActorContext;

    public async Task<ConditionalPolicy> Handle(GetConditionalPolicyQuery request, CancellationToken cancellationToken)
    {
        AbacPolicyQueryGuards.EnsureAuthenticated(Actor);

        var policy = await conditionalPolicyRepository.GetByIdAsync(request.PolicyId, cancellationToken).ConfigureAwait(false)
            ?? throw new EntityNotFoundException("ConditionalPolicy", request.PolicyId);

        AbacPolicyQueryGuards.EnsureCanReadPolicy(Actor, policy.TenantId);

        return policy;
    }
}

/// <summary>
///     Handler for <see cref="GetConditionalPoliciesQuery"/> (issue #1203): lists the
///     conditional policies in the caller's tenant scope (system administrators may
///     target any tenant or the global scope), applying the optional active and
///     condition-type filters in memory before page-based paging.
/// </summary>
public sealed class GetConditionalPoliciesQueryHandler(
    IConditionalPolicyRepository conditionalPolicyRepository,
    IActorContextAccessor actorContextAccessor)
    : IQueryHandler<GetConditionalPoliciesQuery, PagedResult<ConditionalPolicy>>
{
    private ActorContext Actor => actorContextAccessor.ActorContext;

    public async Task<PagedResult<ConditionalPolicy>> Handle(GetConditionalPoliciesQuery request, CancellationToken cancellationToken)
    {
        AbacPolicyQueryGuards.EnsureAuthenticated(Actor);

        var tenantScope = AbacPolicyQueryGuards.ResolveTenantScope(Actor, request.TenantId);
        var policies = await conditionalPolicyRepository.GetByTenantAsync(tenantScope, cancellationToken).ConfigureAwait(false);

        var filtered = policies
            .Where(policy => request.IsActive is null || policy.IsEnabled == request.IsActive)
            .Where(policy => string.IsNullOrWhiteSpace(request.ConditionType)
                || policy.ConditionType.ToString().Equals(request.ConditionType.Trim(), StringComparison.OrdinalIgnoreCase))
            .OrderBy(policy => policy.Priority)
            .ThenBy(policy => policy.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var page = GetAbacPoliciesQueryHandler.NormalizePage(request.Page);
        var pageSize = GetAbacPoliciesQueryHandler.NormalizePageSize(request.PageSize);

        return PagedResult<ConditionalPolicy>.FromPage(
            filtered.Skip((page - 1) * pageSize).Take(pageSize),
            filtered.Count,
            page,
            pageSize);
    }
}

/// <summary>
///     Handler for <see cref="GetConditionalPolicyStatisticsQuery"/> (issue #1203):
///     aggregates the policy inventory for the tenant scope, including the real
///     condition-type breakdown. Evaluation counters are zero until per-policy
///     evaluation telemetry is recorded (see the file header).
/// </summary>
public sealed class GetConditionalPolicyStatisticsQueryHandler(
    IConditionalPolicyRepository conditionalPolicyRepository,
    IActorContextAccessor actorContextAccessor)
    : IQueryHandler<GetConditionalPolicyStatisticsQuery, ConditionalPolicyStatisticsDto>
{
    private ActorContext Actor => actorContextAccessor.ActorContext;

    public async Task<ConditionalPolicyStatisticsDto> Handle(GetConditionalPolicyStatisticsQuery request, CancellationToken cancellationToken)
    {
        AbacPolicyQueryGuards.EnsureAuthenticated(Actor);

        var tenantScope = AbacPolicyQueryGuards.ResolveTenantScope(Actor, request.TenantId);
        var policies = await conditionalPolicyRepository.GetByTenantAsync(tenantScope, cancellationToken).ConfigureAwait(false);

        var result = new ConditionalPolicyStatisticsResult
        {
            TenantId = tenantScope,
            FromDate = request.FromDate,
            ToDate = request.ToDate,
            TotalPolicies = policies.Count,
            ActivePolicies = policies.Count(policy => policy.IsEnabled),
        };

        foreach (var group in policies.GroupBy(policy => policy.ConditionType.ToString()))
        {
            result.PoliciesByConditionType[group.Key] = group.Count();
        }

        return result;
    }
}

/// <summary>
///     Handler for <see cref="GetConditionalPolicyUsageQuery"/> (issue #1203): resolves
///     the policy identity fields from the store. Usage counters are zero until
///     per-policy evaluation telemetry is recorded (see the file header).
/// </summary>
public sealed class GetConditionalPolicyUsageQueryHandler(
    IConditionalPolicyRepository conditionalPolicyRepository,
    IActorContextAccessor actorContextAccessor)
    : IQueryHandler<GetConditionalPolicyUsageQuery, ConditionalPolicyUsageDto>
{
    private ActorContext Actor => actorContextAccessor.ActorContext;

    public async Task<ConditionalPolicyUsageDto> Handle(GetConditionalPolicyUsageQuery request, CancellationToken cancellationToken)
    {
        AbacPolicyQueryGuards.EnsureAuthenticated(Actor);

        var policy = await conditionalPolicyRepository.GetByIdAsync(request.PolicyId, cancellationToken).ConfigureAwait(false)
            ?? throw new EntityNotFoundException("ConditionalPolicy", request.PolicyId);

        AbacPolicyQueryGuards.EnsureCanReadPolicy(Actor, policy.TenantId);

        return new ConditionalPolicyUsageResult
        {
            PolicyId = policy.Id,
            PolicyName = policy.Name,
            FromDate = request.FromDate,
            ToDate = request.ToDate,
        };
    }
}

/// <summary>
///     Handler for <see cref="GetConditionalPolicyEvaluationHistoryQuery"/> (issue
///     #1203): the evaluator currently records no per-policy evaluation history, so the
///   history is empty until that telemetry exists; the policy itself must exist (404
///     otherwise), which keeps the contract distinguishable from an unknown policy.
/// </summary>
public sealed class GetConditionalPolicyEvaluationHistoryQueryHandler(
    IConditionalPolicyRepository conditionalPolicyRepository,
    IActorContextAccessor actorContextAccessor)
    : IQueryHandler<GetConditionalPolicyEvaluationHistoryQuery, ConditionalPolicyEvaluationHistoryDto>
{
    private ActorContext Actor => actorContextAccessor.ActorContext;

    public async Task<ConditionalPolicyEvaluationHistoryDto> Handle(GetConditionalPolicyEvaluationHistoryQuery request, CancellationToken cancellationToken)
    {
        AbacPolicyQueryGuards.EnsureAuthenticated(Actor);

        var policy = await conditionalPolicyRepository.GetByIdAsync(request.PolicyId, cancellationToken).ConfigureAwait(false)
            ?? throw new EntityNotFoundException("ConditionalPolicy", request.PolicyId);

        AbacPolicyQueryGuards.EnsureCanReadPolicy(Actor, policy.TenantId);

        var page = GetAbacPoliciesQueryHandler.NormalizePage(request.Page);
        var pageSize = GetAbacPoliciesQueryHandler.NormalizePageSize(request.PageSize);

        return new ConditionalPolicyEvaluationHistoryResult
        {
            PolicyId = policy.Id,
            History = [],
            TotalCount = 0,
            Page = page,
            PageSize = pageSize,
        };
    }
}

/// <summary>
///     Handler for <see cref="GetConditionalPolicyConflictsQuery"/> (issue #1203):
///     advisory analysis over the enabled conditional policies in the tenant scope. A
///     conflict is reported when an Allow and a Deny policy share the same permission
///     and resource scope (the evaluator lets Deny win, but the pair deserves review);
///     a redundancy dependency is reported when two enabled policies share the same
///     scope and action.
/// </summary>
public sealed class GetConditionalPolicyConflictsQueryHandler(
    IConditionalPolicyRepository conditionalPolicyRepository,
    IActorContextAccessor actorContextAccessor)
    : IQueryHandler<GetConditionalPolicyConflictsQuery, ConditionalPolicyConflictsDto>
{
    private ActorContext Actor => actorContextAccessor.ActorContext;

    public async Task<ConditionalPolicyConflictsDto> Handle(GetConditionalPolicyConflictsQuery request, CancellationToken cancellationToken)
    {
        AbacPolicyQueryGuards.EnsureAuthenticated(Actor);

        var tenantScope = AbacPolicyQueryGuards.ResolveTenantScope(Actor, request.TenantId);
        var enabledPolicies = (await conditionalPolicyRepository.GetByTenantAsync(tenantScope, cancellationToken).ConfigureAwait(false))
            .Where(policy => policy.IsEnabled)
            .ToList();

        var result = new ConditionalPolicyConflictsResult
        {
            TenantId = tenantScope,
            AnalyzedAt = SystemClock.UtcNow,
        };

        foreach (var group in enabledPolicies.GroupBy(ScopeKey))
        {
            var allows = group.Where(policy => policy.Action == PolicyAction.Allow).ToList();
            var denies = group.Where(policy => policy.Action == PolicyAction.Deny).ToList();

            foreach (var allow in allows)
            {
                foreach (var deny in denies)
                {
                    result.Conflicts.Add(new PolicyConflictEntry
                    {
                        PolicyId1 = allow.Id,
                        PolicyName1 = allow.Name,
                        PolicyId2 = deny.Id,
                        PolicyName2 = deny.Name,
                        ConflictType = "ActionConflict",
                        Severity = "Medium",
                        Description = $"Allow '{allow.Name}' and Deny '{deny.Name}' share the scope '{group.Key}'; the evaluator resolves it by priority (Deny wins ties), review whether both are intended",
                    });
                }
            }

            foreach (var sameAction in group.GroupBy(policy => policy.Action).Where(actions => actions.Count() > 1))
            {
                var primary = sameAction.First();
                var duplicates = sameAction.Skip(1).ToList();

                result.Dependencies.Add(new PolicyDependencyEntry
                {
                    PolicyId = primary.Id,
                    PolicyName = primary.Name,
                    DependsOnPolicyIds = duplicates.Select(policy => policy.Id).ToList(),
                    DependsOnPolicyNames = duplicates.Select(policy => policy.Name).ToList(),
                    DependencyType = "Redundant",
                });
            }
        }

        return result;
    }

    private static string ScopeKey(ConditionalPolicy policy)
        => $"{policy.PermissionType ?? "*"}::{policy.ResourceType ?? "*"}";

    private sealed class PolicyConflictEntry : PolicyConflict
    {
    }

    private sealed class PolicyDependencyEntry : PolicyDependency
    {
    }
}

/// <summary>
///     Handler for <see cref="GetPolicyConditionTypesQuery"/> (issue #1203): returns the
///     catalog of condition types the production evaluator
///     (<see cref="ConditionalPolicyEvaluator"/>) understands, with the concrete JSON
///     payload fields each condition type parses. The catalog mirrors the evaluator — it
///     is metadata, not stored data.
/// </summary>
public sealed class GetPolicyConditionTypesQueryHandler
    : IQueryHandler<GetPolicyConditionTypesQuery, IEnumerable<PolicyConditionTypeDto>>
{
    private static readonly IReadOnlyList<PolicyConditionTypeDto> Catalog =
    [
        new PolicyConditionTypeResult
        {
            Type = PolicyConditionType.Time.ToString(),
            Name = "Time window",
            Description = "Restricts by day of week and time-of-day window (optionally overnight)",
            SupportedOperators = ["within", "outside"],
            RequiredParameters = ["DaysOfWeek", "StartTime", "EndTime"],
            Examples = new Dictionary<string, object>
            {
                ["timeConditions"] = new Dictionary<string, object>
                {
                    ["DaysOfWeek"] = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" },
                    ["StartTime"] = "09:00",
                    ["EndTime"] = "17:00",
                },
            },
        },
        new PolicyConditionTypeResult
        {
            Type = PolicyConditionType.Environment.ToString(),
            Name = "Environment",
            Description = "Restricts by MFA verification, risk score and session age",
            SupportedOperators = ["require", "max"],
            RequiredParameters = ["RequireMfa", "MaxRiskScore", "MaxSessionAgeMinutes"],
            Examples = new Dictionary<string, object>
            {
                ["environmentConditions"] = new Dictionary<string, object>
                {
                    ["RequireMfa"] = true,
                    ["MaxRiskScore"] = 50,
                    ["MaxSessionAgeMinutes"] = 480,
                },
            },
        },
        new PolicyConditionTypeResult
        {
            Type = PolicyConditionType.Location.ToString(),
            Name = "Location",
            Description = "Restricts by country allow/deny lists and allowed IP ranges (CIDR or exact)",
            SupportedOperators = ["allow-list", "deny-list"],
            RequiredParameters = ["AllowedCountries", "BlockedCountries", "AllowedIpRanges"],
            Examples = new Dictionary<string, object>
            {
                ["locationConditions"] = new Dictionary<string, object>
                {
                    ["AllowedCountries"] = new[] { "BR", "PT" },
                    ["BlockedCountries"] = Array.Empty<string>(),
                    ["AllowedIpRanges"] = new[] { "192.168.0.0/16" },
                },
            },
        },
        new PolicyConditionTypeResult
        {
            Type = PolicyConditionType.Device.ToString(),
            Name = "Device",
            Description = "Restricts by trusted device fingerprints and blocked user-agent patterns",
            SupportedOperators = ["allow-list", "block-contains"],
            RequiredParameters = ["AllowedFingerprints", "BlockedUserAgents"],
            Examples = new Dictionary<string, object>
            {
                ["deviceConditions"] = new Dictionary<string, object>
                {
                    ["AllowedFingerprints"] = new[] { "fingerprint-1" },
                    ["BlockedUserAgents"] = new[] { "curl" },
                },
            },
        },
        new PolicyConditionTypeResult
        {
            Type = PolicyConditionType.RiskScore.ToString(),
            Name = "Risk score",
            Description = "Risk thresholds are expressed through environment conditions (MaxRiskScore); no separate risk payload is parsed",
            SupportedOperators = ["max"],
            RequiredParameters = ["MaxRiskScore"],
            Examples = new Dictionary<string, object>
            {
                ["environmentConditions"] = new Dictionary<string, object> { ["MaxRiskScore"] = 30 },
            },
        },
        new PolicyConditionTypeResult
        {
            Type = PolicyConditionType.Custom.ToString(),
            Name = "Custom",
            Description = "Free-form JSON object matched against custom context attributes",
            SupportedOperators = ["equals"],
            RequiredParameters = ["CustomAttributes"],
            Examples = new Dictionary<string, object>
            {
                ["customConditions"] = new Dictionary<string, object> { ["project"] = "game-guild" },
            },
        },
    ];

    public Task<IEnumerable<PolicyConditionTypeDto>> Handle(GetPolicyConditionTypesQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult<IEnumerable<PolicyConditionTypeDto>>(Catalog);
    }
}

/// <summary>
///     Handler for <see cref="GetConditionalPolicyTemplatesQuery"/> (issue #1203):
///     returns the built-in template catalog. Templates are code assets, not stored
///     data: each <c>Template</c> is a condition payload the production evaluator
///     (<see cref="ConditionalPolicyEvaluator"/>) parses, so every template can be
///     explored today through the <c>:test-rule</c> and <c>:simulate</c> endpoints.
///     Persisting a template as a policy still requires the management API (currently
///     unavailable, see issue #1203).
/// </summary>
public sealed class GetConditionalPolicyTemplatesQueryHandler
    : IQueryHandler<GetConditionalPolicyTemplatesQuery, IEnumerable<ConditionalPolicyTemplateDto>>
{
    private static readonly IReadOnlyList<ConditionalPolicyTemplateDto> Catalog =
    [
        new ConditionalPolicyTemplateResult
        {
            Id = Guid.Parse("3c8d2f66-4a91-4e75-b8a2-9f0d6c5b2001"),
            Name = "Business hours only",
            Description = "Deny actions outside the configured business window",
            ConditionType = PolicyConditionType.Time.ToString(),
            Template = """{"DaysOfWeek":["Monday","Tuesday","Wednesday","Thursday","Friday"],"StartTime":"09:00","EndTime":"17:00"}""",
            IsActive = true,
            CreatedAt = SystemClock.UtcNow,
        },
        new ConditionalPolicyTemplateResult
        {
            Id = Guid.Parse("3c8d2f66-4a91-4e75-b8a2-9f0d6c5b2002"),
            Name = "Blocked countries",
            Description = "Deny access from the configured countries",
            ConditionType = PolicyConditionType.Location.ToString(),
            Template = """{"BlockedCountries":["XX"]}""",
            IsActive = true,
            CreatedAt = SystemClock.UtcNow,
        },
        new ConditionalPolicyTemplateResult
        {
            Id = Guid.Parse("3c8d2f66-4a91-4e75-b8a2-9f0d6c5b2003"),
            Name = "Require MFA",
            Description = "Deny sessions that are not MFA-verified",
            ConditionType = PolicyConditionType.Environment.ToString(),
            Template = """{"RequireMfa":true}""",
            IsActive = true,
            CreatedAt = SystemClock.UtcNow,
        },
        new ConditionalPolicyTemplateResult
        {
            Id = Guid.Parse("3c8d2f66-4a91-4e75-b8a2-9f0d6c5b2004"),
            Name = "Block legacy user agents",
            Description = "Deny requests whose user agent matches the configured patterns",
            ConditionType = PolicyConditionType.Device.ToString(),
            Template = """{"BlockedUserAgents":["curl","wget"]}""",
            IsActive = true,
            CreatedAt = SystemClock.UtcNow,
        },
    ];

    public Task<IEnumerable<ConditionalPolicyTemplateDto>> Handle(GetConditionalPolicyTemplatesQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult<IEnumerable<ConditionalPolicyTemplateDto>>(Catalog);
    }
}
