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
    public async Task ValidateReceiptAsync_Should_Fail_When_PrivateKey_File_Missing()
    {
        var service = CreateService(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)), new ApplePaySettings
        {
            BundleId = "com.example.app",
            TeamId = "TEAM",
            KeyId = "KEY",
            PrivateKeyPath = Path.Combine(Path.GetTempPath(), $"missing_{Guid.NewGuid()}.p8")
        });

        var result = await service.ValidateReceiptAsync("receipt", "tx", "com.example.app", CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("App Store Server API JWT");
    }

    [Fact]
    public async Task ValidateReceiptAsync_Should_Fail_When_Transaction_Info_Missing()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
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
        result.ErrorMessage.Should().Contain("transaction info");
    }

    [Fact]
    public async Task ValidateReceiptAsync_Should_Fail_When_SignedTransaction_Invalid()
    {
        var header = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"alg\":\"ES256\",\"x5c\":[]}"))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes("{}"))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var signedTransaction = $"{header}.{payload}.sig";

        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent($"{{\"signedTransactionInfo\":\"{signedTransaction}\"}}", Encoding.UTF8, "application/json")
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
        result.ErrorMessage.Should().Contain("decode signed transaction");
    }

    [Fact]
    public async Task ValidateReceiptAsync_Should_Succeed_With_Valid_Signed_Transaction()
    {
        var (leafCert, rootCert, leafKey) = CreateAppleCertificateChain();
        using var _ = leafCert;
        using var __ = rootCert;
        using var ___ = leafKey;

        var signedTransaction = CreateSignedTransaction(leafCert, rootCert, leafKey, "com.example.app");

        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent($"{{\"signedTransactionInfo\":\"{signedTransaction}\"}}", Encoding.UTF8, "application/json")
        });

        var service = CreateService(handler, new ApplePaySettings
        {
            BundleId = "com.example.app",
            TeamId = "TEAM",
            KeyId = "KEY",
            PrivateKeyContent = CreatePrivateKeyPem()
        });

        var result = await service.ValidateReceiptAsync("receipt", "tx", "com.example.app", CancellationToken.None);

        result.IsValid.Should().BeTrue();
        result.ProductId.Should().Be("prod_1");
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
    public async Task VerifyNotificationAsync_Should_Succeed_With_Valid_Signed_Payload()
    {
        var (leafCert, rootCert, leafKey) = CreateAppleCertificateChain();
        using var _ = leafCert;
        using var __ = rootCert;
        using var ___ = leafKey;

        var signedTransaction = CreateSignedTransaction(leafCert, rootCert, leafKey, "com.example.app");
        var signedNotification = CreateSignedNotification(leafCert, rootCert, leafKey, "com.example.app", signedTransaction);

        var service = CreateService(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)), new ApplePaySettings
        {
            BundleId = "com.example.app",
            TeamId = "TEAM",
            KeyId = "KEY"
        });

        var result = await service.VerifyNotificationAsync(signedNotification, CancellationToken.None);

        result.IsValid.Should().BeTrue();
        result.NotificationType.Should().Be("SUBSCRIBED");
        result.ProductId.Should().Be("prod_1");
    }

    [Fact]
    public void Base64UrlDecodeBytes_Should_Decode_String()
    {
        var method = typeof(ApplePayReceiptValidationService)
            .GetMethod("Base64UrlDecodeBytes", BindingFlags.NonPublic | BindingFlags.Static);

        var bytes = (byte[])method!.Invoke(null, new object[] { "aGVsbG8" })!;

        Encoding.UTF8.GetString(bytes).Should().Be("hello");
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
    public void VerifyAppleCertificateChain_Should_Return_False_For_Non_Apple_Cert()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var request = new CertificateRequest("CN=Test", ecdsa, HashAlgorithmName.SHA256);
        using var cert = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));

        var certBase64 = Convert.ToBase64String(cert.Export(X509ContentType.Cert));

        var service = CreateService(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)), new ApplePaySettings
        {
            BundleId = "com.example.app",
            TeamId = "TEAM",
            KeyId = "KEY"
        });

        var method = typeof(ApplePayReceiptValidationService)
            .GetMethod("VerifyAppleCertificateChain", BindingFlags.NonPublic | BindingFlags.Instance);

        var result = (bool)method!.Invoke(service, new object[] { new[] { certBase64, certBase64 } })!;

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

    [Fact]
    public void VerifyJwsSignature_Should_Return_False_For_Invalid_Signature()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var request = new CertificateRequest("CN=Test", ecdsa, HashAlgorithmName.SHA256);
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
        var result = (bool)method!.Invoke(service, new object[] { parts, cert, "ES384" })!;

        result.Should().BeFalse();
    }

    [Fact]
    public void DecodeSignedTransaction_Should_Return_Null_When_Parts_Invalid()
    {
        var service = CreateService(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)), new ApplePaySettings
        {
            BundleId = "com.example.app",
            TeamId = "TEAM",
            KeyId = "KEY"
        });

        var method = typeof(ApplePayReceiptValidationService)
            .GetMethod("DecodeSignedTransaction", BindingFlags.NonPublic | BindingFlags.Instance);

        var result = method!.Invoke(service, new object[] { "a.b" });

        result.Should().BeNull();
    }

    [Fact]
    public void DecodeSignedTransaction_Should_Return_Null_When_Header_Missing_X5c()
    {
        var service = CreateService(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)), new ApplePaySettings
        {
            BundleId = "com.example.app",
            TeamId = "TEAM",
            KeyId = "KEY"
        });

        var header = Base64UrlEncode(Encoding.UTF8.GetBytes("{\"alg\":\"ES256\"}"));
        var payload = Base64UrlEncode(Encoding.UTF8.GetBytes("{}"));
        var signed = $"{header}.{payload}.sig";

        var method = typeof(ApplePayReceiptValidationService)
            .GetMethod("DecodeSignedTransaction", BindingFlags.NonPublic | BindingFlags.Instance);

        var result = method!.Invoke(service, new object[] { signed });

        result.Should().BeNull();
    }

    [Fact]
    public void DecodeSignedNotification_Should_Return_Null_When_Parts_Invalid()
    {
        var service = CreateService(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)), new ApplePaySettings
        {
            BundleId = "com.example.app",
            TeamId = "TEAM",
            KeyId = "KEY"
        });

        var method = typeof(ApplePayReceiptValidationService)
            .GetMethod("DecodeSignedNotification", BindingFlags.NonPublic | BindingFlags.Instance);

        var result = method!.Invoke(service, new object[] { "a.b" });

        result.Should().BeNull();
    }

    private static ApplePayReceiptValidationService CreateService(HttpMessageHandler handler, ApplePaySettings settings)
    {
        var httpClient = new HttpClient(handler);
        var options = Options.Create(settings);
        return new ApplePayReceiptValidationService(httpClient, options, NullLogger<ApplePayReceiptValidationService>.Instance);
    }

    private static X509Certificate2 CreateAppleLikeCertificate(out ECDsa key)
    {
        key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var request = new CertificateRequest("CN=Apple Root", key, HashAlgorithmName.SHA256);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.DigitalSignature, true));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
    }

    private static (X509Certificate2 Leaf, X509Certificate2 Root, ECDsa LeafKey) CreateAppleCertificateChain()
    {
        var rootKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var rootRequest = new CertificateRequest("CN=Apple Root CA", rootKey, HashAlgorithmName.SHA256);
        rootRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        rootRequest.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.DigitalSignature, true));
        rootRequest.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(rootRequest.PublicKey, false));
        var rootCert = rootRequest.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));

        var leafKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var leafRequest = new CertificateRequest("CN=Apple Leaf", leafKey, HashAlgorithmName.SHA256);
        leafRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        leafRequest.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
        leafRequest.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(leafRequest.PublicKey, false));

        var serial = new byte[8];
        RandomNumberGenerator.Fill(serial);
        var leafCert = leafRequest.Create(rootCert, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1), serial);

        return (leafCert, rootCert, leafKey);
    }

    private static string CreateSignedTransaction(X509Certificate2 leafCert, X509Certificate2 rootCert, ECDsa leafKey, string bundleId)
    {
        var headerJson = $"{{\"alg\":\"ES256\",\"x5c\":[\"{Convert.ToBase64String(leafCert.Export(X509ContentType.Cert))}\",\"{Convert.ToBase64String(rootCert.Export(X509ContentType.Cert))}\"]}}";
        var payloadJson = $"{{\"transactionId\":\"tx\",\"originalTransactionId\":\"orig\",\"bundleId\":\"{bundleId}\",\"productId\":\"prod_1\",\"purchaseDate\":1,\"expiresDate\":2,\"type\":\"auto\",\"environment\":\"Sandbox\"}}";

        var header = Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson));
        var payload = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));
        var signingInput = Encoding.UTF8.GetBytes($"{header}.{payload}");

        var signature = leafKey.SignData(signingInput, HashAlgorithmName.SHA256);
        var signaturePart = Base64UrlEncode(signature);

        return $"{header}.{payload}.{signaturePart}";
    }

    private static string CreateSignedNotification(X509Certificate2 leafCert, X509Certificate2 rootCert, ECDsa leafKey, string bundleId, string signedTransaction)
    {
        var headerJson = $"{{\"alg\":\"ES256\",\"x5c\":[\"{Convert.ToBase64String(leafCert.Export(X509ContentType.Cert))}\",\"{Convert.ToBase64String(rootCert.Export(X509ContentType.Cert))}\"]}}";
        var payloadJson = $"{{\"notificationType\":\"SUBSCRIBED\",\"subtype\":null,\"data\":{{\"bundleId\":\"{bundleId}\",\"environment\":\"Sandbox\",\"signedTransactionInfo\":\"{signedTransaction}\"}},\"version\":\"1.0\",\"signedDate\":1}}";

        var header = Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson));
        var payload = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));
        var signingInput = Encoding.UTF8.GetBytes($"{header}.{payload}");

        var signature = leafKey.SignData(signingInput, HashAlgorithmName.SHA256);
        var signaturePart = Base64UrlEncode(signature);

        return $"{header}.{payload}.{signaturePart}";
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
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