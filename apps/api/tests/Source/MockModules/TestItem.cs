namespace GameGuild.Tests.MockModules;

/// <summary>
/// Simple test entity for infrastructure testing
/// </summary>
public class TestItem {
  public Guid Id { get; set; }
  public string Name { get; set; } = string.Empty;
  public string Description { get; set; } = string.Empty;
  public DateTime CreatedAt { get; set; }
  public bool IsActive { get; set; } = true;
}
