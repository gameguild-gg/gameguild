namespace GameGuild;

public class ApiExplorerOptions {
  public bool GroupNameFormat { get; set; } = true;

  public string DefaultGroupName { get; set; } = "v1";

  public void Validate() {
    if (string.IsNullOrWhiteSpace(DefaultGroupName)) throw new InvalidOperationException("Default group name cannot be null or empty.");
  }
}
