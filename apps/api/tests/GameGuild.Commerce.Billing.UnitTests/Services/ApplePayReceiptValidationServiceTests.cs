using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Xunit;

namespace GameGuild.Commerce.Billing.UnitTests.Services;

public class ApplePayReceiptValidationServiceTests
{
    [Fact]
    public async Task ValidateReceiptAsync_Should_Fail_On_BundleId_Mismatch()
    {
        var service = CreateService(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)), new ApplePaySettings
        {
            BundleId = "com.example.app",
            TeamId = "TEAM",
            KeyId = "KEY"
        });

        var result = await service.ValidateReceiptAsync("receipt", "tx", "com.other", CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Bundle ID mismatch");
    }

    [Fact]
    public async Task ValidateReceiptAsync_Should_Fail_When_Jwt_Cannot_Be_Generated()
    {
        var service = CreateService(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)), new ApplePaySettings
        {
            BundleId = "com.example.app",
            TeamId = "TEAM",
            KeyId = "KEY"
        });

        var result = await service.ValidateReceiptAsync("receipt", "tx", "com.example.app", CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("App Store Server API JWT");
    }

    [Fact]
    public async Task ValidateReceiptAsync_Should_Fail_When_Api_Returns_Error()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("error", Encoding.UTF8, "application/json")
        });

        var service = CreateService(handler, new ApplePaySettings
        {
            BundleId = "com.example.app",
            TeamId = "TEAM",
            KeyId = "KEY",
            PrivateKeyContent = CreatePrivateKeyPem()
        });

        var result = await service.ValidateReceiptAsync("receipt", "tx", "com.example.app", CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("API returned");
    }

    [Fact]
    public async Task VerifyNotificationAsync_Should_Fail_When_Payload_Invalid()
    {
        var service = CreateService(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)), new ApplePaySettings
        {
            BundleId = "com.example.app",
            TeamId = "TEAM",
            KeyId = "KEY"
        });

        var result = await service.VerifyNotificationAsync("invalid", CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("decode signed notification");
    }

    [Fact]
    public void Base64UrlDecode_Should_Decode_String()
    {
        var method = typeof(ApplePayReceiptValidationService)
            .GetMethod("Base64UrlDecode", BindingFlags.NonPublic | BindingFlags.Static);

        var decoded = (string)method!.Invoke(null, new object[] { "aGVsbG8" })!;

        decoded.Should().Be("hello");
    }

    [Fact]
    public void VerifyAppleCertificateChain_Should_Return_False_For_Short_Chain()
    {
        var service = CreateService(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)), new ApplePaySettings
        {
            BundleId = "com.example.app",
            TeamId = "TEAM",
            KeyId = "KEY"
        });

        var method = typeof(ApplePayReceiptValidationService)
            .GetMethod("VerifyAppleCertificateChain", BindingFlags.NonPublic | BindingFlags.Instance);

        var result = (bool)method!.Invoke(service, new object[] { new[] { "only" } })!;

        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyJwsSignature_Should_Return_False_For_Non_Ecdsa_Certificate()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=Test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var cert = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));

        var service = CreateService(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)), new ApplePaySettings
        {
            BundleId = "com.example.app",
            TeamId = "TEAM",
            KeyId = "KEY"
        });

        var method = typeof(ApplePayReceiptValidationService)
            .GetMethod("VerifyJwsSignature", BindingFlags.NonPublic | BindingFlags.Instance);

        var parts = new[] { "a", "b", "c" };
        var result = (bool)method!.Invoke(service, new object[] { parts, cert, "ES256" })!;

        result.Should().BeFalse();
    }

    private static ApplePayReceiptValidationService CreateService(HttpMessageHandler handler, ApplePaySettings settings)
    {
        var httpClient = new HttpClient(handler);
        var options = Options.Create(settings);
        return new ApplePayReceiptValidationService(httpClient, options, NullLogger<ApplePayReceiptValidationService>.Instance);
    }

    private static string CreatePrivateKeyPem()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var keyBytes = ecdsa.ExportPkcs8PrivateKey();
        var base64 = Convert.ToBase64String(keyBytes, Base64FormattingOptions.InsertLineBreaks);
        return $"-----BEGIN PRIVATE KEY-----\n{base64}\n-----END PRIVATE KEY-----";
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler = handler;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_handler(request));
    }
}