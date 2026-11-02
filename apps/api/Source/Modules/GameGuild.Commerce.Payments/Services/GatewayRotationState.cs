namespace GameGuild.Commerce.Payments;

/// <summary>
///     Process-wide rotation state for the round-robin routing strategies (issue #413
///     routing expansion). Implementations must be thread-safe: selection happens on
///     concurrent request scopes, so every advance is an atomic step.
/// </summary>
public interface IGatewayRotationState
{
    /// <summary>
    ///     Rotates the candidate list one position further each call (even round-robin):
    ///     the returned list starts at the gateway that follows the last one selected.
    /// </summary>
    IReadOnlyList<GatewayCandidate> Rotate(IReadOnlyList<GatewayCandidate> candidates);

    /// <summary>
    ///     Selects the next gateway through smooth weighted round-robin (nginx-style
    ///     accumulators): each candidate's weight is added to its accumulator every pass,
    ///     the candidate with the highest accumulator wins and has the total candidate
    ///     weight subtracted. Long-run selection shares converge to the weight ratios.
    /// </summary>
    IReadOnlyList<GatewayCandidate> RotateWeighted(IReadOnlyList<GatewayCandidate> candidates);
}

/// <summary>
///     In-memory <see cref="IGatewayRotationState" /> (registered singleton): a global
///     monotonic counter for even round-robin and per-provider accumulators for the
///     smooth weighted variant. State is process-local by design — rotation evenness is
///     a load-distribution concern, not a correctness concern, so a process restart
///     simply restarts the rotation.
/// </summary>
public sealed class InMemoryGatewayRotationState : IGatewayRotationState
{
    private long _rotationCounter;
    private readonly object _weightedLock = new();
    private readonly Dictionary<string, int> _weightedAccumulators = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public IReadOnlyList<GatewayCandidate> Rotate(IReadOnlyList<GatewayCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        if (candidates.Count <= 1)
        {
            return candidates;
        }

        var rotation = (int)(Interlocked.Increment(ref _rotationCounter) % candidates.Count);
        if (rotation == 0)
        {
            return candidates;
        }

        return [.. candidates.Skip(rotation), .. candidates.Take(rotation)];
    }

    /// <inheritdoc />
    public IReadOnlyList<GatewayCandidate> RotateWeighted(IReadOnlyList<GatewayCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        if (candidates.Count <= 1)
        {
            return candidates;
        }

        int winnerIndex;
        lock (_weightedLock)
        {
            var totalWeight = 0;
            var bestAccumulator = int.MinValue;
            winnerIndex = 0;

            for (var index = 0; index < candidates.Count; index++)
            {
                var weight = Math.Max(1, candidates[index].Route?.Weight ?? 1);
                totalWeight += weight;

                var accumulator = (_weightedAccumulators.TryGetValue(
                    candidates[index].Gateway.ProviderId,
                    out var current)
                        ? current
                        : 0) + weight;
                _weightedAccumulators[candidates[index].Gateway.ProviderId] = accumulator;

                if (accumulator > bestAccumulator)
                {
                    bestAccumulator = accumulator;
                    winnerIndex = index;
                }
            }

            _weightedAccumulators[candidates[winnerIndex].Gateway.ProviderId] -= totalWeight;
        }

        if (winnerIndex == 0)
        {
            return candidates;
        }

        return [.. candidates.Skip(winnerIndex), .. candidates.Take(winnerIndex)];
    }
}
