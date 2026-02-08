namespace GameGuild.Features;

/// <summary>
///     Extension method for repository to get flags by environment
/// </summary>
public static class FeatureFlagRepositoryExtensions
{
#pragma warning disable CS0618 // Type or member is obsolete - IFeatureFlagRepository migration pending
    public static async Task<List<FeatureFlag>> GetByEnvironmentAsync(this IFeatureFlagRepository repository, string environment)
#pragma warning restore CS0618
    {
        var allFlags = await repository.GetAllAsync().ConfigureAwait(false);

        return allFlags.Where(f => f.Environment == environment).ToList();
    }
}
