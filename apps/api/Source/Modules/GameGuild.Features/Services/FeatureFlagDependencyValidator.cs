namespace GameGuild.Features;

/// <summary>
///     Implementation of feature flag dependency validation with cycle detection
/// </summary>
public sealed class FeatureFlagDependencyValidator(IFeatureFlagQueryRepository repository) : IFeatureFlagDependencyValidator
{
    public async Task<bool> HasCircularDependencyAsync(string flagKey, string dependsOnKey)
    {
        // Check if adding this dependency would create a cycle
        var visited = new HashSet<string>();
        var recursionStack = new HashSet<string>();

        return await DetectCycleAsync(dependsOnKey, flagKey, visited, recursionStack).ConfigureAwait(false);
    }

    public async Task<List<List<string>>> GetAllCircularDependenciesAsync()
    {
        var allFlags = await repository.GetAllAsync().ConfigureAwait(false);
        var cycles = new List<List<string>>();
        var visited = new HashSet<string>();

        foreach (var flag in allFlags)
        {
            if (!visited.Contains(flag.Key))
            {
                var recursionStack = new HashSet<string>();
                var currentPath = new List<string>();
                await FindCyclesAsync(flag.Key, visited, recursionStack, currentPath, cycles).ConfigureAwait(false);
            }
        }

        return cycles;
    }

    public async Task<(bool IsValid, List<string>? Cycle)> ValidateDependencyGraphAsync(string startFlagKey)
    {
        var visited = new HashSet<string>();
        var recursionStack = new HashSet<string>();
        var path = new List<string>();

        var hasCycle = await DetectCycleWithPathAsync(startFlagKey, visited, recursionStack, path).ConfigureAwait(false);

        return (!hasCycle, hasCycle ? path : null);
    }

    private async Task<bool> DetectCycleAsync(string currentKey, string targetKey, HashSet<string> visited, HashSet<string> recursionStack)
    {
        if (currentKey == targetKey) return true;

        if (recursionStack.Contains(currentKey)) return false;

        if (visited.Contains(currentKey)) return false;

        visited.Add(currentKey);
        recursionStack.Add(currentKey);

        var flag = await repository.GetByKeyAsync(currentKey).ConfigureAwait(false);

        if (flag?.Targets != null)
        {
            foreach (var target in flag.Targets.Where(t => !string.IsNullOrEmpty(t.DependsOn)))
            {
                if (await DetectCycleAsync(target.DependsOn!, targetKey, visited, recursionStack))
                {
                    recursionStack.Remove(currentKey);

                    return true;
                }
            }
        }

        recursionStack.Remove(currentKey);

        return false;
    }

    private async Task FindCyclesAsync(string currentKey, HashSet<string> visited, HashSet<string> recursionStack, List<string> currentPath, List<List<string>> cycles)
    {
        visited.Add(currentKey);
        recursionStack.Add(currentKey);
        currentPath.Add(currentKey);

        var flag = await repository.GetByKeyAsync(currentKey).ConfigureAwait(false);

        if (flag?.Targets != null)
        {
            foreach (var target in flag.Targets.Where(t => !string.IsNullOrEmpty(t.DependsOn)))
            {
                var dependencyKey = target.DependsOn!;

                if (!visited.Contains(dependencyKey)) { await FindCyclesAsync(dependencyKey, visited, recursionStack, currentPath, cycles).ConfigureAwait(false); }
                else if (recursionStack.Contains(dependencyKey))
                {
                    // Found a cycle
                    var cycleStart = currentPath.IndexOf(dependencyKey);
                    var cycle = currentPath.Skip(cycleStart).ToList();
                    cycle.Add(dependencyKey); // Complete the cycle

                    if (!cycles.Any(c => c.SequenceEqual(cycle))) { cycles.Add(cycle); }
                }
            }
        }

        currentPath.Remove(currentKey);
        recursionStack.Remove(currentKey);
    }

    private async Task<bool> DetectCycleWithPathAsync(string currentKey, HashSet<string> visited, HashSet<string> recursionStack, List<string> path)
    {
        if (recursionStack.Contains(currentKey))
        {
            path.Add(currentKey);

            return true;
        }

        if (visited.Contains(currentKey)) return false;

        visited.Add(currentKey);
        recursionStack.Add(currentKey);
        path.Add(currentKey);

        var flag = await repository.GetByKeyAsync(currentKey).ConfigureAwait(false);

        if (flag?.Targets != null)
        {
            foreach (var target in flag.Targets.Where(t => !string.IsNullOrEmpty(t.DependsOn)))
            {
                if (await DetectCycleWithPathAsync(target.DependsOn!, visited, recursionStack, path)) { return true; }
            }
        }

        path.Remove(currentKey);
        recursionStack.Remove(currentKey);

        return false;
    }
}
