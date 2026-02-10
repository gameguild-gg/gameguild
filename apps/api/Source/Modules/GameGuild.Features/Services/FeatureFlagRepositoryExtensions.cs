namespace GameGuild.Features;

/// <summary>
///     Extension method for repository to get flags by environment (client-side filter)
/// </summary>
public static class FeatureFlagRepositoryExtensions
{
    public static async Task<List<FeatureFlag>> GetByEnvironmentAsync(this IFeatureFlagQueryRepository repository, string environment)
    {
        var allFlags = await repository.GetAllAsync().ConfigureAwait(false);

        return allFlags.Where(f => f.Environment == environment).ToList();
    }
}
