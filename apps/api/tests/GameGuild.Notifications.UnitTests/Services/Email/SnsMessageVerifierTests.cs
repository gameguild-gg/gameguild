using System.Text.Json;
using GameGuild.Email;
using GameGuild.Notifications.Services.Email;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace GameGuild.Notifications.UnitTests.Services.Email;

public sealed class SnsMessageVerifierTests
{
    private const string ConfiguredTopicArn = "arn:aws:sns:us-east-1:000000000000:email-events";
    private const string CertUrl = "https://sns.us-east-1.amazonaws.com/SimpleNotificationService-0000000000000000000000000000000000.pem";

    [Theory]
    [InlineData("not json")]
    [InlineData("")]
    [InlineData("{\"half\":\"baked\"")]
    public void Garbage_Body_Is_Malformed_Not_Throwing(string body)
    {
        var verifier = CreateVerifier(ConfiguredTopicArn);

        var result = verifier.ValidateRequest(body);

        var invalid = result.Should().BeOfType<SnsVerificationResult.Invalid>().Subject;
        invalid.Rejection.Should().Be(SnsRejectionReason.Malformed);
    }

    [Fact]
    public void Valid_Json_But_Incomplete_Envelope_Is_Malformed()
    {
        // Routing fields (Type/MessageId/Message) missing → 400; an envelope that HAS them but no
        // signature machinery is covered by the signature-rejection tests (401).
        var verifier = CreateVerifier(ConfiguredTopicArn);

        var result = verifier.ValidateRequest("""{"Type":"Notification","Message":"{}","SigningCertURL":"https://sns.us-east-1.amazonaws.com/c.pem","SignatureVersion":"1","Signature":"YWJj"}""");

        var invalid = result.Should().BeOfType<SnsVerificationResult.Invalid>().Subject;
        invalid.Rejection.Should().Be(SnsRejectionReason.Malformed);
    }

    [Fact]
    public void Unsigned_Valid_Json_Envelope_Is_Rejected_As_Signature()
    {
        // Deterministic signature-layer failure: SignatureVersion 3 makes IsMessageSignatureValid
        // throw AmazonClientException ("SignatureVersion is not a valid value") without network I/O.
        var verifier = CreateVerifier(ConfiguredTopicArn);

        var result = verifier.ValidateRequest(Envelope(signatureVersion: "3"));

        var invalid = result.Should().BeOfType<SnsVerificationResult.Invalid>().Subject;
        invalid.Rejection.Should().Be(SnsRejectionReason.Signature);
    }

    [Fact]
    public void Tampered_Payload_With_Copied_Signature_Is_Rejected_As_Signature()
    {
        // Hand-built signature can never validate: cert download from an unallocated SNS region
        // fails (AmazonClientException) or the signature compares false — either way, Signature.
        var verifier = CreateVerifier(ConfiguredTopicArn);

        var result = verifier.ValidateRequest(Envelope(
            certUrl: "https://sns.no-such-region-zzz.amazonaws.com/SimpleNotificationService-bogus.pem"));

        var invalid = result.Should().BeOfType<SnsVerificationResult.Invalid>().Subject;
        invalid.Rejection.Should().Be(SnsRejectionReason.Signature);
    }

    [Theory]
    [InlineData("http://sns.us-east-1.amazonaws.com/cert.pem")]           // not https
    [InlineData("https://evil.example.com/cert.pem")]                      // foreign host
    [InlineData("https://sns.us-east-1.amazonaws.com.evil.com/cert.pem")]  // suffix trick
    [InlineData("https://snsv2.us-east-1.amazonaws.com/cert.pem")]         // wrong service prefix
    [InlineData("")]                                                       // missing
    public void Untrusted_Signing_Cert_Url_Is_Rejected_Before_Any_Fetch(string certUrl)
    {
        var verifier = CreateVerifier(ConfiguredTopicArn);

        var result = verifier.ValidateRequest(Envelope(certUrl: certUrl));

        var invalid = result.Should().BeOfType<SnsVerificationResult.Invalid>().Subject;
        invalid.Rejection.Should().Be(SnsRejectionReason.Signature);
    }

    [Fact]
    public void Configured_Topic_Mismatch_Is_TopicMismatch()
    {
        var verifier = CreateVerifier(ConfiguredTopicArn);

        var result = verifier.ValidateRequest(Envelope(topicArn: "arn:aws:sns:eu-west-1:000000000000:other-topic"));

        var invalid = result.Should().BeOfType<SnsVerificationResult.Invalid>().Subject;
        invalid.Rejection.Should().Be(SnsRejectionReason.TopicMismatch);
    }

    [Fact]
    public void Matching_Configured_Topic_Passes_The_Topic_Gate()
    {
        // Correct topic → rejection can no longer be TopicMismatch; it fails later at the
        // signature layer (SignatureVersion 3 → SDK throw), proving the gate passed.
        var verifier = CreateVerifier(ConfiguredTopicArn);

        var result = verifier.ValidateRequest(Envelope(signatureVersion: "3"));

        result.Should().BeOfType<SnsVerificationResult.Invalid>()
            .Which.Rejection.Should().Be(SnsRejectionReason.Signature);
    }

    [Fact]
    public void Production_Without_Configured_Topic_Is_TopicNotConfigured()
    {
        var verifier = CreateVerifier(topicArn: null, production: true);

        var result = verifier.ValidateRequest(Envelope());

        var invalid = result.Should().BeOfType<SnsVerificationResult.Invalid>().Subject;
        invalid.Rejection.Should().Be(SnsRejectionReason.TopicNotConfigured);
    }

    [Fact]
    public void NonProduction_Without_Configured_Topic_Accepts_Any_Topic_And_Warns_Once()
    {
        var logger = new Mock<ILogger<SnsMessageVerifier>>();
        var verifier = CreateVerifier(topicArn: null, production: false, logger: logger.Object);

        var first = verifier.ValidateRequest(Envelope(signatureVersion: "3"));
        var second = verifier.ValidateRequest(Envelope(signatureVersion: "3"));

        first.Should().BeOfType<SnsVerificationResult.Invalid>()
            .Which.Rejection.Should().Be(SnsRejectionReason.Signature);
        second.Should().BeOfType<SnsVerificationResult.Invalid>()
            .Which.Rejection.Should().Be(SnsRejectionReason.Signature);
        logger.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((_, _) => true),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData("https://sns.us-east-1.amazonaws.com/cert.pem", true)]
    [InlineData("https://sns.eu-central-1.amazonaws.com/SimpleNotificationService-x.pem", true)]
    [InlineData("https://sns.us-east-1.amazonaws.com.evil.com/cert.pem", false)]
    [InlineData("http://sns.us-east-1.amazonaws.com/cert.pem", false)]
    [InlineData(null, false)]
    public void IsTrustedAwsSnsUrl_Pins_Host_And_Scheme(string? url, bool expected)
    {
        SnsMessageVerifier.IsTrustedAwsSnsUrl(url).Should().Be(expected);
    }

    private static string Envelope(
        string? topicArn = ConfiguredTopicArn,
        string signatureVersion = "1",
        string? certUrl = null,
        string type = "Notification") =>
        JsonSerializer.Serialize(new Dictionary<string, string?>
        {
            ["Type"] = type,
            ["MessageId"] = "sns-message-id-1",
            ["TopicArn"] = topicArn,
            ["Message"] = """{"eventType":"Send","mail":{"messageId":"ses-1","destination":["member@example.com"],"timestamp":"2026-01-15T08:00:00.000Z"}}""",
            ["Timestamp"] = "2026-01-15T08:00:00.000Z",
            ["SignatureVersion"] = signatureVersion,
            ["Signature"] = "YWJjZGVmZw==", // hand-built, never actually valid
            ["SigningCertURL"] = certUrl ?? CertUrl,
        });

    private static SnsMessageVerifier CreateVerifier(
        string? topicArn = null,
        bool production = false,
        ILogger<SnsMessageVerifier>? logger = null)
    {
        var options = Options.Create(new EmailDeliveryOptions());
        options.Value.Events.TopicArn = topicArn;
        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(e => e.EnvironmentName).Returns(production ? "Production" : "Development");
        return new SnsMessageVerifier(options, environment.Object, logger ?? NullLogger<SnsMessageVerifier>.Instance);
    }
}
