namespace GameGuild;

public class ResponseCachingOptions
{
    public bool EnableCompression { get; set; } = true;

    public long MaximumBodySize { get; set; } = 64 * 1024 * 1024; // 64MB

    public bool UseCaseSensitivePaths { get; set; }

    public void Validate()
    {
        if (MaximumBodySize <= 0)
            throw new InvalidOperationException("Maximum body size must be greater than zero.");

        if (MaximumBodySize > long.MaxValue / 2)
            throw new InvalidOperationException("Maximum body size is too large.");
    }
}
