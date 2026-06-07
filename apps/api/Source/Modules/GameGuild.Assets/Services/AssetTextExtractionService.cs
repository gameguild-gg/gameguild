using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.Assets;

public sealed class AssetTextExtractionService(
    IAssetStorageService storageService,
    IOptions<AssetTextExtractionOptions> options,
    IHttpClientFactory httpClientFactory,
    ILogger<AssetTextExtractionService> logger) : IAssetTextExtractionService
{
    public AssetTextExtractionService(
        IAssetStorageService storageService,
        IHttpClientFactory httpClientFactory,
        IOptions<AssetTextExtractionOptions> options,
        ILogger<AssetTextExtractionService> logger)
        : this(storageService, options, httpClientFactory, logger)
    {
    }

    public async Task<ExtractedAssetTextResult> ExtractAsync(AssetReference reference, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(reference);

        var content = reference.Content ?? throw new InvalidOperationException("Asset reference is missing content.");
        var currentOptions = options.Value;
        if (!currentOptions.Enabled)
        {
            return new ExtractedAssetTextResult(string.Empty, content.MimeType, "disabled", false, false, ["Asset text extraction is disabled."]);
        }

        var isTextLike = IsTextLike(content.MimeType);
        var isPdf = IsSimplePdf(content.MimeType);
        var isOcrCapable = IsOcrCapable(content.MimeType, currentOptions);

        if (!isTextLike && !isPdf && !isOcrCapable)
        {
            return new ExtractedAssetTextResult(string.Empty, content.MimeType, "unsupported", false, false, [$"MIME type '{content.MimeType}' is not supported for text extraction."]);
        }

        await using var stream = await storageService.DownloadAsync(content.BucketName, content.ObjectKey, ct);
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, ct);
        var bytes = buffer.ToArray();
        var warnings = new List<string>();

        if (bytes.Length > currentOptions.MaxBytes)
        {
            bytes = bytes[..currentOptions.MaxBytes];
            warnings.Add($"Input truncated to {currentOptions.MaxBytes} bytes before extraction.");
        }

        var text = isPdf
            ? ExtractReadablePdfText(bytes)
            : NormalizeExtractedText(Encoding.UTF8.GetString(bytes));
        var source = isPdf ? "pdf-text" : "text";
        var usedOcr = false;

        if ((!isTextLike && !isPdf) || (isPdf && text.Trim().Length < currentOptions.OcrMinPdfTextLength))
        {
            if (!currentOptions.OcrEnabled)
            {
                warnings.Add($"OCR provider is not configured or OCR is disabled for MIME type '{content.MimeType}'.");
                text = isTextLike || isPdf ? text : string.Empty;
            }
            else
            {
                var ocr = await TryExtractWithOcrAsync(bytes, content.MimeType, currentOptions, ct).ConfigureAwait(false);
                if (ocr.Success)
                {
                    text = ocr.Text ?? string.Empty;
                    source = IsAzureVisionProvider(currentOptions) ? "ocr" : $"ocr:{currentOptions.OcrProvider ?? "http"}";
                    usedOcr = true;
                    warnings.AddRange(ocr.Warnings);
                }
                else
                {
                    warnings.AddRange(ocr.Warnings);
                    if (currentOptions.FailOnOcrUnavailable)
                    {
                        throw new InvalidOperationException(string.Join(" ", ocr.Warnings));
                    }
                }
            }
        }

        var isTruncated = false;
        if (text.Length > currentOptions.MaxTextLength)
        {
            text = text[..currentOptions.MaxTextLength];
            isTruncated = true;
            warnings.Add($"Extracted text truncated to {currentOptions.MaxTextLength} characters.");
        }

        return new ExtractedAssetTextResult(text, content.MimeType, source, usedOcr, isTruncated, warnings);
    }

    private static bool IsTextLike(string mimeType)
        => mimeType.StartsWith("text/", StringComparison.OrdinalIgnoreCase)
           || mimeType.Contains("json", StringComparison.OrdinalIgnoreCase)
           || mimeType.Contains("xml", StringComparison.OrdinalIgnoreCase)
           || mimeType.Contains("csv", StringComparison.OrdinalIgnoreCase);

    private static bool IsSimplePdf(string mimeType)
        => mimeType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase);

    private static bool IsOcrCapable(string mimeType, AssetTextExtractionOptions options)
        => options.OcrMimeTypes.Any(current => current.Equals(mimeType, StringComparison.OrdinalIgnoreCase));

    private async Task<OcrExtractionResult> TryExtractWithOcrAsync(
        byte[] bytes,
        string mimeType,
        AssetTextExtractionOptions currentOptions,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(currentOptions.OcrEndpoint))
        {
            return OcrExtractionResult.Failure("OCR is enabled, but Assets:TextExtraction:OcrEndpoint is not configured.");
        }

        try
        {
            if (IsAzureVisionProvider(currentOptions))
            {
                return await TryExtractWithAzureVisionAsync(bytes, mimeType, currentOptions, ct).ConfigureAwait(false);
            }

            var client = httpClientFactory.CreateClient(nameof(AssetTextExtractionService));
            using var form = new MultipartFormDataContent();
            form.Add(new ByteArrayContent(bytes)
            {
                Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mimeType) }
            }, "file", "asset");
            form.Add(new StringContent(mimeType), "mimeType");

            using var request = new HttpRequestMessage(HttpMethod.Post, currentOptions.OcrEndpoint)
            {
                Content = form
            };

            if (!string.IsNullOrWhiteSpace(currentOptions.OcrApiKey))
            {
                request.Headers.TryAddWithoutValidation(currentOptions.OcrApiKeyHeader, currentOptions.OcrApiKey);
            }

            using var response = await client.SendAsync(request, ct).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("OCR provider returned {StatusCode}: {Body}", response.StatusCode, body);
                return OcrExtractionResult.Failure($"OCR provider returned {(int)response.StatusCode} {response.ReasonPhrase}.");
            }

            using var payload = JsonDocument.Parse(body);
            var text = FindStringProperty(payload.RootElement, currentOptions.OcrTextPropertyName)
                       ?? FindStringProperty(payload.RootElement, "text")
                       ?? FindStringProperty(payload.RootElement, "content");

            return string.IsNullOrWhiteSpace(text)
                ? OcrExtractionResult.Failure("OCR provider response did not include extracted text.")
                : OcrExtractionResult.SuccessResult(NormalizeExtractedText(text));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "OCR extraction failed for MIME type {MimeType}", mimeType);
            return OcrExtractionResult.Failure($"OCR extraction failed: {ex.Message}");
        }
    }

    private async Task<OcrExtractionResult> TryExtractWithAzureVisionAsync(
        byte[] bytes,
        string mimeType,
        AssetTextExtractionOptions currentOptions,
        CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient(nameof(AssetTextExtractionService));
        using var request = new HttpRequestMessage(HttpMethod.Post, currentOptions.OcrEndpoint)
        {
            Content = new ByteArrayContent(bytes)
            {
                Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mimeType) }
            }
        };

        if (!string.IsNullOrWhiteSpace(currentOptions.OcrApiKey))
        {
            request.Headers.TryAddWithoutValidation("Ocp-Apim-Subscription-Key", currentOptions.OcrApiKey);
            request.Headers.TryAddWithoutValidation(currentOptions.OcrApiKeyHeader, currentOptions.OcrApiKey);
        }

        using var response = await client.SendAsync(request, ct).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Azure Vision OCR returned {StatusCode}: {Body}", response.StatusCode, body);
            return OcrExtractionResult.Failure($"Azure Vision OCR returned {(int)response.StatusCode} {response.ReasonPhrase}.");
        }

        if (!response.Headers.TryGetValues("Operation-Location", out var operationLocations))
        {
            return ExtractOcrTextFromBody(body);
        }

        var operationLocation = operationLocations.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(operationLocation))
        {
            return OcrExtractionResult.Failure("Azure Vision OCR response did not include Operation-Location.");
        }

        var attempts = Math.Max(1, currentOptions.OcrMaxPollingAttempts);
        for (var attempt = 0; attempt < attempts; attempt++)
        {
            if (currentOptions.OcrPollingIntervalMs > 0)
            {
                await Task.Delay(currentOptions.OcrPollingIntervalMs, ct).ConfigureAwait(false);
            }

            using var pollResponse = await client.GetAsync(operationLocation, ct).ConfigureAwait(false);
            var pollBody = await pollResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            if (!pollResponse.IsSuccessStatusCode)
            {
                logger.LogWarning("Azure Vision OCR polling returned {StatusCode}: {Body}", pollResponse.StatusCode, pollBody);
                return OcrExtractionResult.Failure($"Azure Vision OCR polling returned {(int)pollResponse.StatusCode} {pollResponse.ReasonPhrase}.");
            }

            using var payload = JsonDocument.Parse(pollBody);
            var status = FindStringProperty(payload.RootElement, "status");
            if (string.Equals(status, "succeeded", StringComparison.OrdinalIgnoreCase))
            {
                return ExtractOcrTextFromPayload(payload.RootElement);
            }

            if (string.Equals(status, "failed", StringComparison.OrdinalIgnoreCase))
            {
                return OcrExtractionResult.Failure("Azure Vision OCR failed.");
            }
        }

        return OcrExtractionResult.Failure("Azure Vision OCR did not complete before the polling limit.");
    }

    private OcrExtractionResult ExtractOcrTextFromBody(string body)
    {
        using var payload = JsonDocument.Parse(body);
        return ExtractOcrTextFromPayload(payload.RootElement);
    }

    private OcrExtractionResult ExtractOcrTextFromPayload(JsonElement root)
    {
        var currentOptions = options.Value;
        var text = FindStringProperty(root, currentOptions.OcrTextPropertyName)
                   ?? FindStringProperty(root, "text")
                   ?? FindStringProperty(root, "content");

        return string.IsNullOrWhiteSpace(text)
            ? OcrExtractionResult.Failure("OCR provider response did not include extracted text.")
            : OcrExtractionResult.SuccessResult(NormalizeExtractedText(text));
    }

    private static string? FindStringProperty(JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (property.NameEquals(propertyName) && property.Value.ValueKind == JsonValueKind.String)
                {
                    return property.Value.GetString();
                }

                var nested = FindStringProperty(property.Value, propertyName);
                if (!string.IsNullOrWhiteSpace(nested))
                {
                    return nested;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var nested = FindStringProperty(item, propertyName);
                if (!string.IsNullOrWhiteSpace(nested))
                {
                    return nested;
                }
            }
        }

        return null;
    }

    private static string ExtractReadablePdfText(byte[] bytes)
    {
        var raw = Encoding.Latin1.GetString(bytes);
        var literals = new List<string>();

        for (var index = 0; index < raw.Length; index++)
        {
            if (raw[index] != '(')
            {
                continue;
            }

            var builder = new StringBuilder();
            var escaped = false;
            for (index++; index < raw.Length; index++)
            {
                var ch = raw[index];
                if (escaped)
                {
                    builder.Append(ch switch
                    {
                        'n' => '\n',
                        'r' => '\r',
                        't' => '\t',
                        'b' => '\b',
                        'f' => '\f',
                        _ => ch,
                    });
                    escaped = false;
                    continue;
                }

                if (ch == '\\')
                {
                    escaped = true;
                    continue;
                }

                if (ch == ')')
                {
                    break;
                }

                builder.Append(ch);
            }

            var literal = NormalizeExtractedText(builder.ToString());
            if (literal.Any(char.IsLetterOrDigit))
            {
                literals.Add(literal);
            }
        }

        return NormalizeExtractedText(string.Join(" ", literals));
    }

    private static string NormalizeExtractedText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(text.Length);
        var inWhitespace = false;
        foreach (var ch in text)
        {
            if (char.IsWhiteSpace(ch))
            {
                if (!inWhitespace)
                {
                    builder.Append(' ');
                    inWhitespace = true;
                }

                continue;
            }

            builder.Append(ch);
            inWhitespace = false;
        }

        return builder.ToString().Trim();
    }

    private static bool IsAzureVisionProvider(AssetTextExtractionOptions options)
        => string.Equals(options.OcrProvider, AssetTextExtractionOptions.AzureVisionProvider, StringComparison.OrdinalIgnoreCase);

    private sealed record OcrExtractionResult(bool Success, string? Text, IReadOnlyList<string> Warnings)
    {
        public static OcrExtractionResult SuccessResult(string text) => new(true, text, Array.Empty<string>());
        public static OcrExtractionResult Failure(string warning) => new(false, null, [warning]);
    }
}
