using System.Diagnostics;
using GameGuild.CQRS;
using GameGuild.Database;
using GameGuild.Modules.Permissions.Abstractions;
using GameGuild.Modules.Permissions.Commands;

namespace GameGuild.Modules.Permissions.Handlers;

/// <summary>
/// Handler for simulating permission evaluation
/// Provides what-if analysis without actually affecting permissions
/// </summary>
public class SimulatePermissionHandler : IRequestHandler<SimulatePermissionCommand, PermissionSimulationResult>
{
    private readonly ApplicationDbContext _context;
    private readonly IPermissionService _permissionService;
    private readonly IAbacPolicyService _abacService;
    private readonly ILogger<SimulatePermissionHandler> _logger;

    public SimulatePermissionHandler(
        ApplicationDbContext context,
        IPermissionService permissionService,
        IAbacPolicyService abacService,
        ILogger<SimulatePermissionHandler> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
        _abacService = abacService ?? throw new ArgumentNullException(nameof(abacService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PermissionSimulationResult> Handle(SimulatePermissionCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var overallStopwatch = Stopwatch.StartNew();
        var result = new PermissionSimulationResult();

        _logger.LogInformation("Simulating permission check for User:{UserId}, Permission:{Permission}, Resource:{ResourceType}:{ResourceId}",
            request.UserId, request.Permission, request.ResourceType, request.ResourceId);

        try
        {
            // 1. Evaluate DAC Layer (Tenant permissions)
            if (request.TenantId.HasValue)
            {
                await EvaluateDacLayerAsync(request, result, cancellationToken);
            }

            // 2. Evaluate ABAC Layer (Attribute-based policies)
            if (!string.IsNullOrEmpty(request.ResourceType))
            {
                await EvaluateAbacLayerAsync(request, result, cancellationToken);
            }

            // 3. Evaluate Owner Override
            if (request.ResourceId.HasValue)
            {
                await EvaluateOwnerOverrideAsync(request, result, cancellationToken);
            }

            // 4. Determine final result
            DetermineFinalResult(result);

            // 5. Generate recommendations
            GenerateRecommendations(request, result);

            overallStopwatch.Stop();
            result.EvaluationTimeMs = overallStopwatch.ElapsedMilliseconds;

            _logger.LogInformation("Permission simulation completed: {Result}, Layers: {LayerCount}, Time: {Time}ms",
                result.WouldBeGranted ? "GRANTED" : "DENIED", result.LayerResults.Count, result.EvaluationTimeMs);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during permission simulation");

            overallStopwatch.Stop();
            result.WouldBeGranted = false;
            result.Reason = $"Simulation error: {ex.Message}";
            result.EvaluationTimeMs = overallStopwatch.ElapsedMilliseconds;
            result.EvaluationTrace.Add($"ERROR: {ex.Message}");

            return result;
        }
    }

    private async Task EvaluateDacLayerAsync(SimulatePermissionCommand request, PermissionSimulationResult result, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var layerResult = new LayerEvaluationResult { LayerName = "DAC (Discretionary Access Control)" };

        try
        {
            var hasTenantPermission = await _permissionService.HasTenantPermissionAsync(
                request.UserId,
                request.TenantId,
                request.Permission);

            layerResult.WouldGrant = hasTenantPermission;
            layerResult.Reason = hasTenantPermission
                ? "User has explicit tenant-level permission"
                : "User does not have tenant-level permission";

            if (hasTenantPermission)
            {
                var permissions = await _permissionService.GetTenantPermissionsAsync(request.UserId, request.TenantId);
                layerResult.MatchedRules.Add($"Tenant permissions: {string.Join(", ", permissions)}");
            }

            result.EvaluationTrace.Add($"DAC Layer: {layerResult.Reason}");
        }
        catch (Exception ex)
        {
            layerResult.WouldGrant = false;
            layerResult.Reason = $"DAC evaluation error: {ex.Message}";
            result.EvaluationTrace.Add($"DAC Layer Error: {ex.Message}");
        }

        stopwatch.Stop();
        layerResult.EvaluationTimeMs = stopwatch.ElapsedMilliseconds;
        result.LayerResults.Add(layerResult);
    }

    private async Task EvaluateAbacLayerAsync(SimulatePermissionCommand request, PermissionSimulationResult result, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var layerResult = new LayerEvaluationResult { LayerName = "ABAC (Attribute-Based Access Control)" };

        try
        {
            var context = new AbacEvaluationContext
            {
                UserId = request.UserId,
                TenantId = request.TenantId,
                ResourceId = request.ResourceId,
                ResourceType = request.ResourceType ?? string.Empty,
                Permission = request.Permission,
                UserAttributes = request.UserAttributes ?? new Dictionary<string, object>(),
                ResourceAttributes = request.ResourceAttributes ?? new Dictionary<string, object>(),
                ContextAttributes = request.ContextAttributes ?? new Dictionary<string, object>()
            };

            var abacResult = await _abacService.EvaluatePoliciesAsync(context, cancellationToken);

            layerResult.WouldGrant = abacResult.IsGranted;
            layerResult.Reason = abacResult.Reason;

            if (abacResult.MatchedPolicy != null)
            {
                layerResult.MatchedRules.Add($"Policy: {abacResult.MatchedPolicy.Name} (Effect: {abacResult.MatchedPolicy.Effect})");
            }

            result.EvaluationTrace.Add($"ABAC Layer: {layerResult.Reason}");
            result.EvaluationTrace.AddRange(abacResult.EvaluationTrace.Select(t => $"  {t}"));
        }
        catch (Exception ex)
        {
            layerResult.WouldGrant = false;
            layerResult.Reason = $"ABAC evaluation error: {ex.Message}";
            result.EvaluationTrace.Add($"ABAC Layer Error: {ex.Message}");
        }

        stopwatch.Stop();
        layerResult.EvaluationTimeMs = stopwatch.ElapsedMilliseconds;
        result.LayerResults.Add(layerResult);
    }

    private async Task EvaluateOwnerOverrideAsync(SimulatePermissionCommand request, PermissionSimulationResult result, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var layerResult = new LayerEvaluationResult { LayerName = "Owner Override" };

        try
        {
            // Check if user is the owner of the resource
            // This is a placeholder - actual implementation would check the resource's owner
            var isOwner = false; // TODO: Implement owner check

            layerResult.WouldGrant = isOwner;
            layerResult.Reason = isOwner
                ? "User is the resource owner (full access)"
                : "User is not the resource owner";

            result.EvaluationTrace.Add($"Owner Override: {layerResult.Reason}");
        }
        catch (Exception ex)
        {
            layerResult.WouldGrant = false;
            layerResult.Reason = $"Owner check error: {ex.Message}";
            result.EvaluationTrace.Add($"Owner Override Error: {ex.Message}");
        }

        stopwatch.Stop();
        layerResult.EvaluationTimeMs = stopwatch.ElapsedMilliseconds;
        result.LayerResults.Add(layerResult);
    }

    private void DetermineFinalResult(PermissionSimulationResult result)
    {
        // Any layer that denies wins (deny takes precedence)
        var anyDeny = result.LayerResults.Any(l => l.WouldGrant == false && l.Reason.Contains("denied", StringComparison.OrdinalIgnoreCase));

        if (anyDeny)
        {
            result.WouldBeGranted = false;
            result.Reason = "Access denied by one or more authorization layers";
            return;
        }

        // If any layer grants, access is granted
        var anyGrant = result.LayerResults.Any(l => l.WouldGrant);

        if (anyGrant)
        {
            result.WouldBeGranted = true;
            var grantingLayers = result.LayerResults.Where(l => l.WouldGrant).Select(l => l.LayerName);
            result.Reason = $"Access granted by: {string.Join(", ", grantingLayers)}";
        }
        else
        {
            result.WouldBeGranted = false;
            result.Reason = "No authorization layer grants access (deny by default)";
        }
    }

    private void GenerateRecommendations(SimulatePermissionCommand request, PermissionSimulationResult result)
    {
        if (result.WouldBeGranted)
        {
            result.Recommendations.Add("Access would be granted - no action needed");
            return;
        }

        // Generate recommendations based on which layers failed
        foreach (var layer in result.LayerResults.Where(l => !l.WouldGrant))
        {
            switch (layer.LayerName)
            {
                case "DAC (Discretionary Access Control)":
                    result.Recommendations.Add($"Grant tenant-level permission '{request.Permission}' to user in tenant {request.TenantId}");
                    break;

                case "ABAC (Attribute-Based Access Control)":
                    result.Recommendations.Add($"Create an ABAC policy that allows '{request.Permission}' for resource type '{request.ResourceType}'");
                    result.Recommendations.Add("Verify that user/resource attributes match policy requirements");
                    break;

                case "Owner Override":
                    result.Recommendations.Add("Transfer resource ownership to the user");
                    break;
            }
        }

        if (result.Recommendations.Count == 0)
        {
            result.Recommendations.Add("Review all authorization layers - access denied by default");
        }
    }
}
