using System.ComponentModel.DataAnnotations;


namespace GameGuild.Common;

public class CorsOptions {
  public const string SectionName = "Cors";

  [Required] public string[] AllowedOrigins { get; set; } = [];

  public bool AllowCredentials { get; set; } = true;

  public string[] AllowedMethods { get; set; } = ["GET", "POST", "PUT", "DELETE", "OPTIONS"];

  public string[] AllowedHeaders { get; set; } = ["*"];
}
