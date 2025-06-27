namespace GameGuild.Config;

public class AppConfig {
  private string _host = "localhost";

  private int _port = 5000;

  private bool _isDevelopmentEnvironment;

  private bool _isProductionEnvironment;

  private bool _isDocumentationEnabled;

  private string _environment = "Development";

  public string Host {
    get => _host;
    set => _host = value;
  }

  public int Port {
    get => _port;
    set => _port = value;
  }

  public bool IsDevelopmentEnvironment {
    get => _isDevelopmentEnvironment;
    set => _isDevelopmentEnvironment = value;
  }

  public bool IsProductionEnvironment {
    get => _isProductionEnvironment;
    set => _isProductionEnvironment = value;
  }

  public bool IsDocumentationEnabled {
    get => _isDocumentationEnabled;
    set => _isDocumentationEnabled = value;
  }

  public string Environment {
    get => _environment;
    set => _environment = value;
  }
}
