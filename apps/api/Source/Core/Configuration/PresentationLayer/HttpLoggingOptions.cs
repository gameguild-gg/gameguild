namespace GameGuild;

public class HttpLoggingOptions {
  public bool LogRequestHeaders { get; set; } = true;

  public bool LogResponseHeaders { get; set; } = true;

  public bool LogRequestBody { get; set; }

  public bool LogResponseBody { get; set; }

  public void Validate() {
    // HTTP logging options are generally valid with any boolean values
  }
}
