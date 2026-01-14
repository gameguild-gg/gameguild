using System.Collections.Concurrent;
using System.Text.Json;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Provides rulesets for policies from the database with caching.
/// </summary>
public sealed class RulesetProvider : IRulesetProvider
{
    private const string CacheKeyPrefix = "ruleset:";
    private static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromMinutes(5);

    /// <summary>
    ///     Tracks all cache keys for proper invalidation.
    ///     Using ConcurrentDictionary as a thread-safe set.
    /// </summary>
    private static readonly ConcurrentDictionary<string, byte> CacheKeys = new();

    private readonly ILogger<RulesetProvider> _logger;
    private readonly IMemoryCache _memoryCache;
    private readonly IPolicyDefinitionRepository _policyRepository;

    public RulesetProvider(
        IPolicyDefinitionRepository policyRepository,
        IMemoryCache memoryCache,
        ILogger<RulesetProvider> logger)
    {
        _policyRepository = policyRepository;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public async Task<PolicyRuleset?> GetRulesetAsync(
        string policyName,
        CancellationToken cancellationToken = default)
    {
        return await GetRulesetForTenantAsync(policyName, null, cancellationToken);
    }

    public async Task<PolicyRuleset?> GetRulesetForTenantAsync(
        string policyName,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = tenantId.HasValue
            ? $"{CacheKeyPrefix}{policyName}:{tenantId}"
            : $"{CacheKeyPrefix}{policyName}";

        if (_memoryCache.TryGetValue(cacheKey, out PolicyRuleset? cached))
        {
            return cached;
        }

        // Get policy from repository (it handles tenant fallback internally)
        var entity = await _policyRepository.GetByNameAsync(policyName, tenantId, cancellationToken);

        if (entity is null)
        {
            _logger.LogDebug("No policy found for {PolicyName}", policyName);
            return null;
        }

        var ruleset = ConvertToRuleset(entity);

        // Track the cache key for proper invalidation
        CacheKeys.TryAdd(cacheKey, 0);

        _memoryCache.Set(cacheKey, ruleset, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = DefaultCacheDuration,
            PostEvictionCallbacks =
            {
                new PostEvictionCallbackRegistration
                {
                    EvictionCallback = (key, _, _, _) =>
                    {
                        if (key is string keyStr)
                        {
                            CacheKeys.TryRemove(keyStr, out _);
                        }
                    }
                }
            }
        });

        return ruleset;
    }

    public void InvalidatePolicy(string policyName)
    {
        // Find all cache keys for this policy (base and tenant-specific)
        var keysToRemove = CacheKeys.Keys
            .Where(k => k.StartsWith($"{CacheKeyPrefix}{policyName}", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var key in keysToRemove)
        {
            _memoryCache.Remove(key);
            CacheKeys.TryRemove(key, out _);
        }

        _logger.LogDebug("Invalidated {Count} cache entries for policy {PolicyName}", keysToRemove.Count, policyName);
    }

    public void InvalidateAll()
    {
        var keysToRemove = CacheKeys.Keys.ToList();
        var count = keysToRemove.Count;

        foreach (var key in keysToRemove)
        {
            _memoryCache.Remove(key);
            CacheKeys.TryRemove(key, out _);
        }

        _logger.LogInformation("Invalidated all {Count} cached rulesets", count);
    }

    private PolicyRuleset ConvertToRuleset(PolicyDefinitionEntity entity)
    {
        // Parse rules from JSON
        var rules = new List<RuleDefinition>();

        if (!string.IsNullOrEmpty(entity.RulesJson))
        {
            try
            {
                var parsedRules = JsonSerializer.Deserialize<List<RuleDefinition>>(entity.RulesJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (parsedRules is not null)
                {
                    rules = parsedRules;
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex,
                    "Failed to parse RulesJson for policy {PolicyName}",
                    entity.PolicyName);
            }
        }

        // Add permission rules from RequiredPermissionsJson if no explicit rules
        if (rules.Count == 0)
        {
            var permissions = ParseJsonArray(entity.RequiredPermissionsJson);
            if (permissions.Count > 0)
            {
                rules.Add(new RuleDefinition
                {
                    Type = RuleTypes.RequireAllPermissions,
                    Description = "Require all specified permissions",
                    Params = new Dictionary<string, JsonElement>
                    {
                        ["permissions"] = JsonSerializer.SerializeToElement(permissions)
                    },
                    Enabled = true
                });
            }

            // Add role rules from RequiredRolesJson
            var roles = ParseJsonArray(entity.RequiredRolesJson);
            if (roles.Count > 0)
            {
                rules.Add(new RuleDefinition
                {
                    Type = RuleTypes.RequireAllPermissions,
                    Description = "Require all specified roles (as permissions)",
                    Params = new Dictionary<string, JsonElement>
                    {
                        ["permissions"] = JsonSerializer.SerializeToElement(
                            roles.Select(r => $"role:{r}").ToList())
                    },
                    Enabled = true
                });
            }

            // Add ACL rules from RequireAccessControlListAccess
            if (entity.RequireAccessControlListAccess)
            {
                rules.Add(new RuleDefinition
                {
                    Type = RuleTypes.OwnerOrAcl,
                    Description = "Check ACL access",
                    Params = new Dictionary<string, JsonElement>
                    {
                        ["allowOwner"] = JsonSerializer.SerializeToElement(false),
                        ["minimumAccessLevel"] = JsonSerializer.SerializeToElement(
                            entity.MinimumAccessLevel ?? "Read")
                    },
                    Enabled = true
                });
            }
        }

        return new PolicyRuleset
        {
            Name = entity.PolicyName,
            Description = entity.Description,
            RequireAuthentication = entity.RequireAuthentication,
            Rules = rules,
            Version = entity.PolicyVersion,
            IsActive = entity.IsActive
        };
    }

    private List<string> ParseJsonArray(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse JSON array: {Json}", json);
            return [];
        }
    }
}
