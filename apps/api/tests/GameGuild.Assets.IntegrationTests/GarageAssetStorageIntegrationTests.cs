using System.Net;
using System.Security.Cryptography;
using System.Text;
using Amazon.S3;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace GameGuild.Assets.IntegrationTests;

public sealed class GarageAssetStorageIntegrationTests
{
    private const string Endpoint = "http://localhost:3900";
    private const string AccessKey = "GK111111111111111111111111";
    private const string SecretKey = "2222222222222222222222222222222222222222222222222222222222222222";
    private const string Region = "garage";

    [Fact]
    public async Task AssetStorageService_RoundTripsContentThroughLocalGarage()
    {
        await AssertGarageIsAvailableAsync();

        const string bucketName = "assets";
        var options = Options.Create(new AssetStorageOptions
        {
            BucketName = bucketName,
            ServiceUrl = Endpoint,
            AccessKey = AccessKey,
            SecretKey = SecretKey,
            Region = Region,
            ForcePathStyle = true
        });

        using var s3Client = CreateGarageClient();
        var storage = new AssetStorageService(s3Client, options);
        var payload = "garage-storage-smoke-" + Guid.NewGuid().ToString("N");
        var bytes = Encoding.UTF8.GetBytes(payload);
        var contentHash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

        StorageUploadResult upload;
        await using (var uploadStream = new MemoryStream(bytes))
        {
            upload = await storage.UploadAsync(uploadStream, contentHash, "text/plain");
        }

        upload.BucketName.Should().Be(bucketName);
        upload.ObjectKey.Should().EndWith(".txt");

        (await storage.ExistsAsync(upload.BucketName, upload.ObjectKey)).Should().BeTrue();

        var metadata = await storage.GetMetadataAsync(upload.BucketName, upload.ObjectKey);
        metadata.Should().NotBeNull();
        metadata!.SizeBytes.Should().Be(bytes.Length);
        metadata.MimeType.Should().Be("text/plain");

        await using (var downloadStream = await storage.DownloadAsync(upload.BucketName, upload.ObjectKey))
        using (var reader = new StreamReader(downloadStream, Encoding.UTF8))
        {
            var downloaded = await reader.ReadToEndAsync();
            downloaded.Should().Be(payload);
        }

        var downloadUrl = await storage.GeneratePresignedUrlAsync(
            upload.BucketName,
            upload.ObjectKey,
            TimeSpan.FromMinutes(5));

        downloadUrl.Should().StartWith(Endpoint);

        using var http = new HttpClient();
        var presignedResponse = await http.GetAsync(downloadUrl);
        presignedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await presignedResponse.Content.ReadAsStringAsync()).Should().Be(payload);

        await storage.DeleteAsync(upload.BucketName, upload.ObjectKey);

        (await storage.ExistsAsync(upload.BucketName, upload.ObjectKey)).Should().BeFalse();
    }

    private static AmazonS3Client CreateGarageClient()
    {
        return new AmazonS3Client(
            AccessKey,
            SecretKey,
            new AmazonS3Config
            {
                ServiceURL = Endpoint,
                AuthenticationRegion = Region,
                ForcePathStyle = true,
                UseHttp = true
            });
    }

    private static async Task AssertGarageIsAvailableAsync()
    {
        using var http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        try
        {
            using var response = await http.GetAsync(Endpoint);
            response.StatusCode.Should().NotBe(0);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Local Garage is required for this integration test. Start it from web-development with: docker compose --profile storage up -d garage garage-init",
                ex);
        }
    }
}
