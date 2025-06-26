using System.ComponentModel.DataAnnotations;

namespace GameGuild.Config;

public class CorsOptions
{
    private string[] _allowedOrigins = [];

    private bool _allowCredentials = true;

    private string[] _allowedMethods = ["GET", "POST", "PUT", "DELETE", "OPTIONS"];

    private string[] _allowedHeaders = ["*"];

    public const string SectionName = "Cors";

    [Required]
    public string[] AllowedOrigins
    {
        get => _allowedOrigins;
        set => _allowedOrigins = value;
    }

    public bool AllowCredentials
    {
        get => _allowCredentials;
        set => _allowCredentials = value;
    }

    public string[] AllowedMethods
    {
        get => _allowedMethods;
        set => _allowedMethods = value;
    }

    public string[] AllowedHeaders
    {
        get => _allowedHeaders;
        set => _allowedHeaders = value;
    }
}
