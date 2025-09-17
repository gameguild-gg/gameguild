namespace GameGuild;

public class ResponseCompressionOptions {
  public string[ ] MimeTypes { get; set; } = ["application/json", "text/plain"];

  public string CompressionLevel { get; set; } = "Optimal";

  public void Validate() {
    if (MimeTypes == null || MimeTypes.Length == 0) throw new InvalidOperationException("At least one MIME type must be specified for compression.");

    if (string.IsNullOrWhiteSpace(CompressionLevel)) throw new InvalidOperationException("Compression level cannot be null or empty.");

    var validCompressionLevels = new[ ] { "Optimal", "Fastest", "NoCompression", "SmallestSize" };

    if (!validCompressionLevels.Contains(CompressionLevel)) throw new InvalidOperationException($"Compression level must be one of: {string.Join(", ", validCompressionLevels)}");
  }
}
