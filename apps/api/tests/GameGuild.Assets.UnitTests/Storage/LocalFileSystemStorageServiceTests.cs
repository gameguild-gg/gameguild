using System.Text;
using FluentAssertions;
using GameGuild.Assets.Storage;
using Xunit;

namespace GameGuild.Assets.UnitTests;

public class LocalFileSystemStorageServiceTests
{
    [Fact]
    public async Task UploadDownloadMetadataAndDelete_RoundTripsLocalObject()
    {
        var root = Path.Combine(Path.GetTempPath(), $"gameguild-assets-{Guid.NewGuid():N}");

        try
        {
            var service = new LocalFileSystemStorageService(
                new LocalFileSystemConfiguration { BasePath = root },
                "assets",
                "assets-transformed",
                "assets-quarantine");
            var bytes = Encoding.UTF8.GetBytes("hello local storage");
            var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)).ToLowerInvariant();

            await using var upload = new MemoryStream(bytes);
            var result = await service.UploadAsync(upload, hash, "text/plain");

            result.BucketName.Should().Be("assets");
            result.ObjectKey.Should().EndWith(".txt");
            (await service.ExistsAsync(result.BucketName, result.ObjectKey)).Should().BeTrue();

            var metadata = await service.GetMetadataAsync(result.BucketName, result.ObjectKey);
            metadata.Should().NotBeNull();
            metadata!.SizeBytes.Should().Be(bytes.Length);
            metadata.MimeType.Should().Be("text/plain");

            await using (var download = await service.DownloadAsync(result.BucketName, result.ObjectKey))
            using (var reader = new StreamReader(download, Encoding.UTF8))
            {
                (await reader.ReadToEndAsync()).Should().Be("hello local storage");
            }

            await service.DeleteAsync(result.BucketName, result.ObjectKey);
            (await service.ExistsAsync(result.BucketName, result.ObjectKey)).Should().BeFalse();
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ExistsAsync_RejectsPathTraversalKeys()
    {
        var root = Path.Combine(Path.GetTempPath(), $"gameguild-assets-{Guid.NewGuid():N}");

        try
        {
            var service = new LocalFileSystemStorageService(
                new LocalFileSystemConfiguration { BasePath = root },
                "assets",
                "assets-transformed",
                "assets-quarantine");

            var act = () => service.ExistsAsync("assets", "../escape.txt");

            await act.Should().ThrowAsync<InvalidOperationException>();
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
