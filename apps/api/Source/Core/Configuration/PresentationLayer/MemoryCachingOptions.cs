namespace GameGuild;

public class MemoryCachingOptions {
  public long SizeLimit { get; set; } = 100 * 1024 * 1024; // 100MB

  public double CompactionPercentage { get; set; } = 0.05; // 5%

  public TimeSpan ExpirationScanFrequency { get; set; } = TimeSpan.FromMinutes(1);

  public void Validate() {
    if (SizeLimit <= 0) throw new InvalidOperationException("Size limit must be greater than zero.");

    if (CompactionPercentage <= 0 || CompactionPercentage >= 1) throw new InvalidOperationException("Compaction percentage must be between 0 and 1.");

    if (ExpirationScanFrequency <= TimeSpan.Zero) throw new InvalidOperationException("Expiration scan frequency must be greater than zero.");
  }
}
