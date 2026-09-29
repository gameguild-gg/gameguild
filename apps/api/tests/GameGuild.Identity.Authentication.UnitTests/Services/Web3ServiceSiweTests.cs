using System.Globalization;
using System.Security.Cryptography;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Nethereum.Signer;
using Nethereum.Siwe.Core;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

public sealed class Web3ServiceSiweTests
{
    [Fact]
    public async Task GenerateChallengeAsync_ShouldReturnCanonicalSiweMessageBoundToConfiguredOriginAndChain()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = CreateService(cache, "https://gameguild.test", "1", "5");
        var key = CreateTestKey();

        var challenge = await service.GenerateChallengeAsync(key.GetPublicAddress(), chainId: "5");
        var parsed = SiweMessageParser.ParseUsingAbnf(challenge.Message);

        challenge.Message.Should().StartWith("gameguild.test wants you to sign in with your Ethereum account:");
        parsed.Domain.Should().Be("gameguild.test");
        parsed.Uri.Should().Be("https://gameguild.test/");
        parsed.Statement.Should().Be("Sign in with your Ethereum wallet.");
        parsed.Version.Should().Be("1");
        parsed.ChainId.Should().Be("5");
        parsed.Nonce.Should().Be(challenge.Nonce);
        parsed.Nonce.Should().MatchRegex("^[a-zA-Z0-9]{8,}$");
        parsed.IssuedAt.Should().Be(new DateTimeOffset(challenge.IssuedAt.ToUniversalTime()).ToString("O", CultureInfo.InvariantCulture));
        parsed.ExpirationTime.Should().Be(new DateTimeOffset(challenge.ExpiresAt.ToUniversalTime()).ToString("O", CultureInfo.InvariantCulture));
        challenge.ChainId.Should().Be("5");
    }

    [Fact]
    public async Task VerifySignatureAsync_ShouldAcceptValidEip191SignatureOnlyOnce()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = CreateService(cache, "https://gameguild.test", "1");
        var key = CreateTestKey();
        var challenge = await service.GenerateChallengeAsync(key.GetPublicAddress());
        var signature = Sign(challenge.Message, key);

        var firstAttempt = await service.VerifySignatureAsync(key.GetPublicAddress(), signature, challenge.Message, "1", challenge.Nonce);
        var replayAttempt = await service.VerifySignatureAsync(key.GetPublicAddress(), signature, challenge.Message, "1", challenge.Nonce);

        firstAttempt.Should().BeTrue();
        replayAttempt.Should().BeFalse();
    }

    [Fact]
    public async Task VerifySignatureAsync_ShouldRejectSignatureFromAnotherWallet()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = CreateService(cache, "https://gameguild.test", "1");
        var expectedKey = CreateTestKey();
        var otherKey = CreateTestKey();
        var challenge = await service.GenerateChallengeAsync(expectedKey.GetPublicAddress());

        var result = await service.VerifySignatureAsync(
            expectedKey.GetPublicAddress(),
            Sign(challenge.Message, otherKey),
            challenge.Message,
            "1",
            challenge.Nonce);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task VerifySignatureAsync_ShouldRejectMismatchedConfiguredOrigin()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var issuer = CreateService(cache, "https://gameguild.test", "1");
        var verifier = CreateService(cache, "https://attacker.test", "1");
        var key = CreateTestKey();
        var challenge = await issuer.GenerateChallengeAsync(key.GetPublicAddress());

        var result = await verifier.VerifySignatureAsync(
            key.GetPublicAddress(),
            Sign(challenge.Message, key),
            challenge.Message,
            "1",
            challenge.Nonce);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task VerifySignatureAsync_ShouldAcceptExplicitHttpSchemeForConfiguredLocalhost()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = CreateService(cache, "http://localhost:3000", "1");
        var key = CreateTestKey();
        var challenge = await service.GenerateChallengeAsync(key.GetPublicAddress());

        challenge.Message.Should().StartWith("http://localhost:3000 wants you to sign in with your Ethereum account:");
        var result = await service.VerifySignatureAsync(
            key.GetPublicAddress(),
            Sign(challenge.Message, key),
            challenge.Message,
            "1",
            challenge.Nonce);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task VerifySignatureAsync_ShouldRejectRequestChainAndNonceThatDoNotMatchChallenge()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = CreateService(cache, "https://gameguild.test", "1", "5");
        var key = CreateTestKey();
        var challenge = await service.GenerateChallengeAsync(key.GetPublicAddress());
        var signature = Sign(challenge.Message, key);

        var wrongChain = await service.VerifySignatureAsync(key.GetPublicAddress(), signature, challenge.Message, "5", challenge.Nonce);
        var wrongNonce = await service.VerifySignatureAsync(key.GetPublicAddress(), signature, challenge.Message, "1", "different-nonce");
        var correctRequest = await service.VerifySignatureAsync(key.GetPublicAddress(), signature, challenge.Message, "1", challenge.Nonce);

        wrongChain.Should().BeFalse();
        wrongNonce.Should().BeFalse();
        correctRequest.Should().BeTrue();
    }

    [Fact]
    public async Task VerifySignatureAsync_ShouldRejectIssuedAtDifferentFromStoredChallenge()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = CreateService(cache, "https://gameguild.test", "1");
        var key = CreateTestKey();
        var challenge = await service.GenerateChallengeAsync(key.GetPublicAddress());
        var issuedAt = new DateTimeOffset(challenge.IssuedAt.ToUniversalTime()).ToString("O", CultureInfo.InvariantCulture);
        var futureIssuedAt = DateTimeOffset.UtcNow.AddMinutes(10).ToString("O", CultureInfo.InvariantCulture);
        challenge.Message = challenge.Message.Replace($"Issued At: {issuedAt}", $"Issued At: {futureIssuedAt}", StringComparison.Ordinal);

        var result = await service.VerifySignatureAsync(
            key.GetPublicAddress(),
            Sign(challenge.Message, key),
            challenge.Message,
            "1",
            challenge.Nonce);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task VerifySignatureAsync_ShouldRejectExpirationDifferentFromStoredChallenge()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = CreateService(cache, "https://gameguild.test", "1");
        var key = CreateTestKey();
        var challenge = await service.GenerateChallengeAsync(key.GetPublicAddress());
        var expiration = new DateTimeOffset(challenge.ExpiresAt.ToUniversalTime()).ToString("O", CultureInfo.InvariantCulture);
        var extendedExpiration = new DateTimeOffset(challenge.ExpiresAt.AddMinutes(1).ToUniversalTime()).ToString("O", CultureInfo.InvariantCulture);
        challenge.Message = challenge.Message.Replace($"Expiration Time: {expiration}", $"Expiration Time: {extendedExpiration}", StringComparison.Ordinal);

        var result = await service.VerifySignatureAsync(
            key.GetPublicAddress(),
            Sign(challenge.Message, key),
            challenge.Message,
            "1",
            challenge.Nonce);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task GenerateWeb3ChallengeHandler_ShouldForwardChainAndReturnNonce()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = CreateService(cache, "https://gameguild.test", "1", "5");
        var key = CreateTestKey();
        var handler = new GenerateWeb3ChallengeHandler(service);

        var response = await handler.Handle(
            new GenerateWeb3ChallengeCommand { WalletAddress = key.GetPublicAddress(), ChainId = "5" },
            CancellationToken.None);
        var parsed = SiweMessageParser.ParseUsingAbnf(response.Challenge);

        parsed.ChainId.Should().Be("5");
        parsed.Nonce.Should().Be(response.Nonce);
        response.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task GenerateChallengeAsync_ShouldRejectChainOutsideConfiguredAllowlist()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = CreateService(cache, "https://gameguild.test", "1");
        var key = CreateTestKey();

        var act = () => service.GenerateChallengeAsync(key.GetPublicAddress(), chainId: "5");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GenerateChallengeAsync_ShouldUseHostConfiguredStatement()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = CreateService(cache, "https://tenant.example", ["1"], "Authenticate to tenant.example");
        var key = CreateTestKey();

        var challenge = await service.GenerateChallengeAsync(key.GetPublicAddress());
        var parsed = SiweMessageParser.ParseUsingAbnf(challenge.Message);

        parsed.Domain.Should().Be("tenant.example");
        parsed.Statement.Should().Be("Authenticate to tenant.example");
    }

    [Fact]
    public void Constructor_ShouldRequireHostConfiguredOrigin()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var configuration = new ConfigurationBuilder().AddInMemoryCollection([]).Build();

        var act = () => new Web3Service(NullLogger<Web3Service>.Instance, cache, configuration);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Origin must be configured*");
    }

    private static Web3Service CreateService(IMemoryCache cache, string origin, params string[] chainIds) =>
        CreateService(cache, origin, chainIds, statement: null);

    private static Web3Service CreateService(IMemoryCache cache, string origin, string[] chainIds, string? statement)
    {
        var settings = new Dictionary<string, string?>
        {
            ["Authentication:Web3:Siwe:Origin"] = origin
        };

        if (statement is not null)
        {
            settings["Authentication:Web3:Siwe:Statement"] = statement;
        }

        for (var index = 0; index < chainIds.Length; index++)
        {
            settings[$"Authentication:Web3:Siwe:AllowedChainIds:{index}"] = chainIds[index];
        }

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        return new Web3Service(NullLogger<Web3Service>.Instance, cache, configuration);
    }

    private static string Sign(string message, EthECKey key)
    {
        var signature = new EthereumMessageSigner().EncodeUTF8AndSign(message, key);
        return signature.StartsWith("0x", StringComparison.Ordinal) ? signature : $"0x{signature}";
    }

    private static EthECKey CreateTestKey() => new(Convert.ToHexString(RandomNumberGenerator.GetBytes(32)));
}
