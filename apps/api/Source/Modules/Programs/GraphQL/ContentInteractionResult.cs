using GameGuild.Modules.Programs.Models;


namespace GameGuild.Modules.Programs.GraphQL;

/// <summary>
/// Result type for ContentInteraction mutations with proper error handling
/// </summary>
public class ContentInteractionResult {
  public bool Success { get; set; }

  public string? ErrorMessage { get; set; }

  public ContentInteraction? Interaction { get; set; }
}
