using GameGuild.Features.Abstractions;
using GameGuild.Features.Entities;

namespace GameGuild.Features.Services;

/// <summary>
///     Extension method for repository to get flags by environment
/// </summary>
public static class FeatureFlagRepositoryExtensions
{
    public static async Task<List<FeatureFlag>> GetByEnvironmentAsync(this IFeatureFlagRepository repository, string environment)
    {
        var allFlags = await repository.GetAllAsync();

        return allFlags.Where(f => f.Environment == environment).ToList();
    }
}
