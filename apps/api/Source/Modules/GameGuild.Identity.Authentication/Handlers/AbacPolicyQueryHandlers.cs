using System.Text.Json;
using GameGuild.CQRS;
using GameGuild.CQRS.Models;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;

namespace GameGuild.Identity.Authentication;

// ============================================================================
// ABAC POLICY QUERY HANDLERS (issue #1203)
//
// The read side of AbacPolicyController previously had query records without
// handlers, so every read endpoint failed at dispatch time with 500 "Handler
// not found" (docs/authorization/cqrs-wiring-matrix.md §2.1). These handlers
// back them with the Authorization module's production policy store
// (IAbacPolicyRepository), the same store the effective-resolution ABAC
// overlay evaluates.
//
// Data-source honesty: policy inventory fields (counts, categories, audit
// entries, conflict analysis) are computed from the policy store. Per-policy
// evaluation telemetry is not recorded anywhere yet — the ABAC overlay does
// not write the permission evaluation log — so evaluation-count and timing
// fields return zero/empty until that telemetry exists.
//
// Guards (fail-closed, mirroring the evaluate handlers in
// PolicyEvaluationCommandHandlers): the actor must be authenticated; a
// non-system actor is pinned to their own tenant scope and cannot read
// another tenant's policies.
// ============================================================================

/// <summary>Concrete statistics envelope.</summary>
public sealed class AbacPolicyStatisticsResult : AbacPolicyStatisticsDto
{
}

/// <summary>Concrete per-policy usage envelope.</summary>
public sealed class AbacPolicyUsageResult : AbacPolicyUsageDto
{
}

/// <summary>Concrete audit-trail envelope.</summary>
public sealed class AbacPolicyAuditTrailResult : AbacPolicyAuditTrailDto
{
}

/// <summary>Concrete conflicts envelope.</summary>
public sealed class AbacPolicyConflictsResult : AbacPolicyConflictsDto
{
}

/// <summary>Concrete template-catalog envelope.</summary>
public sealed class AbacPolicyTemplateResult : AbacPolicyTemplateDto
{
}

/// <summary>
///     Handler for <see cref="GetAbacPolicyQuery"/> (issue #1203): loads one ABAC
///     policy from the production policy store. Unknown ids fail with 404; reading
///     another tenant's policy fails closed with 403.
/// </summary>
public sealed class GetAbacPolicyQueryHandler(
    IAbacPolicyRepository abacPolicyRepository,
    IActorContextAccessor actorContextAccessor)
    : IQueryHandler<GetAbacPolicyQuery, AbacPolicy>
{
    private ActorContext Actor => actorContextAccessor.ActorContext;

    public async Task<AbacPolicy> Handle(GetAbacPolicyQuery request, CancellationToken cancellationToken)
    {
        AbacPolicyQueryGuards.EnsureAuthenticated(Actor);

        var policy = await abacPolicyRepository.GetByIdAsync(request.PolicyId, cancellationToken).ConfigureAwait(false)
            ?? throw new EntityNotFoundException("AbacPolicy", request.PolicyId);

        AbacPolicyQueryGuards.EnsureCanReadPolicy(Actor, policy.TenantId);

        return policy;
    }
}

/// <summary>
///     Handler for <see cref="GetAbacPoliciesQuery"/> (issue #1203): lists the policies
///     in the caller's tenant scope (system administrators may target any tenant or the
///     global scope), applying the optional active and category (tag) filters in memory
///     before page-based paging.
/// </summary>
public sealed class GetAbacPoliciesQueryHandler(
    IAbacPolicyRepository abacPolicyRepository,
    IActorContextAccessor actorContextAccessor)
    : IQueryHandler<GetAbacPoliciesQuery, PagedResult<AbacPolicy>>
{
    private ActorContext Actor => actorContextAccessor.ActorContext;

    public async Task<PagedResult<AbacPolicy>> Handle(GetAbacPoliciesQuery request, CancellationToken cancellationToken)
    {
        AbacPolicyQueryGuards.EnsureAuthenticated(Actor);

        var tenantScope = AbacPolicyQueryGuards.ResolveTenantScope(Actor, request.TenantId);
        var policies = await abacPolicyRepository.GetByTenantAsync(tenantScope, cancellationToken).ConfigureAwait(false);

        var filtered = policies
            .Where(policy => request.IsActive is null || policy.IsEnabled == request.IsActive)
            .Where(policy => AbacPolicyQueryGuards.MatchesCategory(policy, request.Category))
            .OrderBy(policy => policy.Priority)
            .ThenBy(policy => policy.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return PagedResult<AbacPolicy>.FromPage(
            PageItems(filtered, request.Page, request.PageSize),
            filtered.Count,
            NormalizePage(request.Page),
            NormalizePageSize(request.PageSize));
    }

    private static IEnumerable<AbacPolicy> PageItems(List<AbacPolicy> items, int page, int pageSize)
        => items.Skip((NormalizePage(page) - 1) * NormalizePageSize(pageSize)).Take(NormalizePageSize(pageSize));

    internal static int NormalizePage(int page) => page < 1 ? 1 : page;

    internal static int NormalizePageSize(int pageSize) => pageSize < 1 ? 20 : Math.Min(pageSize, 100);
}

/// <summary>
///     Handler for <see cref="GetAbacPolicyStatisticsQuery"/> (issue #1203): aggregates
///     the policy inventory for the tenant scope. Evaluation counters are zero until
///     per-policy evaluation telemetry is recorded (see the file header).
/// </summary>
public sealed class GetAbacPolicyStatisticsQueryHandler(
    IAbacPolicyRepository abacPolicyRepository,
    IActorContextAccessor actorContextAccessor)
    : IQueryHandler<GetAbacPolicyStatisticsQuery, AbacPolicyStatisticsDto>
{
    private ActorContext Actor => actorContextAccessor.ActorContext;

    public async Task<AbacPolicyStatisticsDto> Handle(GetAbacPolicyStatisticsQuery request, CancellationToken cancellationToken)
    {
        AbacPolicyQueryGuards.EnsureAuthenticated(Actor);

        var tenantScope = AbacPolicyQueryGuards.ResolveTenantScope(Actor, request.TenantId);
        var policies = await abacPolicyRepository.GetByTenantAsync(tenantScope, cancellationToken).ConfigureAwait(false);

        var result = new AbacPolicyStatisticsResult
        {
            TenantId = tenantScope,
            FromDate = request.FromDate,
            ToDate = request.ToDate,
            TotalPolicies = policies.Count,
            ActivePolicies = policies.Count(policy => policy.IsEnabled),
        };
        result.InactivePolicies = result.TotalPolicies - result.ActivePolicies;

        foreach (var category in policies.Select(AbacPolicyQueryGuards.CategoryOf).GroupBy(category => category, StringComparer.OrdinalIgnoreCase))
        {
            result.PoliciesByCategory[category.Key] = category.Count();
        }

        return result;
    }
}

/// <summary>
///     Handler for <see cref="GetAbacPolicyUsageQuery"/> (issue #1203): resolves the
///     policy identity fields from the store. Usage counters are zero until per-policy
///     evaluation telemetry is recorded (see the file header).
/// </summary>
public sealed class GetAbacPolicyUsageQueryHandler(
    IAbacPolicyRepository abacPolicyRepository,
    IActorContextAccessor actorContextAccessor)
    : IQueryHandler<GetAbacPolicyUsageQuery, AbacPolicyUsageDto>
{
    private ActorContext Actor => actorContextAccessor.ActorContext;

    public async Task<AbacPolicyUsageDto> Handle(GetAbacPolicyUsageQuery request, CancellationToken cancellationToken)
    {
        AbacPolicyQueryGuards.EnsureAuthenticated(Actor);

        var policy = await abacPolicyRepository.GetByIdAsync(request.PolicyId, cancellationToken).ConfigureAwait(false)
            ?? throw new EntityNotFoundException("AbacPolicy", request.PolicyId);

        AbacPolicyQueryGuards.EnsureCanReadPolicy(Actor, policy.TenantId);

        return new AbacPolicyUsageResult
        {
            PolicyId = policy.Id,
            PolicyName = policy.Name,
            FromDate = request.FromDate,
            ToDate = request.ToDate,
        };
    }
}

/// <summary>
///     Handler for <see cref="GetAbacPolicyAuditTrailQuery"/> (issue #1203): materializes
///     the directly attested lifecycle facts of the policy (creation and last update from
///     the entity's own stamps). Finer-grained per-policy audit events are not recorded
///     yet — activation/deletion events will appear once the management API ships.
/// </summary>
public sealed class GetAbacPolicyAuditTrailQueryHandler(
    IAbacPolicyRepository abacPolicyRepository,
    IActorContextAccessor actorContextAccessor)
    : IQueryHandler<GetAbacPolicyAuditTrailQuery, AbacPolicyAuditTrailDto>
{
    private ActorContext Actor => actorContextAccessor.ActorContext;

    public async Task<AbacPolicyAuditTrailDto> Handle(GetAbacPolicyAuditTrailQuery request, CancellationToken cancellationToken)
    {
        AbacPolicyQueryGuards.EnsureAuthenticated(Actor);

        var policy = await abacPolicyRepository.GetByIdAsync(request.PolicyId, cancellationToken).ConfigureAwait(false)
            ?? throw new EntityNotFoundException("AbacPolicy", request.PolicyId);

        AbacPolicyQueryGuards.EnsureCanReadPolicy(Actor, policy.TenantId);

        var entries = new List<PolicyAuditEntry>
        {
            new PolicyAuditEntryResult
            {
                Id = policy.Id,
                Timestamp = policy.CreatedAt,
                Action = "Created",
                UserId = policy.CreatedBy == Guid.Empty ? null : policy.CreatedBy,
            },
        };

        if (policy.UpdatedAt is { } updatedAt)
        {
            entries.Add(new PolicyAuditEntryResult
            {
                Id = policy.Id,
                Timestamp = updatedAt,
                Action = "Updated",
                UserId = policy.UpdatedBy == Guid.Empty ? null : policy.UpdatedBy,
                Details = $"Current state: {(policy.IsEnabled ? "enabled" : "disabled")}, version {policy.Version}",
            });
        }

        entries.Sort((left, right) => right.Timestamp.CompareTo(left.Timestamp));

        var page = GetAbacPoliciesQueryHandler.NormalizePage(request.Page);
        var pageSize = GetAbacPoliciesQueryHandler.NormalizePageSize(request.PageSize);

        return new AbacPolicyAuditTrailResult
        {
            PolicyId = policy.Id,
            Entries = entries.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
            TotalCount = entries.Count,
            Page = page,
            PageSize = pageSize,
        };
    }

    private sealed class PolicyAuditEntryResult : PolicyAuditEntry
    {
    }
}

/// <summary>
///     Handler for <see cref="GetAbacPolicyConflictsQuery"/> (issue #1203): advisory
///     analysis over the enabled policies in the tenant scope. A conflict is reported
///     when an Allow and a Deny policy target the same resource type (both enabled) —
///     the evaluator resolves such pairs by priority, so pairs with distinct priorities
///     are informational while equal-priority pairs are genuinely ambiguous. Overlaps
///     list every resource type targeted by more than one enabled policy.
/// </summary>
public sealed class GetAbacPolicyConflictsQueryHandler(
    IAbacPolicyRepository abacPolicyRepository,
    IActorContextAccessor actorContextAccessor)
    : IQueryHandler<GetAbacPolicyConflictsQuery, AbacPolicyConflictsDto>
{
    private ActorContext Actor => actorContextAccessor.ActorContext;

    public async Task<AbacPolicyConflictsDto> Handle(GetAbacPolicyConflictsQuery request, CancellationToken cancellationToken)
    {
        AbacPolicyQueryGuards.EnsureAuthenticated(Actor);

        var tenantScope = AbacPolicyQueryGuards.ResolveTenantScope(Actor, request.TenantId);
        var enabledPolicies = (await abacPolicyRepository.GetByTenantAsync(tenantScope, cancellationToken).ConfigureAwait(false))
            .Where(policy => policy.IsEnabled)
            .ToList();

        var result = new AbacPolicyConflictsResult
        {
            TenantId = tenantScope,
            AnalyzedAt = SystemClock.UtcNow,
        };

        foreach (var group in enabledPolicies.GroupBy(policy => policy.ResourceType ?? "*", StringComparer.OrdinalIgnoreCase))
        {
            var allows = group.Where(policy => policy.Effect == AbacPolicyEffect.Allow).ToList();
            var denies = group.Where(policy => policy.Effect == AbacPolicyEffect.Deny).ToList();

            foreach (var allow in allows)
            {
                foreach (var deny in denies)
                {
                    var samePriority = allow.Priority == deny.Priority;
                    result.Conflicts.Add(new PolicyConflictEntry
                    {
                        PolicyId1 = allow.Id,
                        PolicyName1 = allow.Name,
                        PolicyId2 = deny.Id,
                        PolicyName2 = deny.Name,
                        ConflictType = "EffectConflict",
                        Severity = samePriority ? "High" : "Medium",
                        Description = samePriority
                            ? $"Allow '{allow.Name}' and Deny '{deny.Name}' both target resource type '{group.Key}' with equal priority {allow.Priority}; evaluation order is undefined"
                            : $"Allow '{allow.Name}' and Deny '{deny.Name}' both target resource type '{group.Key}'; the Deny takes precedence at equal priority, otherwise priority decides",
                    });
                }
            }

            if (group.Count() > 1)
            {
                var policies = group.ToList();
                result.Overlaps.Add(new PolicyOverlapEntry
                {
                    PolicyIds = policies.Select(policy => policy.Id).ToList(),
                    PolicyNames = policies.Select(policy => policy.Name).ToList(),
                    OverlapArea = group.Key,
                    Description = $"{policies.Count} enabled policies target resource type '{group.Key}'",
                    OverlapPercentage = 100.0,
                });
            }
        }

        return result;
    }

    private sealed class PolicyConflictEntry : PolicyConflict
    {
    }

    private sealed class PolicyOverlapEntry : PolicyOverlap
    {
    }
}

/// <summary>
///     Handler for <see cref="GetAbacPolicyTemplatesQuery"/> (issue #1203): returns the
///     built-in template catalog. Templates are code assets, not stored data: each
///     <c>JsonTemplate</c> is a valid <see cref="AbacPolicy.AttributeExpression"/> that
///     the production expression evaluator (<see cref="AbacExpressionEvaluator"/>) parses,
///     so every template can be explored today through the <c>:test-expression</c> and
///     <c>:evaluate</c> endpoints. Persisting a template as a policy still requires the
///     management API (currently unavailable, see issue #1203).
/// </summary>
public sealed class GetAbacPolicyTemplatesQueryHandler
    : IQueryHandler<GetAbacPolicyTemplatesQuery, IEnumerable<AbacPolicyTemplateDto>>
{
    public Task<IEnumerable<AbacPolicyTemplateDto>> Handle(GetAbacPolicyTemplatesQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult<IEnumerable<AbacPolicyTemplateDto>>(AbacPolicyTemplateCatalog.All);
    }
}

/// <summary>
///     Shared fail-closed guards for the ABAC/conditional policy query handlers.
/// </summary>
internal static class AbacPolicyQueryGuards
{
    /// <summary>The actor must be authenticated for any policy read.</summary>
    public static void EnsureAuthenticated(ActorContext actor)
    {
        if (!actor.IsAuthenticated)
        {
            throw new AuthenticationRequiredException("Reading policies requires an authenticated actor");
        }
    }

    /// <summary>
    ///     Resolves the tenant scope for a list/statistics read: system administrators
    ///     may target any tenant or the global scope (null); every other actor is pinned
    ///     to their own tenant and fails closed when asking for another.
    /// </summary>
    public static Guid? ResolveTenantScope(ActorContext actor, Guid? requestedTenantId)
    {
        if (actor.IsSystemAdmin)
        {
            return requestedTenantId is { } tenant && tenant != Guid.Empty ? tenant : null;
        }

        if (requestedTenantId is { } requested && requested != Guid.Empty)
        {
            if (actor.TenantId != requested)
            {
                throw new CrossTenantAccessException(
                    actor.SubjectIdAsGuid ?? Guid.Empty,
                    requested,
                    actor.TenantId);
            }

            return requested;
        }

        return actor.TenantId;
    }

    /// <summary>
    ///     Guards a single-tenant policy read: global policies are readable by any
    ///     authenticated actor; tenant policies only by that tenant's actors (or a
    ///     system administrator).
    /// </summary>
    public static void EnsureCanReadPolicy(ActorContext actor, TenantId? policyTenantId)
    {
        if (policyTenantId is not { } tenant || actor.IsSystemAdmin)
        {
            return;
        }

        if (actor.TenantId != tenant.Value)
        {
            throw new CrossTenantAccessException(
                actor.SubjectIdAsGuid ?? Guid.Empty,
                tenant.Value,
                actor.TenantId);
        }
    }

    /// <summary>
    ///     Category filter: a policy's category is its first tag (the entity stores
    ///     categorization as a JSON tag array). A null filter matches everything.
    /// </summary>
    public static bool MatchesCategory(AbacPolicy policy, string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return true;
        }

        return TagsOf(policy).Contains(category.Trim(), StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>The policy's category (first tag) or <c>Uncategorized</c>.</summary>
    public static string CategoryOf(AbacPolicy policy)
    {
        var tag = TagsOf(policy).FirstOrDefault();
        return string.IsNullOrWhiteSpace(tag) ? "Uncategorized" : tag.Trim();
    }

    private static IEnumerable<string> TagsOf(AbacPolicy policy)
    {
        if (string.IsNullOrWhiteSpace(policy.Tags))
        {
            return [];
        }

        try
        {
            using var document = JsonDocument.Parse(policy.Tags);
            return document.RootElement.ValueKind == JsonValueKind.Array
                ? document.RootElement.EnumerateArray()
                    .Where(element => element.ValueKind == JsonValueKind.String)
                    .Select(element => element.GetString() ?? string.Empty)
                    .Where(tag => tag.Length > 0)
                    .ToList()
                : [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}

/// <summary>
///     The built-in ABAC policy template catalog (issue #1203). Every template's JSON is
///     structurally valid for <see cref="AbacExpressionEvaluator.ValidateAttributeExpression"/>
///     and uses only the supported operators (<c>$eq</c>, <c>$ne</c>, <c>$gt</c>,
///     <c>$gte</c>, <c>$lt</c>, <c>$lte</c>, <c>$in</c>, <c>$nin</c>, <c>$exists</c>).
/// </summary>
internal static class AbacPolicyTemplateCatalog
{
    /// <summary>Fresh copies of the built-in templates per call (the DTOs are mutable).</summary>
    public static IReadOnlyList<AbacPolicyTemplateDto> All =>
    [
        new AbacPolicyTemplateResult
        {
            Id = Guid.Parse("7a1f9c52-8b3e-4d6a-9f10-2c5b8e7d1001"),
            Name = "Department match",
            Description = "Allow principals whose department attribute equals a configured value",
            Category = "Identity",
            JsonTemplate = """{"user.department": "{department}"}""",
            Parameters =
            [
                new TemplateParameterEntry
                {
                    Name = "department",
                    Type = "string",
                    Description = "Department the policy matches on",
                    IsRequired = true,
                },
            ],
            IsActive = true,
            CreatedAt = SystemClock.UtcNow,
        },
        new AbacPolicyTemplateResult
        {
            Id = Guid.Parse("7a1f9c52-8b3e-4d6a-9f10-2c5b8e7d1002"),
            Name = "Minimum clearance level",
            Description = "Allow principals whose clearance level is at least the configured value",
            Category = "Identity",
            JsonTemplate = """{"user.clearanceLevel": {"$gte": 0}}""",
            Parameters =
            [
                new TemplateParameterEntry
                {
                    Name = "level",
                    Type = "number",
                    Description = "Minimum clearance level (substituted for the 0 placeholder)",
                    IsRequired = true,
                },
            ],
            IsActive = true,
            CreatedAt = SystemClock.UtcNow,
        },
        new AbacPolicyTemplateResult
        {
            Id = Guid.Parse("7a1f9c52-8b3e-4d6a-9f10-2c5b8e7d1003"),
            Name = "Resource classification allow-list",
            Description = "Allow access only to resources whose classification is in the configured set",
            Category = "Resource",
            JsonTemplate = """{"resource.classification": {"$in": ["public", "internal"]}}""",
            Parameters =
            [
                new TemplateParameterEntry
                {
                    Name = "classifications",
                    Type = "array",
                    Description = "Classifications the policy allows",
                    IsRequired = true,
                    AllowedValues = ["public", "internal", "confidential", "restricted"],
                },
            ],
            IsActive = true,
            CreatedAt = SystemClock.UtcNow,
        },
        new AbacPolicyTemplateResult
        {
            Id = Guid.Parse("7a1f9c52-8b3e-4d6a-9f10-2c5b8e7d1004"),
            Name = "Owner-only access",
            Description = "Allow access only when the principal is the resource owner (subject matches resource owner id)",
            Category = "Resource",
            JsonTemplate = """{"resource.owner-id": {"$exists": true}}""",
            Parameters = [],
            IsActive = true,
            CreatedAt = SystemClock.UtcNow,
        },
    ];

    private sealed class TemplateParameterEntry : TemplateParameter
    {
    }
}
