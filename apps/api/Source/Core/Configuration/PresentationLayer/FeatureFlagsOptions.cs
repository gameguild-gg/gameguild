namespace GameGuild;

public class FeatureFlagsOptions {
  /// <summary> When true, registers OpenFeature SDK and an in-memory provider by default. </summary>
  public bool UseInMemoryProvider { get; set; } = true;

  /// <summary> Optional default domain for OpenFeature client. </summary>
  public string? DefaultDomain { get; set; }

  /// <summary> Optional: Predefined boolean flags when using InMemoryProvider. Key: flag key; Value: default variant name (on/off) or literal true/false. </summary>
  public Dictionary<string, bool>? BooleanFlags { get; set; }

  public void Validate() {
    // No-op for now; add constraints as needed later.
  }
}
