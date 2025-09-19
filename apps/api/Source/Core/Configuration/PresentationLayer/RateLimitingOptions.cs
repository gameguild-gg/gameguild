namespace GameGuild;

public class RateLimitingOptions {
  public int RequestsPerMinute { get; set; } = 60;

  public int BurstSize { get; set; } = 10;

  public string[] ExemptPaths { get; set; } = [];

  // Enhanced rate limiting configurations
  public int AuthRequestsPerMinute { get; set; } = 10;
  public int GraphQLRequestsPerMinute { get; set; } = 30;
  public int PaymentRequestsPerMinute { get; set; } = 5;

  // Per-IP rate limiting
  public int RequestsPerMinutePerIP { get; set; } = 100;
  public int BurstSizePerIP { get; set; } = 20;

  // User-specific rate limiting
  public int RequestsPerMinutePerUser { get; set; } = 120;
  public int BurstSizePerUser { get; set; } = 25;

  // Redis configuration for distributed rate limiting
  public string? RedisConnectionString { get; set; }
  public bool UseDistributedRateLimiting { get; set; } = false;

  // Rate limiting policies
  public Dictionary<string, EndpointRateLimitConfig> EndpointSpecificLimits { get; set; } = new();

  public void Validate() {
    if (RequestsPerMinute <= 0) throw new InvalidOperationException("Requests per minute must be greater than zero.");
    if (BurstSize <= 0) throw new InvalidOperationException("Burst size must be greater than zero.");
    if (ExemptPaths == null) throw new InvalidOperationException("Exempt paths cannot be null.");

    if (AuthRequestsPerMinute <= 0) throw new InvalidOperationException("Auth requests per minute must be greater than zero.");
    if (GraphQLRequestsPerMinute <= 0) throw new InvalidOperationException("GraphQL requests per minute must be greater than zero.");
    if (PaymentRequestsPerMinute <= 0) throw new InvalidOperationException("Payment requests per minute must be greater than zero.");

    if (RequestsPerMinutePerIP <= 0) throw new InvalidOperationException("Requests per minute per IP must be greater than zero.");
    if (BurstSizePerIP <= 0) throw new InvalidOperationException("Burst size per IP must be greater than zero.");

    if (RequestsPerMinutePerUser <= 0) throw new InvalidOperationException("Requests per minute per user must be greater than zero.");
    if (BurstSizePerUser <= 0) throw new InvalidOperationException("Burst size per user must be greater than zero.");

    if (UseDistributedRateLimiting && string.IsNullOrWhiteSpace(RedisConnectionString)) {
      throw new InvalidOperationException("Redis connection string is required when distributed rate limiting is enabled.");
    }
  }
}

public class EndpointRateLimitConfig {
  public int RequestsPerMinute { get; set; }
  public int BurstSize { get; set; }
  public bool ApplyToUser { get; set; } = true;
  public bool ApplyToIP { get; set; } = true;
  public string[] ExemptRoles { get; set; } = [];
}
