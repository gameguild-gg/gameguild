namespace GameGuild;

public class ModelValidationOptions {
  public bool SuppressModelStateInvalidFilter { get; set; }

  public bool AutomaticModelValidation { get; set; } = true;

  public void Validate() {
    // ModelValidationOptions validation is optional - boolean flags are always valid
  }
}
