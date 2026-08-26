using System.Net;
using System.Text;
using Amazon.S3;
using Amazon.S3.Model;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace GameGuild.Compliance.KYC.Tests;

public sealed class SumSubProductionInfrastructureTests
{
    [Fact]
    public void ModuleAndModelConfigurationCoverTheProductionComposition()
    {
        var module = new KycModule();
        module.EnabledByDefault.Should().BeTrue();

        var disabled = module.ConfigureServices(new ServiceCollection(), new ConfigurationBuilder().Build());
        disabled.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IComplianceRawObjectStore) &&
            descriptor.ImplementationType == typeof(UnavailableComplianceRawObjectStore));

        var enabledConfiguration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                [$"{ComplianceRawObjectStoreOptions.SectionName}:Enabled"] = "true"
            }).Build();
        var enabled = new ServiceCollection().AddKycComposition(enabledConfiguration);
        enabled.Should().Contain(descriptor => descriptor.ServiceType == typeof(IAmazonS3));
        enabled.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IComplianceRawObjectStore) &&
            descriptor.ImplementationType == typeof(S3ComplianceRawObjectStore));

        var modelBuilder = new ModelBuilder();
        new SumSubEvidenceModelConfiguration().Configure(modelBuilder);
        modelBuilder.Model.GetEntityTypes().Should().HaveCount(2);
        FluentActions.Invoking(() => new SumSubEvidenceModelConfiguration().Configure(null!))
            .Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task S3StoreEncryptsAndHashesEvidenceWithoutExposingProviderIdentity()
    {
        PutObjectRequest? captured = null;
        var s3 = new Mock<IAmazonS3>();
        s3.Setup(client => client.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PutObjectRequest, CancellationToken>((request, _) => captured = request)
            .ReturnsAsync(new PutObjectResponse { ETag = "etag-1" });
        var store = Store(s3.Object, ValidOptions());
        var payload = Encoding.UTF8.GetBytes("sensitive-provider-payload");

        var result = await store.PutAsync(" SumSub ", " Production ", "event/secret", payload, CancellationToken.None);

        result.PayloadHash.Should().HaveLength(64);
        result.Reference.Should().StartWith("s3://compliance-bucket/evidence/sumsub/production/");
        captured.Should().NotBeNull();
        captured!.BucketName.Should().Be("compliance-bucket");
        captured.Key.Should().EndWith(result.PayloadHash + ".bin");
        captured.ServerSideEncryptionMethod.Should().Be(ServerSideEncryptionMethod.AWSKMS);
        captured.ServerSideEncryptionKeyManagementServiceKeyId.Should().Be("kms-key");
        captured.Metadata["payload-sha256"].Should().Be(result.PayloadHash);
        captured.Metadata["provider-event-hash"].Should().HaveLength(64);
    }

    [Fact]
    public async Task S3StoreFailsClosedForEveryMissingSettingAndInvalidInput()
    {
        var s3 = new Mock<IAmazonS3>();
        s3.Setup(client => client.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PutObjectResponse());
        Action nullClient = () => Store(null!, ValidOptions());
        Action nullOptions = () => new S3ComplianceRawObjectStore(s3.Object, null!);
        nullClient.Should().Throw<ArgumentNullException>();
        nullOptions.Should().Throw<ArgumentNullException>();

        foreach (var options in new[]
                 {
                     ValidOptions().With(enabled: false),
                     ValidOptions().With(bucketName: " "),
                     ValidOptions().With(kmsKeyId: " "),
                     ValidOptions().With(prefix: " ")
                 })
        {
            var act = () => Store(s3.Object, options).PutAsync("sumsub", "prod", "event", new byte[] { 1 }, CancellationToken.None).AsTask();
            await act.Should().ThrowAsync<ComplianceRawObjectStoreUnavailableException>();
        }

        var store = Store(s3.Object, ValidOptions());
        await FluentActions.Awaiting(() => store.PutAsync(" ", "prod", "event", new byte[] { 1 }, CancellationToken.None).AsTask())
            .Should().ThrowAsync<ArgumentException>();
        await FluentActions.Awaiting(() => store.PutAsync("sumsub", " ", "event", new byte[] { 1 }, CancellationToken.None).AsTask())
            .Should().ThrowAsync<ArgumentException>();
        await FluentActions.Awaiting(() => store.PutAsync("sumsub", "prod", " ", new byte[] { 1 }, CancellationToken.None).AsTask())
            .Should().ThrowAsync<ArgumentException>();
        await FluentActions.Awaiting(() => store.PutAsync("sumsub", "prod", "event", ReadOnlyMemory<byte>.Empty, CancellationToken.None).AsTask())
            .Should().ThrowAsync<ArgumentException>();
        await FluentActions.Awaiting(() => store.PutAsync("sumsub", "prod", "event", new byte[] { 1 }, CancellationToken.None).AsTask())
            .Should().ThrowAsync<ComplianceRawObjectStoreUnavailableException>()
            .WithMessage("*object identity*");

        var unavailable = new UnavailableComplianceRawObjectStore();
        await FluentActions.Awaiting(() => unavailable.PutAsync("sumsub", "prod", "event", new byte[] { 1 }, CancellationToken.None).AsTask())
            .Should().ThrowAsync<ComplianceRawObjectStoreUnavailableException>();
        new ComplianceRawObjectStoreUnavailableException("disabled").Message.Should().Be("disabled");
    }

    private static S3ComplianceRawObjectStore Store(
        IAmazonS3 s3,
        ComplianceRawObjectStoreOptions options) => new(s3, Options.Create(options));

    private static ComplianceRawObjectStoreOptions ValidOptions() => new()
    {
        Enabled = true,
        BucketName = "compliance-bucket",
        KmsKeyId = "kms-key",
        Prefix = "/evidence/"
    };
}

internal static class ComplianceRawObjectStoreOptionsExtensions
{
    internal static ComplianceRawObjectStoreOptions With(
        this ComplianceRawObjectStoreOptions source,
        bool? enabled = null,
        string? bucketName = null,
        string? kmsKeyId = null,
        string? prefix = null) => new()
    {
        Enabled = enabled ?? source.Enabled,
        BucketName = bucketName ?? source.BucketName,
        KmsKeyId = kmsKeyId ?? source.KmsKeyId,
        Prefix = prefix ?? source.Prefix
    };
}
