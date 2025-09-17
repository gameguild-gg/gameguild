namespace GameGuild;

/// <summary> Represents metrics related to the registration process within the GameGuild API. Tracks the total number of handlers and validators registered, as well as the duration of the registration process. </summary>
public class RegistrationMetrics {
  /// <summary> Gets or sets the total number of handlers that have been registered. </summary>
  public int TotalHandlersRegistered { get; set; }

  /// <summary> Gets or sets the total number of validators that have been registered. </summary>
  public int TotalValidatorsRegistered { get; set; }

  /// <summary> Gets or sets the duration of the registration process. </summary>
  public TimeSpan RegistrationDuration { get; set; }
}
