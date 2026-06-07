namespace GameGuild.Assets;

public sealed class AssetTextExtractionOptions
{
    public const string SectionName = "Assets:TextExtraction";
    public const string AzureVisionProvider = "AzureVision";

    public bool Enabled { get; set; } = true;
    public bool OcrEnabled { get; set; }
    public bool EnableOcr
    {
        get => OcrEnabled;
        set => OcrEnabled = value;
    }

    public string? OcrProvider { get; set; }
    public string? OcrEndpoint { get; set; }
    public string? OcrApiKey { get; set; }
    public string OcrApiKeyHeader { get; set; } = "x-api-key";
    public string OcrTextPropertyName { get; set; } = "text";
    public int OcrMinPdfTextLength { get; set; } = 80;
    public int OcrPollingIntervalMs { get; set; } = 1000;
    public int OcrMaxPollingAttempts { get; set; } = 10;
    public bool FailOnOcrUnavailable { get; set; }
    public int MaxBytes { get; set; } = 2 * 1024 * 1024;
    public int MaxTextLength { get; set; } = 200_000;
    public string[] OcrMimeTypes { get; set; } =
    [
        "application/pdf",
        "image/png",
        "image/jpeg",
        "image/jpg",
        "image/webp",
        "image/tiff"
    ];
}
