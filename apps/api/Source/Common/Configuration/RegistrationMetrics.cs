namespace GameGuild.Common;

public class RegistrationMetrics {
  public int TotalHandlersRegistered { get; set; }

  public int TotalValidatorsRegistered { get; set; }

  public TimeSpan RegistrationDuration { get; set; }
}
