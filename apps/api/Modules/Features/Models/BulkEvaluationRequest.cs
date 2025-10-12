using System.Collections.ObjectModel;

namespace GameGuild.Modules.Features.Models;

/// <summary>
///     Bulk feature flags evaluation request
/// </summary>
public class BulkEvaluationRequest
{
    public Collection<string> FeatureKeys { get; init; } = new Collection<string>();

    public FeatureContext Context { get; set; } = new FeatureContext();
}

