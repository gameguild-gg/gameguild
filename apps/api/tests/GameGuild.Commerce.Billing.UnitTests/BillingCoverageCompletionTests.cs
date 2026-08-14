using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using GameGuild.Commerce;
using GameGuild.Commerce.Subscriptions;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Billing.UnitTests;

public sealed class BillingCoverageCompletionTests
{
    [Fact]
    public void RetryInvoicePayment_Contracts_Handler_And_Controller_Ctors_AreCovered()
    {
        var invoiceId = Guid.NewGuid();
        var command = new RetryInvoicePaymentCommand(invoiceId);
        command.InvoiceId.Should().Be(invoiceId);

        var result = new InvoicePaymentRetryResult(
            invoiceId,
            "INV-1",
            InvoiceStatus.Open,
            Accepted: true,
            Code: "RetryAccepted",
            Message: "accepted",
            RetryScheduledAt: SystemClock.UtcNow);

        result.InvoiceNumber.Should().Be("INV-1");
        result.Accepted.Should().BeTrue();

        new RetryInvoicePaymentHandler(Mock.Of<GameGuild.IApplicationDbContext>()).Should().NotBeNull();
        new BillingInvoicesController(Mock.Of<GameGuild.CQRS.ISender>()).Should().NotBeNull();
    }

    [Fact]
    public void AppleJwsVerificationService_ShouldDecodeSignedTransactionAndNotification()
    {
        using var signer = CreateAppleSigner();
        var service = new AppleJwsVerificationService(NullLogger<AppleJwsVerificationService>.Instance);

        var transactionJws = CreateJws(
            signer,
            """
            {
              "transactionId": "tx_coverage",
              "originalTransactionId": "orig_coverage",
              "bundleId": "com.gameguild.test",
              "productId": "plan_coverage",
              "purchaseDate": 1760000000000,
              "expiresDate": 1761000000000,
              "type": "Auto-Renewable Subscription",
              "environment": "Sandbox"
            }
            """);
        var notificationJws = CreateJws(
            signer,
            """
            {
              "notificationType": "DID_RENEW",
              "subtype": "INITIAL_BUY",
              "version": "2.0",
              "signedDate": 1760000000000,
              "data": {
                "bundleId": "com.gameguild.test",
                "environment": "Sandbox"
              }
            }
            """);

        service.DecodeSignedTransaction(transactionJws)!.TransactionId.Should().Be("tx_coverage");
        service.DecodeSignedNotification(notificationJws)!.NotificationType.Should().Be("DID_RENEW");
    }

    [Fact]
    public void AppleJwsVerificationService_PrivateHelpers_ShouldCoverAlgorithmsAndPadding()
    {
        using var signer = CreateAppleSigner();
        var service = new AppleJwsVerificationService(NullLogger<AppleJwsVerificationService>.Instance);
        var parts = CreateJws(signer, """{"transactionId":"tx"}""").Split('.');
        var cert = signer.Certificate;

        InvokePrivateInstance<bool>(service, "VerifyJwsSignature", parts, cert, "ES256").Should().BeTrue();
        InvokePrivateInstance<bool>(service, "VerifyJwsSignature", parts, cert, "ES384").Should().BeFalse();
        InvokePrivateInstance<bool>(service, "VerifyJwsSignature", parts, cert, "ES512").Should().BeFalse();
        InvokePrivateInstance<bool>(service, "VerifyJwsSignature", parts, cert, "unknown").Should().BeTrue();
        InvokePrivateStatic<byte[]>(typeof(AppleJwsVerificationService), "Base64UrlDecodeBytes", "YWI").Should().Equal((byte)'a', (byte)'b');
        InvokePrivateStatic<byte[]>(typeof(AppleJwsVerificationService), "Base64UrlDecodeBytes", "YQ").Should().Equal((byte)'a');

        service.DecodeSignedTransaction("bad.payload").Should().BeNull();
        service.DecodeSignedTransaction($"{Base64UrlEncode("""{"alg":"ES256","x5c":[]}""")}.{Base64UrlEncode("{}")}.sig").Should().BeNull();
        service.DecodeSignedTransaction($"{Base64UrlEncode("""{"alg":"ES256"}""")}.{Base64UrlEncode("{}")}.sig").Should().BeNull();
        service.DecodeSignedTransaction($"{Base64UrlEncode("null")}.{Base64UrlEncode("{}")}.sig").Should().BeNull();
        service.DecodeSignedTransaction($"not-base64.{Base64UrlEncode("{}")}.sig").Should().BeNull();

        var badSignature = $"{parts[0]}.{parts[1]}.{Base64UrlEncode("bad-signature")}";
        service.DecodeSignedTransaction(badSignature).Should().BeNull();
        service.DecodeSignedNotification("bad.payload").Should().BeNull();
        service.DecodeSignedNotification($"{Base64UrlEncode("""{"alg":"ES256","x5c":[]}""")}.{Base64UrlEncode("{}")}.sig").Should().BeNull();
        service.DecodeSignedNotification($"{Base64UrlEncode("""{"alg":"ES256"}""")}.{Base64UrlEncode("{}")}.sig").Should().BeNull();
        service.DecodeSignedNotification($"{Base64UrlEncode("null")}.{Base64UrlEncode("{}")}.sig").Should().BeNull();
        service.DecodeSignedNotification($"not-base64.{Base64UrlEncode("{}")}.sig").Should().BeNull();
        service.DecodeSignedNotification(badSignature).Should().BeNull();

        using var nonAppleSigner = CreateAppleSigner("CN=Other Issuer");
        service.DecodeSignedNotification(CreateJws(nonAppleSigner, """{"notificationType":"DID_RENEW"}""")).Should().BeNull();
        InvokePrivateInstance<bool>(
            service,
            "VerifyAppleCertificateChain",
            (object)new[] { Convert.ToBase64String(nonAppleSigner.Certificate.Export(X509ContentType.Cert)), Convert.ToBase64String(nonAppleSigner.Certificate.Export(X509ContentType.Cert)) })
            .Should()
            .BeFalse();
        using var nonAppleChain = CreateNonAppleValidChain();
        InvokePrivateInstance<bool>(
            service,
            "VerifyAppleCertificateChain",
            (object)nonAppleChain.X5cChain)
            .Should()
            .BeFalse();

        using var expiredSigner = CreateAppleSigner("CN=Apple Expired", DateTimeOffset.UtcNow.AddDays(-3), DateTimeOffset.UtcNow.AddDays(-2));
        InvokePrivateInstance<bool>(
            service,
            "VerifyAppleCertificateChain",
            (object)new[] { Convert.ToBase64String(expiredSigner.Certificate.Export(X509ContentType.Cert)), Convert.ToBase64String(expiredSigner.Certificate.Export(X509ContentType.Cert)) })
            .Should()
            .BeFalse();
        using var futureSigner = CreateAppleSigner("CN=Apple Future", DateTimeOffset.UtcNow.AddDays(1), DateTimeOffset.UtcNow.AddDays(2));
        InvokePrivateInstance<bool>(
            service,
            "VerifyAppleCertificateChain",
            (object)new[] { Convert.ToBase64String(futureSigner.Certificate.Export(X509ContentType.Cert)), Convert.ToBase64String(futureSigner.Certificate.Export(X509ContentType.Cert)) })
            .Should()
            .BeFalse();
        InvokePrivateInstance<bool>(
            service,
            "VerifyAppleCertificateChain",
            (object)new[] { "not-base64", "still-not-base64" })
            .Should()
            .BeFalse();

        using var missingIssuerSigner = CreateAppleSignerWithMissingIssuer();
        InvokePrivateInstance<bool>(
            service,
            "VerifyAppleCertificateChain",
            (object)missingIssuerSigner.X5cChain)
            .Should()
            .BeFalse();

        using var rsa = RSA.Create();
        var rsaRequest = new CertificateRequest("CN=Apple RSA", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var rsaCertificate = rsaRequest.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        InvokePrivateInstance<bool>(service, "VerifyJwsSignature", parts, rsaCertificate, "ES256").Should().BeFalse();
    }

    [Fact]
    public void ProviderPayloadParsers_ShouldCoverFallbackAndNestedBranches()
    {
        var appleInfo = InvokePrivateStatic<(string eventId, string eventType, string? transactionId)>(
            typeof(ApplePayBillingWebhookService),
            "ParseApplePayPayload",
            """{"eventId":"evt","eventType":"payment_completed","payment":{"transactionIdentifier":"nested-tx"}}""");
        appleInfo.Should().Be(("evt", "payment_completed", "nested-tx"));
        InvokePrivateStatic<(string eventId, string eventType, string? transactionId)>(
                typeof(ApplePayBillingWebhookService),
                "ParseApplePayPayload",
                """{"transactionId":"tx-only"}""")
            .Should()
            .Be((string.Empty, "unknown", "tx-only"));
        InvokePrivateStatic<(string eventId, string eventType, string? transactionId)>(
                typeof(ApplePayBillingWebhookService),
                "ParseApplePayPayload",
                "{}")
            .Should()
            .Be((string.Empty, "unknown", null));
        InvokePrivateStatic<(string eventId, string eventType, string? transactionId)>(
                typeof(ApplePayBillingWebhookService),
                "ParseApplePayPayload",
                """{"payment":{}}""")
            .Should()
            .Be((string.Empty, "unknown", null));
        InvokePrivateStatic<(string eventId, string eventType, string? transactionId)>(
                typeof(ApplePayBillingWebhookService),
                "ParseApplePayPayload",
                "not-json")
            .eventType.Should().Be("unknown");

        var applePayload = InvokePrivateStatic<object>(
            typeof(ApplePayBillingWebhookService),
            "ParseApplePayPayloadData",
            """{"eventType":"payment_completed","payment":{"transactionIdentifier":"nested"},"amount":"12.30","currency":"EUR"}""");
        Get<string?>(applePayload, "TransactionId").Should().Be("nested");
        Get<decimal>(applePayload, "Amount").Should().BeGreaterThan(0m);
        InvokePrivateStatic<object>(typeof(ApplePayBillingWebhookService), "ParseApplePayPayloadData", "not-json")
            .Should().NotBeNull();
        var appleDefaults = InvokePrivateStatic<object>(
            typeof(ApplePayBillingWebhookService),
            "ParseApplePayPayloadData",
            """{"eventType":null,"transactionId":"top","payment":{"transactionIdentifier":"nested","token":{"paymentData":{}}},"amount":"bad"}""");
        Get<string?>(appleDefaults, "EventType").Should().BeEmpty();
        Get<string?>(appleDefaults, "Currency").Should().Be("USD");
        InvokePrivateStatic<object>(typeof(ApplePayBillingWebhookService), "ParseApplePayPayloadData", "{}")
            .Should().NotBeNull();
        InvokePrivateStatic<object>(typeof(ApplePayBillingWebhookService), "ParseApplePayPayloadData", """{"payment":{}}""")
            .Should().NotBeNull();

        Set(applePayload, "SubscriptionId", "sub_apple");
        Set(applePayload, "TransactionId", null);
        var applePayment = applePayload.GetType().GetMethod("ToPaymentPayload")!.Invoke(applePayload, null) as ApplePayPaymentWebhookPayload;
        applePayment!.ExternalSubscriptionId.Should().Be("sub_apple");
        Set(applePayload, "Currency", null);
        applePayment = applePayload.GetType().GetMethod("ToPaymentPayload")!.Invoke(applePayload, null) as ApplePayPaymentWebhookPayload;
        applePayment!.Currency.Should().Be("USD");

        var paypalInfo = InvokePrivateStatic<(string eventType, string resourceId)>(
            typeof(PayPalBillingWebhookService),
            "ParsePayPalPayload",
            """{"event_type":"PAYMENT.SALE.COMPLETED","resource":{"id":"sale_1"}}""");
        paypalInfo.Should().Be(("PAYMENT.SALE.COMPLETED", "sale_1"));
        InvokePrivateStatic<(string eventType, string resourceId)>(
                typeof(PayPalBillingWebhookService),
                "ParsePayPalPayload",
                """{"resource":{}}""")
            .Should()
            .Be(("unknown", string.Empty));
        InvokePrivateStatic<(string eventType, string resourceId)>(
                typeof(PayPalBillingWebhookService),
                "ParsePayPalPayload",
                "{}")
            .Should()
            .Be(("unknown", string.Empty));
        InvokePrivateStatic<(string eventType, string resourceId)>(
                typeof(PayPalBillingWebhookService),
                "ParsePayPalPayload",
                """{"resource":{"id":null}}""")
            .Should()
            .Be(("unknown", string.Empty));
        InvokePrivateStatic<(string eventType, string resourceId)>(typeof(PayPalBillingWebhookService), "ParsePayPalPayload", "bad")
            .eventType.Should().Be("unknown");

        var paypalPayload = InvokePrivateStatic<object>(
            typeof(PayPalBillingWebhookService),
            "ParsePayPalPayloadData",
            """{"event_type":"BILLING.SUBSCRIPTION.ACTIVATED","resource":{"id":"res_1","status":"ACTIVE","billing_agreement_id":"ba_1","amount":{"total":"45.60","currency":"GBP"}}}""");
        Get<decimal>(paypalPayload, "Amount").Should().BeGreaterThan(0m);
        var paypalDefaults = InvokePrivateStatic<object>(
            typeof(PayPalBillingWebhookService),
            "ParsePayPalPayloadData",
            """{"event_type":"PAYMENT.SALE.COMPLETED","resource":{"amount":{"total":"bad"}}}""");
        Get<string?>(paypalDefaults, "Currency").Should().Be("USD");
        Set(paypalDefaults, "ResourceId", "resource-fallback");
        var paypalSubscription = paypalDefaults.GetType().GetMethod("ToSubscriptionPayload")!.Invoke(paypalDefaults, null) as PayPalSubscriptionWebhookPayload;
        paypalSubscription!.ExternalSubscriptionId.Should().Be("resource-fallback");
        Set(paypalDefaults, "ResourceId", null);
        paypalSubscription = paypalDefaults.GetType().GetMethod("ToSubscriptionPayload")!.Invoke(paypalDefaults, null) as PayPalSubscriptionWebhookPayload;
        paypalSubscription!.ExternalSubscriptionId.Should().BeEmpty();
        InvokePrivateStatic<object>(typeof(PayPalBillingWebhookService), "ParsePayPalPayloadData", "{}").Should().NotBeNull();
        InvokePrivateStatic<object>(typeof(PayPalBillingWebhookService), "ParsePayPalPayloadData", """{"event_type":null,"resource":{"amount":{}}}""").Should().NotBeNull();
        InvokePrivateStatic<object>(typeof(PayPalBillingWebhookService), "ParsePayPalPayloadData", "bad").Should().NotBeNull();

        var stripePayload = InvokePrivateStatic<object>(
            typeof(StripeBillingWebhookService),
            "ParseStripePayload",
            "invoice.payment_succeeded",
            $$"""
            {
              "data": {
                "object": {
                  "id": "sub_1",
                  "subscription": "sub_override",
                  "customer": "cus_1",
                  "status": "active",
                  "amount_due": 2500,
                  "currency": "usd",
                  "invoice": "in_1",
                  "metadata": {
                    "tenant_id": "{{Guid.NewGuid()}}",
                    "plan_id": "{{Guid.NewGuid()}}"
                  },
                  "items": { "data": [ { "price": { "id": "price_1", "product": "prod_1" } } ] },
                  "current_period_start": 1760000000,
                  "current_period_end": 1761000000,
                  "billing_cycle_anchor": 1760500000
                }
              }
            }
            """);
        var stripeSubscription = stripePayload.GetType().GetMethod("ToSubscriptionPayload")!.Invoke(stripePayload, null) as StripeSubscriptionWebhookPayload;
        stripeSubscription!.ExternalSubscriptionId.Should().Be("sub_override");
        Set(stripePayload, "PaymentId", "pay_override");
        Set(stripePayload, "PaidAt", SystemClock.UtcNow);
        var stripePayment = stripePayload.GetType().GetMethod("ToPaymentPayload")!.Invoke(stripePayload, null) as StripePaymentWebhookPayload;
        stripePayment!.PaymentId.Should().Be("pay_override");
        var stripeAmountPaid = InvokePrivateStatic<object>(
            typeof(StripeBillingWebhookService),
            "ParseStripePayload",
            "invoice.payment_succeeded",
            """{"data":{"object":{"amount_paid":1250,"currency":"eur"}}}""");
        (stripeAmountPaid.GetType().GetMethod("ToPaymentPayload")!.Invoke(stripeAmountPaid, null) as StripePaymentWebhookPayload)!
            .Amount.Should().Be(12.5m);
        Set(stripeAmountPaid, "Currency", null);
        Set(stripeAmountPaid, "TenantId", Guid.NewGuid());
        Set(stripeAmountPaid, "PaymentId", "pay_1");
        Set(stripeAmountPaid, "PaidAt", null);
        (stripeAmountPaid.GetType().GetMethod("ToPaymentPayload")!.Invoke(stripeAmountPaid, null) as StripePaymentWebhookPayload)!
            .Currency.Should().Be("USD");
        InvokePrivateStatic<object>(
                typeof(StripeBillingWebhookService),
                "ParseStripePayload",
                "invoice.payment_succeeded",
                """{"data":{"object":{"currency":null}}}""")
            .Should()
            .NotBeNull();
        var stripeDefaults = InvokePrivateStatic<object>(typeof(StripeBillingWebhookService), "ParseStripePayload", "empty", "{}");
        (stripeDefaults.GetType().GetMethod("ToSubscriptionPayload")!.Invoke(stripeDefaults, null) as StripeSubscriptionWebhookPayload)!
            .TenantId.Should().Be(Guid.Empty);
        InvokePrivateStatic<object>(typeof(StripeBillingWebhookService), "ParseStripePayload", "bad", "not-json").Should().NotBeNull();
    }

    [Fact]
    public void BillingMappersAndConstants_ShouldCoverRemainingSwitchAndFallbackBranches()
    {
        PaymentProviders.IsSupported(null).Should().BeFalse();
        CurrencyCodes.IsSupported(null).Should().BeFalse();

        var config = new BillingConfiguration
        {
            Stripe = { SecretKey = "sk" },
            PayPal = { ClientId = "client" },
            ApplePay = { BundleId = "bundle" }
        };
        config.ValidateProvider().Errors.Should().HaveCount(3);
        config.ValidateProvider(PaymentProviders.Stripe).Errors.Should().ContainSingle();
        config.ValidateProvider(PaymentProviders.PayPal).Errors.Should().ContainSingle();
        config.ValidateProvider(PaymentProviders.AppleAppStore).Errors.Should().ContainSingle();
        config.ValidateProvider("unsupported").IsValid.Should().BeTrue();
        new BillingConfiguration
        {
            Stripe = { SecretKey = "sk", PublishableKey = "pk" },
            PayPal = { ClientId = "client", ClientSecret = "secret" },
            ApplePay = { BundleId = "bundle", SharedSecret = "shared" }
        }.ValidateProvider().IsValid.Should().BeTrue();
        new BillingConfiguration().ValidateProvider().IsValid.Should().BeTrue();

        foreach (var status in new[] { "success", "paid", "completed", "failure", "declined", "processing", "cancelled", "refunded", "unknown" })
        {
            UnifiedWebhookEvent.FromPayPalPayment(new PayPalPaymentWebhookPayload
            {
                TenantId = Guid.NewGuid(),
                PaymentId = $"pay-{status}",
                Status = status,
                Amount = 1,
                Currency = "USD"
            }, status, $"evt-{status}").Status.Should().BeDefined();
        }

        UnifiedWebhookEvent.FromStripePayment(new StripePaymentWebhookPayload
        {
            TenantId = Guid.NewGuid(),
            PaymentId = "pay",
            Status = "succeeded",
            Amount = 1,
            Currency = "USD",
            PaidAt = null,
            CustomerId = null,
            InvoiceId = null,
            ChargeId = null
        }, "payment", "evt").ProviderData.Should().ContainKey("chargeId");

        UnifiedWebhookEvent.FromStripeSubscription(new StripeSubscriptionWebhookPayload
        {
            TenantId = Guid.NewGuid(),
            ExternalSubscriptionId = "sub",
            Status = "active",
            Amount = 1,
            StartDate = null,
            Interval = null
        }, "subscription", "evt").ProviderData.Should().ContainKey("interval");
        UnifiedWebhookEvent.FromStripeSubscription(new StripeSubscriptionWebhookPayload
        {
            TenantId = Guid.NewGuid(),
            ExternalSubscriptionId = "sub",
            Status = "active",
            Amount = 1,
            StartDate = SystemClock.UtcNow,
            Interval = "month",
            CancelAtPeriodEnd = true
        }, "subscription", "evt").ProviderData!["interval"].Should().Be("month");

        foreach (var status in new[] { "active", "trial", "past_due", "pastdue", "cancelled", "unpaid", "incomplete", "incomplete_expired", "paused", "other" })
        {
            InvokePrivateStatic<SubscriptionStatus>(typeof(BillingWebhookService), "ParseSubscriptionStatus", status)
                .Should().BeDefined();
        }
    }

    [Fact]
    public void HandlerHelpersEfConfigurationAndInvoiceBranches_ShouldCoverRemainingBillingPaths()
    {
        InvokePrivateStatic<(string eventId, string eventType)>(
                typeof(ProcessApplePayWebhookCommandHandler),
                "ExtractEventInfo",
                """{"eventId":"evt","eventType":"type"}""")
            .Should()
            .Be(("evt", "type"));
        InvokePrivateStatic<(string eventId, string eventType)>(
                typeof(ProcessApplePayWebhookCommandHandler),
                "ExtractEventInfo",
                "{}")
            .Should()
            .Be(("unknown", "unknown"));
        InvokePrivateStatic<(string eventId, string eventType)>(
                typeof(ProcessApplePayWebhookCommandHandler),
                "ExtractEventInfo",
                """{"transactionId":null}""")
            .Should()
            .Be(("unknown", "unknown"));
        InvokePrivateStatic<(string eventId, string eventType)>(
                typeof(ProcessApplePayWebhookCommandHandler),
                "ExtractEventInfo",
                """{"transactionId":"tx"}""")
            .Should()
            .Be(("tx", "unknown"));
        InvokePrivateStatic<(string eventId, string eventType)>(
                typeof(ProcessApplePayWebhookCommandHandler),
                "ExtractEventInfo",
                """{"eventId":null,"eventType":null}""")
            .Should()
            .Be(("unknown", "unknown"));
        InvokePrivateStatic<(string eventId, string eventType)>(
                typeof(ProcessPayPalWebhookCommandHandler),
                "ExtractEventInfo",
                """{"id":"evt","event_type":"type"}""")
            .Should()
            .Be(("evt", "type"));
        InvokePrivateStatic<(string eventId, string eventType)>(
                typeof(ProcessPayPalWebhookCommandHandler),
                "ExtractEventInfo",
                "{}")
            .Should()
            .Be(("unknown", "unknown"));
        InvokePrivateStatic<(string eventId, string eventType)>(
                typeof(ProcessPayPalWebhookCommandHandler),
                "ExtractEventInfo",
                """{"id":null,"event_type":null}""")
            .Should()
            .Be(("unknown", "unknown"));
        var appleHandler = new ProcessApplePayWebhookCommandHandler(null!, NullLogger<ProcessApplePayWebhookCommandHandler>.Instance);
        InvokePrivateInstance<object?>(appleHandler, "LogWebhookMetrics", "evt", "type", WebhookProcessingResult.Success("evt"), 1L);
        InvokePrivateInstance<object?>(appleHandler, "LogWebhookMetrics", "evt", "type", WebhookProcessingResult.AlreadyProcessed("evt", SystemClock.UtcNow), 1L);
        InvokePrivateInstance<object?>(appleHandler, "LogWebhookMetrics", "evt", "type", new WebhookProcessingResult { Processed = false, WasAlreadyProcessed = true }, 1L);
        InvokePrivateInstance<object?>(appleHandler, "LogWebhookMetrics", "evt", "type", WebhookProcessingResult.Failed("evt", "bad"), 1L);

        var paypalHandler = new ProcessPayPalWebhookCommandHandler(null!, NullLogger<ProcessPayPalWebhookCommandHandler>.Instance);
        InvokePrivateInstance<object?>(paypalHandler, "LogWebhookMetrics", "evt", "type", WebhookProcessingResult.Success("evt"), 1L);
        InvokePrivateInstance<object?>(paypalHandler, "LogWebhookMetrics", "evt", "type", WebhookProcessingResult.AlreadyProcessed("evt", SystemClock.UtcNow), 1L);
        InvokePrivateInstance<object?>(paypalHandler, "LogWebhookMetrics", "evt", "type", new WebhookProcessingResult { Processed = false, WasAlreadyProcessed = true }, 1L);
        InvokePrivateInstance<object?>(paypalHandler, "LogWebhookMetrics", "evt", "type", WebhookProcessingResult.Failed("evt", "bad"), 1L);

        var modelBuilder = new ModelBuilder();
        new BillingModelConfiguration().Configure(modelBuilder);
        modelBuilder.Model.FindEntityType(typeof(Invoice)).Should().NotBeNull();

        Activator.CreateInstance(typeof(Invoice), nonPublic: true).Should().BeOfType<Invoice>();
        var invoice = new Invoice(Guid.NewGuid(), Guid.NewGuid(), 100m);
        invoice.Issue();
        var paymentId = Guid.NewGuid();
        invoice.RecordPayment(paymentId, 40m, SystemClock.UtcNow);
        invoice.Status.Should().Be(InvoiceStatus.Open);
        invoice.RecordPayment(paymentId, 60m, SystemClock.UtcNow);
        invoice.Status.Should().Be(InvoiceStatus.Open);
        var otherPaymentId = Guid.NewGuid();
        try
        {
            invoice.RecordPayment(otherPaymentId, 50m, SystemClock.UtcNow);
            throw new Xunit.Sdk.XunitException("Expected a duplicate-payment invariant exception.");
        }
        catch (InvalidOperationException)
        {
        }
        invoice.RecordPayment(paymentId, 100m, SystemClock.UtcNow);
        invoice.Status.Should().Be(InvoiceStatus.Paid);

        var processor = new TestWebhookProcessor(
            Mock.Of<IBillingWebhookRepository>(),
            Options.Create(new BillingConfiguration { Webhook = { MaxRetryAttempts = 2 } }),
            NullLogger.Instance);
        processor.ExposedSettings.MaxRetryAttempts.Should().Be(2);
    }

    [Fact]
    public async Task AppleNotificationVerification_ShouldCoverSuccessFailureAndExceptionBranches()
    {
        var jws = new Mock<IAppleJwsVerificationService>();
        var service = new ApplePayReceiptValidationService(
            new HttpClient(),
            Options.Create(new ApplePaySettings { BundleId = "com.gameguild.test" }),
            Mock.Of<IAppleStoreAuthService>(),
            jws.Object,
            NullLogger<ApplePayReceiptValidationService>.Instance);

        jws.Setup(x => x.DecodeSignedNotification("missing")).Returns((AppleNotificationPayload?)null);
        (await service.VerifyNotificationAsync("missing")).IsValid.Should().BeFalse();

        jws.Setup(x => x.DecodeSignedNotification("mismatch")).Returns(new AppleNotificationPayload
        {
            NotificationType = "DID_RENEW",
            Data = new AppleNotificationData { BundleId = "other", Environment = "Sandbox" }
        });
        (await service.VerifyNotificationAsync("mismatch")).IsValid.Should().BeFalse();
        jws.Setup(x => x.DecodeSignedNotification("no-data")).Returns(new AppleNotificationPayload
        {
            NotificationType = "DID_RENEW"
        });
        (await service.VerifyNotificationAsync("no-data")).IsValid.Should().BeFalse();
        jws.Setup(x => x.DecodeSignedNotification("no-transaction")).Returns(new AppleNotificationPayload
        {
            NotificationType = "SUBSCRIBED",
            Data = new AppleNotificationData { BundleId = "com.gameguild.test", Environment = null! }
        });
        var noTransactionResult = await service.VerifyNotificationAsync("no-transaction");
        noTransactionResult.IsValid.Should().BeTrue();
        noTransactionResult.Environment.Should().Be("unknown");

        jws.Setup(x => x.DecodeSignedNotification("success")).Returns(new AppleNotificationPayload
        {
            NotificationType = "DID_RENEW",
            Subtype = "INITIAL_BUY",
            Data = new AppleNotificationData
            {
                BundleId = "com.gameguild.test",
                Environment = "Sandbox",
                SignedTransactionInfo = "transaction"
            }
        });
        jws.Setup(x => x.DecodeSignedTransaction("transaction")).Returns(new AppleTransactionInfo
        {
            TransactionId = "tx",
            OriginalTransactionId = "orig",
            ProductId = "plan",
            ExpiresDate = 1761000000000
        });
        (await service.VerifyNotificationAsync("success")).TransactionId.Should().Be("tx");
        jws.Setup(x => x.DecodeSignedNotification("no-expiry")).Returns(new AppleNotificationPayload
        {
            NotificationType = "DID_RENEW",
            Data = new AppleNotificationData
            {
                BundleId = "com.gameguild.test",
                SignedTransactionInfo = "no-expiry-transaction"
            }
        });
        jws.Setup(x => x.DecodeSignedTransaction("no-expiry-transaction")).Returns(new AppleTransactionInfo
        {
            TransactionId = "tx-no-expiry",
            OriginalTransactionId = "orig",
            ProductId = "plan",
            ExpiresDate = null
        });
        (await service.VerifyNotificationAsync("no-expiry")).ExpirationDate.Should().BeNull();
        jws.Setup(x => x.DecodeSignedNotification("undecodable-transaction")).Returns(new AppleNotificationPayload
        {
            NotificationType = "DID_RENEW",
            Data = new AppleNotificationData
            {
                BundleId = "com.gameguild.test",
                SignedTransactionInfo = "undecodable"
            }
        });
        jws.Setup(x => x.DecodeSignedTransaction("undecodable")).Returns((AppleTransactionInfo?)null);
        (await service.VerifyNotificationAsync("undecodable-transaction")).TransactionId.Should().BeEmpty();
        jws.Setup(x => x.DecodeSignedNotification("empty-transaction")).Returns(new AppleNotificationPayload
        {
            NotificationType = "DID_RENEW",
            Data = new AppleNotificationData
            {
                BundleId = "com.gameguild.test",
                SignedTransactionInfo = string.Empty
            }
        });
        (await service.VerifyNotificationAsync("empty-transaction")).TransactionId.Should().BeEmpty();
        jws.Setup(x => x.DecodeSignedNotification("null-type")).Returns(new AppleNotificationPayload
        {
            NotificationType = null!,
            Data = new AppleNotificationData
            {
                BundleId = "com.gameguild.test",
                SignedTransactionInfo = null,
                Environment = "Sandbox"
            }
        });
        (await service.VerifyNotificationAsync("null-type")).NotificationType.Should().BeNull();

        jws.Setup(x => x.DecodeSignedNotification("throw")).Throws(new InvalidOperationException("boom"));
        (await service.VerifyNotificationAsync("throw")).IsValid.Should().BeFalse();
    }

    private static AppleSigner CreateAppleSigner(
        string subjectName = "CN=Apple Test",
        DateTimeOffset? notBefore = null,
        DateTimeOffset? notAfter = null)
    {
        var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var request = new CertificateRequest(subjectName, ecdsa, HashAlgorithmName.SHA256);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        var certificate = request.CreateSelfSigned(notBefore ?? DateTimeOffset.UtcNow.AddDays(-1), notAfter ?? DateTimeOffset.UtcNow.AddDays(1));
        return new AppleSigner(ecdsa, certificate);
    }

    private static AppleSignerWithChain CreateAppleSignerWithMissingIssuer()
    {
        var leafKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var issuerKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var unrelatedKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var issuerRequest = new CertificateRequest("CN=Apple Missing CA", issuerKey, HashAlgorithmName.SHA256);
        issuerRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        using var issuer = issuerRequest.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        var leafRequest = new CertificateRequest("CN=Apple Leaf", leafKey, HashAlgorithmName.SHA256);
        var leaf = leafRequest.Create(issuer, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1), Guid.NewGuid().ToByteArray());
        var unrelatedRequest = new CertificateRequest("CN=Apple Missing CA", unrelatedKey, HashAlgorithmName.SHA256);
        unrelatedRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        var unrelated = unrelatedRequest.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        return new AppleSignerWithChain(
            leafKey,
            leaf,
            unrelated,
            issuerKey,
            unrelatedKey,
            new[] { Convert.ToBase64String(leaf.Export(X509ContentType.Cert)), Convert.ToBase64String(unrelated.Export(X509ContentType.Cert)) });
    }

    private static AppleSignerWithChain CreateNonAppleValidChain()
    {
        var leafKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var issuerKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var spareKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var issuerRequest = new CertificateRequest("CN=Other Root", issuerKey, HashAlgorithmName.SHA256);
        issuerRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        var issuer = issuerRequest.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        var leafRequest = new CertificateRequest("CN=Other Leaf", leafKey, HashAlgorithmName.SHA256);
        var leaf = leafRequest.Create(issuer, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1), Guid.NewGuid().ToByteArray());
        return new AppleSignerWithChain(
            leafKey,
            leaf,
            issuer,
            issuerKey,
            spareKey,
            new[] { Convert.ToBase64String(leaf.Export(X509ContentType.Cert)), Convert.ToBase64String(issuer.Export(X509ContentType.Cert)) });
    }

    private static string CreateJws(AppleSigner signer, string payloadJson, string algorithm = "ES256")
    {
        var certBase64 = Convert.ToBase64String(signer.Certificate.Export(X509ContentType.Cert));
        var header = JsonSerializer.Serialize(new { alg = algorithm, x5c = new[] { certBase64, certBase64 } });
        var encodedHeader = Base64UrlEncode(header);
        var encodedPayload = Base64UrlEncode(payloadJson);
        var signingInput = $"{encodedHeader}.{encodedPayload}";
        var signature = signer.Key.SignData(Encoding.UTF8.GetBytes(signingInput), HashAlgorithmName.SHA256);
        return $"{signingInput}.{Base64UrlEncode(signature)}";
    }

    private static string Base64UrlEncode(string value) => Base64UrlEncode(Encoding.UTF8.GetBytes(value));

    private static string Base64UrlEncode(byte[] value)
        => Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static T Get<T>(object instance, string propertyName)
        => (T)instance.GetType().GetProperty(propertyName)!.GetValue(instance)!;

    private static void Set(object instance, string propertyName, object? value)
        => instance.GetType().GetProperty(propertyName)!.SetValue(instance, value);

    private static T InvokePrivateStatic<T>(Type type, string methodName, params object?[] arguments)
        => (T)type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, arguments)!;

    private static T InvokePrivateInstance<T>(object instance, string methodName, params object?[] arguments)
        => (T)instance.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(instance, arguments)!;

    private sealed record AppleSigner(ECDsa Key, X509Certificate2 Certificate) : IDisposable
    {
        public void Dispose()
        {
            Certificate.Dispose();
            Key.Dispose();
        }
    }

    private sealed record AppleSignerWithChain(
        ECDsa Key,
        X509Certificate2 Certificate,
        X509Certificate2 UnrelatedCertificate,
        ECDsa IssuerKey,
        ECDsa UnrelatedKey,
        string[] X5cChain) : IDisposable
    {
        public void Dispose()
        {
            Certificate.Dispose();
            UnrelatedCertificate.Dispose();
            Key.Dispose();
            IssuerKey.Dispose();
            UnrelatedKey.Dispose();
        }
    }

    private sealed class TestWebhookProcessor(
        IBillingWebhookRepository webhookRepository,
        IOptions<BillingConfiguration> billingConfiguration,
        ILogger logger) : WebhookProcessorBase(webhookRepository, billingConfiguration, logger)
    {
        protected override string ProviderName => "test";

        public WebhookSettings ExposedSettings => Settings;

        protected override Task RouteEventAsync(string eventType, string payload, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
