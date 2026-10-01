using System.Security.Cryptography;
using FluentAssertions;
using GameGuild.Compliance.Audit;
using Microsoft.Extensions.Options;
using Xunit;

namespace GameGuild.Tests.Audit.Unit.Services;

public sealed class EcdsaCryptographicSigningServiceTests
{
    [Fact]
    public async Task SignData_ShouldVerifyAndRejectChangedContent()
    {
        var keys = CreateKeys();
        var service = CreateService(keys);
        var data = "audit-chain-hash";

        var signature = service.SignData(data, "current");

        service.VerifySignature(data, signature, "current").Should().BeTrue();
        service.VerifySignature("changed-content", signature, "current").Should().BeFalse();
        service.VerifySignature(data, signature, "unknown").Should().BeFalse();

        var publicKey = await service.GetPublicKeyAsync("current");
        publicKey.IsSuccess.Should().BeTrue();
        publicKey.Value.Should().Contain("PUBLIC KEY");
        publicKey.Value.Should().NotContain("PRIVATE KEY");
    }

    [Fact]
    public async Task RotateSigningKey_ShouldUseOnlyPreconfiguredPrivateKeys()
    {
        var service = CreateService(CreateKeys());

        var rotated = await service.RotateSigningKeyAsync("next");

        rotated.IsSuccess.Should().BeTrue();
        service.GetActiveKeyId().Should().Be("next");
        var signature = service.SignData("chain", service.GetActiveKeyId());
        service.VerifySignature("chain", signature, "next").Should().BeTrue();

        var missing = await service.RotateSigningKeyAsync("not-configured");
        missing.IsFailure.Should().BeTrue();
        service.GetActiveKeyId().Should().Be("next");
    }

    [Fact]
    public void ComputeHashes_ShouldBeDeterministicAndSequenceSensitive()
    {
        var service = CreateService(CreateKeys());

        var contentHash = service.ComputeContentHash("canonical-event");

        service.ComputeContentHash("canonical-event").Should().Be(contentHash);
        service.ComputeChainHash(contentHash, string.Empty, 1).Should().NotBe(
            service.ComputeChainHash(contentHash, string.Empty, 2));
    }

    private static EcdsaCryptographicSigningService CreateService(AuditSigningOptions options)
        => new(Options.Create(options));

    private static AuditSigningOptions CreateKeys()
    {
        return new AuditSigningOptions
        {
            ActiveKeyId = "current",
            Keys = new Dictionary<string, AuditSigningKeyOptions>(StringComparer.Ordinal)
            {
                ["current"] = CreateKey(),
                ["next"] = CreateKey()
            }
        };
    }

    private static AuditSigningKeyOptions CreateKey()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        return new AuditSigningKeyOptions
        {
            PrivateKeyPem = key.ExportPkcs8PrivateKeyPem(),
            PublicKeyPem = key.ExportSubjectPublicKeyInfoPem()
        };
    }
}
