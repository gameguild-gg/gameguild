using System.Reflection;
using System.Text;

namespace GameGuild.Social.Assets.UnitTests;

public sealed class SocialMediaPolicyCoverageTests
{
    [Fact]
    public void SocialMediaPolicy_CoversNormalizationSupportAndProcessingStates()
    {
        SocialMediaAssetPolicy.NormalizeMimeType(null).Should().BeEmpty();
        SocialMediaAssetPolicy.NormalizeMimeType(" IMAGE/PNG ; charset=binary ").Should().Be("image/png");
        SocialMediaAssetPolicy.IsSupported("application/xml", 1).Should().BeFalse();
        SocialMediaAssetPolicy.IsSupported("image/png", 0).Should().BeFalse();
        SocialMediaAssetPolicy.IsSupported("image/png", SocialMediaAssetPolicy.ImageLimitBytes + 1).Should().BeFalse();
        SocialMediaAssetPolicy.IsSupported("image/png", SocialMediaAssetPolicy.ImageLimitBytes).Should().BeTrue();

        var content = CreateContent();
        content.VirusScanStatus = VirusScanStatus.Infected;
        SocialMediaAssetPolicy.GetProcessingState(content).Should().Be(SocialMediaProcessingState.Rejected);
        content.VirusScanStatus = VirusScanStatus.ScanFailed;
        SocialMediaAssetPolicy.GetProcessingState(content).Should().Be(SocialMediaProcessingState.Rejected);
        content.VirusScanStatus = VirusScanStatus.Clean;
        content.ModerationStatus = ModerationStatus.Blocked;
        SocialMediaAssetPolicy.GetProcessingState(content).Should().Be(SocialMediaProcessingState.Rejected);
        content.ModerationStatus = ModerationStatus.Rejected;
        SocialMediaAssetPolicy.GetProcessingState(content).Should().Be(SocialMediaProcessingState.Rejected);
        content.ModerationStatus = ModerationStatus.Approved;
        SocialMediaAssetPolicy.GetProcessingState(content).Should().Be(SocialMediaProcessingState.Ready);
        content.ModerationStatus = ModerationStatus.Pending;
        SocialMediaAssetPolicy.GetProcessingState(content).Should().Be(SocialMediaProcessingState.Processing);
    }

    [Fact]
    public async Task SocialMediaPolicy_RecognizesEverySignatureAndRejectsEveryAlteredByte()
    {
        var signatures = new Dictionary<string, (byte[] Bytes, int[] SignificantIndexes)>
        {
            ["image/jpeg"] = ([0xff, 0xd8, 0xff], [0, 1, 2]),
            ["image/png"] = ([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a], [0, 1, 2, 3, 4, 5, 6, 7]),
            ["image/gif"] = (Encoding.ASCII.GetBytes("GIF87a"), [0, 1, 2, 3, 4, 5]),
            ["image/webp"] = (Encoding.ASCII.GetBytes("RIFFxxxxWEBP"), [0, 1, 2, 3, 8, 9, 10, 11]),
            ["video/mp4"] = ([0, 0, 0, 8, (byte)'f', (byte)'t', (byte)'y', (byte)'p'], [4, 5, 6, 7])
        };

        foreach (var (mimeType, signature) in signatures)
        {
            (await ValidateAsync(mimeType, signature.Bytes)).IsValid.Should().BeTrue(mimeType);
            (await ValidateAsync(mimeType, signature.Bytes[..^1])).IsValid.Should().BeFalse(mimeType);

            foreach (var index in signature.SignificantIndexes)
            {
                var altered = signature.Bytes.ToArray();
                altered[index] ^= 0xff;
                (await ValidateAsync(mimeType, altered)).IsValid.Should().BeFalse($"{mimeType} byte {index}");
            }
        }

        (await ValidateAsync("image/gif", Encoding.ASCII.GetBytes("GIF89a"))).IsValid.Should().BeTrue();
        (await ValidateAsync("application/xml", [1, 2, 3])).IsValid.Should().BeFalse();

        var signatureMethod = typeof(SocialMediaAssetPolicy)
            .GetMethod("SignatureMatches", BindingFlags.Static | BindingFlags.NonPublic)!;
        var signatureMatcher = signatureMethod.CreateDelegate<SignatureMatcher>();
        signatureMatcher("application/xml", ReadOnlySpan<byte>.Empty).Should().BeFalse();
    }

    private static async Task<SocialMediaValidationResult> ValidateAsync(string mimeType, byte[] bytes)
    {
        await using var content = new MemoryStream(bytes);
        return await SocialMediaAssetPolicy.ValidateAsync(content, mimeType, bytes.Length);
    }

    private static AssetContent CreateContent() => new(
        "assets", "object-key", new string('a', 64), "image/png", 128, 32, 32);

    private delegate bool SignatureMatcher(string mimeType, ReadOnlySpan<byte> header);
}
