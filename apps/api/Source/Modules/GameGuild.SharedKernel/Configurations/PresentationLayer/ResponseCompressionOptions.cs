namespace GameGuild.SharedKernel.Configuration;

public class ResponseCompressionOptions : BaseOptions
{
    public bool EnableCompression { get; set; } = false;

    public string[ ] MimeTypes { get; set; } = new[ ] { "text/plain", "application/json" };

    public static ResponseCompressionOptions CreateDefault() { return new ResponseCompressionOptions(); }
}
