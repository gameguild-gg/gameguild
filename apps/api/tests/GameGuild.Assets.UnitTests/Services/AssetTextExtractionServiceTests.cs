using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace GameGuild.Assets.UnitTests.Services;

public class AssetTextExtractionServiceTests
{
    [Fact]
    public async Task ExtractAsync_ForPlainTextAsset_ReturnsDecodedText()
    {
        var bytes = Encoding.UTF8.GetBytes("lease signed\nby tenant");
        var reference = CreateReference("text/plain", "lease.txt", bytes);
        var storage = CreateStorage(reference, bytes);

        var service = CreateService(storage);

        var result = await service.ExtractAsync(reference, CancellationToken.None);

        result.Text.Should().Be("lease signed by tenant");
        result.Source.Should().Be("text");
        result.UsedOcr.Should().BeFalse();
        result.Warnings.Should().BeEmpty();
    }

    [Fact]
    public async Task ExtractAsync_ForPdfWithEmbeddedText_ReturnsInlineText()
    {
        var pdf = Encoding.Latin1.GetBytes("%PDF-1.4\n1 0 obj\n<<>>\nstream\nBT\n(Lease Agreement) Tj\n(Tenant Name) Tj\nET\nendstream\nendobj\n%%EOF");
        var reference = CreateReference("application/pdf", "lease.pdf", pdf);
        var storage = CreateStorage(reference, pdf);

        var service = CreateService(storage);

        var result = await service.ExtractAsync(reference, CancellationToken.None);

        result.Text.Should().Be("Lease Agreement Tenant Name");
        result.Source.Should().Be("pdf-text");
        result.UsedOcr.Should().BeFalse();
    }

    [Fact]
    public async Task ExtractAsync_ForImageWithAzureVisionConfigured_UsesOcrAndReturnsText()
    {
        var bytes = Encoding.UTF8.GetBytes("png-bytes");
        var reference = CreateReference("image/png", "image.png", bytes);
        var storage = CreateStorage(reference, bytes);

        var handler = new StubHttpMessageHandler(
            _ =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.Accepted);
                response.Headers.Add("Operation-Location", "https://vision.example.test/operations/123");
                return response;
            },
            _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(@"{""status"":""succeeded"",""analyzeResult"":{""content"":""Lease scanned text""}}", Encoding.UTF8, "application/json")
            });

        var httpClientFactory = new StubHttpClientFactory(new HttpClient(handler));
        var options = Options.Create(new AssetTextExtractionOptions
        {
            EnableOcr = true,
            OcrProvider = AssetTextExtractionOptions.AzureVisionProvider,
            OcrEndpoint = "https://vision.example.test",
            OcrApiKey = "key",
            OcrPollingIntervalMs = 1,
            OcrMaxPollingAttempts = 1,
        });

        var service = new AssetTextExtractionService(storage.Object, httpClientFactory, options, NullLogger<AssetTextExtractionService>.Instance);

        var result = await service.ExtractAsync(reference, CancellationToken.None);

        result.Text.Should().Be("Lease scanned text");
        result.UsedOcr.Should().BeTrue();
        result.Source.Should().Be("ocr");
    }

    [Fact]
    public async Task ExtractAsync_ForImageWithoutOcrConfiguration_ReturnsWarning()
    {
        var bytes = Encoding.UTF8.GetBytes("png-bytes");
        var reference = CreateReference("image/png", "image.png", bytes);
        var storage = CreateStorage(reference, bytes);

        var httpClientFactory = new StubHttpClientFactory(
            new HttpClient(new StubHttpMessageHandler(
                _ => new HttpResponseMessage(HttpStatusCode.OK),
                _ => new HttpResponseMessage(HttpStatusCode.OK))));

        var service = new AssetTextExtractionService(
            storage.Object,
            httpClientFactory,
            Options.Create(new AssetTextExtractionOptions()),
            NullLogger<AssetTextExtractionService>.Instance);

        var result = await service.ExtractAsync(reference, CancellationToken.None);

        result.Text.Should().BeEmpty();
        result.UsedOcr.Should().BeFalse();
        result.Warnings.Should().Contain(w => w.Contains("OCR provider is not configured"));
    }

    private static Mock<IAssetStorageService> CreateStorage(AssetReference reference, byte[] bytes)
    {
        var storage = new Mock<IAssetStorageService>();
        storage
            .Setup(service => service.DownloadAsync(reference.Content.BucketName, reference.Content.ObjectKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream(bytes));

        return storage;
    }

    private static AssetReference CreateReference(string mimeType, string objectKey, byte[] bytes)
    {
        var content = new AssetContent("assets", objectKey, "hash", mimeType, bytes.Length, null, null)
        {
            VirusScanStatus = VirusScanStatus.Clean,
            ModerationStatus = ModerationStatus.Approved,
        };

        return new AssetReference(content.Id, Guid.NewGuid(), "scan", AssetAccessPolicy.Private, null, null)
        {
            Content = content,
        };
    }

    private static AssetTextExtractionService CreateService(Mock<IAssetStorageService> storage)
        => new(
            storage.Object,
            new StubHttpClientFactory(new HttpClient(new StubHttpMessageHandler(
                _ => new HttpResponseMessage(HttpStatusCode.OK),
                _ => new HttpResponseMessage(HttpStatusCode.OK)))),
            Options.Create(new AssetTextExtractionOptions()),
            NullLogger<AssetTextExtractionService>.Instance);

    private sealed class StubHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> postHandler,
        Func<HttpRequestMessage, HttpResponseMessage> getHandler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = request.Method == HttpMethod.Post
                ? postHandler(request)
                : getHandler(request);

            return Task.FromResult(response);
        }
    }
}
