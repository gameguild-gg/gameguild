using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace GameGuild.Commerce.Billing.UnitTests.Services;

public class BraintreeWebhookVerificationServiceTests
{
    private const string ConfiguredPublicKey = "test_public_key_20261031";

    [Fact]
    public void Verify_Should_Fail_Closed_When_PublicKey_NotConfigured()
    {
        var service = CreateService(publicKey: "");

        var result = service.Verify("key|digest", Base64Of(SampleXmlPayload()));

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not configured");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("nodigestseparator")]
    [InlineData("|onlydigest")]
    [InlineData("onlykey|")]
    public void Verify_Should_Reject_Malformed_Signatures(string signature)
    {
        var service = CreateService();

        var result = service.Verify(signature, Base64Of(SampleXmlPayload()));

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("bt_signature");
    }

    [Fact]
    public void Verify_Should_Reject_Payload_Over_The_Size_Bound()
    {
        var service = CreateService();
        var payload = Base64Of(SampleXmlPayload() + new string(' ', 1_100_000));

        var signature = Sign(ConfiguredPublicKey, payload);

        var result = service.Verify(signature, payload);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("size bound");
    }

    [Fact]
    public void Verify_Should_Reject_Delivery_Signed_By_A_Different_Key()
    {
        var service = CreateService();
        var payload = Base64Of(SampleXmlPayload());

        var signature = Sign("other_public_key", payload);

        var result = service.Verify(signature, payload);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("does not match");
    }

    [Fact]
    public void Verify_Should_Reject_Tampered_Payload()
    {
        var service = CreateService();
        var payload = Base64Of(SampleXmlPayload());
        var tampered = Base64Of(SampleXmlPayload().Replace("29.00", "1.00"));

        var signature = Sign(ConfiguredPublicKey, payload);

        var result = service.Verify(signature, tampered);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("digest mismatch");
    }

    [Fact]
    public void Verify_Should_Succeed_And_Decode_Xml_Notification()
    {
        var service = CreateService();
        var payload = Base64Of(SampleXmlPayload());

        var result = service.Verify(Sign(ConfiguredPublicKey, payload), payload);

        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        result.Notification.Should().NotBeNull();
        result.Notification!.Kind.Should().Be("subscription_charged_successfully");
        result.Notification.SubscriptionId.Should().Be("sub_7j8k9l");
        result.Notification.SubscriptionStatus.Should().Be("active");
        result.Notification.TransactionId.Should().Be("txn_5f6g7h");
        result.Notification.TransactionStatus.Should().Be("submitted_for_settlement");
        result.Notification.Amount.Should().Be(29.00m);
        result.Notification.CurrencyIsoCode.Should().Be("USD");
        result.Notification.TimestampUtc.Should().HaveValue();
    }

    [Fact]
    public void Verify_Should_Succeed_And_Decode_Json_Notification()
    {
        var service = CreateService();
        var payload = Base64Of(
            """
            {"kind":"subscription_canceled","timestamp":"2026-10-31T10:00:00Z","subject":{"subscription":{"id":"sub_json_1","status":"canceled"}}}
            """);

        var result = service.Verify(Sign(ConfiguredPublicKey, payload), payload);

        result.IsValid.Should().BeTrue();
        result.Notification!.Kind.Should().Be("subscription_canceled");
        result.Notification.SubscriptionId.Should().Be("sub_json_1");
        result.Notification.SubscriptionStatus.Should().Be("canceled");
    }

    [Fact]
    public void Verify_Should_Accept_UrlSafe_Base64_Payload_Variants()
    {
        var service = CreateService();
        var standard = Base64Of(SampleXmlPayload());
        var urlSafe = standard.Replace('+', '-').Replace('/', '_').TrimEnd('=');

        var result = service.Verify(Sign(ConfiguredPublicKey, urlSafe), urlSafe);

        result.IsValid.Should().BeTrue();
        result.Notification!.Kind.Should().Be("subscription_charged_successfully");
    }

    [Fact]
    public void Verify_Should_Reject_Undecodable_Payload()
    {
        var service = CreateService();
        const string payload = "!!not-base64!!";

        var result = service.Verify(Sign(ConfiguredPublicKey, payload), payload);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("decoded");
    }

    [Fact]
    public void Verify_Should_Reject_Notification_Without_Kind()
    {
        var service = CreateService();
        var payload = Base64Of("<notification><subject><transaction><id>t1</id></transaction></subject></notification>");

        var result = service.Verify(Sign(ConfiguredPublicKey, payload), payload);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("kind");
    }

    [Fact]
    public void Verify_Should_Not_Process_Xml_External_Entities()
    {
        // The XML parser must prohibit DTD processing: a hostile payload declaring an
        // entity must fail to decode rather than resolve anything.
        var service = CreateService();
        const string hostile =
            """<?xml version="1.0"?><!DOCTYPE notification [<!ENTITY xxe SYSTEM "file:///etc/passwd">]><notification><kind>&xxe;</kind></notification>""";
        var payload = Base64Of(hostile);

        var result = service.Verify(Sign(ConfiguredPublicKey, payload), payload);

        result.IsValid.Should().BeFalse();
    }

    private static BraintreeWebhookVerificationService CreateService(string publicKey = ConfiguredPublicKey) =>
        new(
            Options.Create(new BraintreeSettings { Environment = "sandbox", PublicKey = publicKey }),
            NullLogger<BraintreeWebhookVerificationService>.Instance);

    /// <summary>Produces a valid <c>bt_signature</c> for the given key and raw payload.</summary>
    internal static string Sign(string publicKey, string btPayload)
    {
        var digest = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(publicKey + "|" + btPayload))).ToLowerInvariant();
        return $"{publicKey}|{digest}";
    }

    private static string Base64Of(string value) => Convert.ToBase64String(Encoding.UTF8.GetBytes(value));

    private static string SampleXmlPayload() =>
        """
        <?xml version="1.0" encoding="UTF-8"?>
        <notification>
          <kind>subscription_charged_successfully</kind>
          <timestamp type="datetime">2026-10-31T09:30:00Z</timestamp>
          <subject>
            <subscription>
              <id>sub_7j8k9l</id>
              <status>active</status>
              <price>29.00</price>
            </subscription>
            <transaction>
              <id>txn_5f6g7h</id>
              <status>submitted_for_settlement</status>
              <amount>29.00</amount>
              <currency-iso-code>USD</currency-iso-code>
            </transaction>
          </subject>
        </notification>
        """;
}
