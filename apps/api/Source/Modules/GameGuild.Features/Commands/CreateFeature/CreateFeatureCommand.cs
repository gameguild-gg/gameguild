using GameGuild.CQRS;

namespace GameGuild.Features.Commands;

/// <summary>
///     Command to create a new feature
/// </summary>
public record CreateFeatureCommand(string Key, string Name, string? Description = null) : ICommand<Guid>;
