namespace GameGuild.Config;

public class DatabaseConfig
{
    private string _connectionString = string.Empty;

    private string _provider = "PostgreSQL";

    private bool _enableSensitiveDataLogging = false;

    private bool _enableDetailedErrors = false;

    public string ConnectionString
    {
        get => _connectionString;
        set => _connectionString = value;
    }

    public string Provider
    {
        get => _provider;
        set => _provider = value;
    }

    public bool EnableSensitiveDataLogging
    {
        get => _enableSensitiveDataLogging;
        set => _enableSensitiveDataLogging = value;
    }

    public bool EnableDetailedErrors
    {
        get => _enableDetailedErrors;
        set => _enableDetailedErrors = value;
    }
}
