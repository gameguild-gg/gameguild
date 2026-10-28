using System.IO.Compression;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;
using FluentAssertions;
using GameGuild.Configuration.PresentationLayer.Authentication;
using GameGuild.Identity.Authentication.UnitTests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

/// <summary>
///     SamlFederationService tests with real XML-DSig signatures: an in-test identity
///     provider signs SAML responses with self-signed X.509 certificates and the service
///     must verify (or refuse) them exactly as a production IdP exchange would —
///     signature/expiry/audience/destination/lifetime/correlation/replay failure modes
///     plus the fail-closed provider registry, SP metadata, and logout forwarding.
/// </summary>
public sealed class SamlFederationServiceTests : IDisposable
{
    private const string Slug = "corp-idp";
    private const string SpEntityId = "https://gameguild.example.test/auth/saml";
    private const string AcsUrl = "https://gameguild.example.test/auth/saml/corp-idp/callback";
    private const string IdpEntityId = "https://sts.corp.example.test";
    private const string IdpSignInUrl = "https://sts.corp.example.test/sso";

    private readonly RSA _idpKey = RSA.Create(2048);
    private readonly RSA _rotationKey = RSA.Create(2048);
    private readonly RSA _foreignKey = RSA.Create(2048);

    public void Dispose()
    {
        _idpKey.Dispose();
        _rotationKey.Dispose();
        _foreignKey.Dispose();
    }

    private readonly InMemorySamlProtocolStateStore _protocolState = new();

    /// <summary>RelayState issued by the most recent <see cref="IssuePendingRequestIdAsync" /> call.</summary>
    private string _lastIssuedRelayState = string.Empty;

    private SamlFederationService CreateSut(AuthenticationOptions options) =>
        new(
            _protocolState,
            options,
            NullLogger<SamlFederationService>.Instance);

    /// <summary>
    ///     Consumes with the RelayState issued by the latest initiation (the value a
    ///     well-behaved browser echoes back); relay-mismatch tests pass an explicit value.
    /// </summary>
    private Task<SamlFederatedIdentity> Consume(SamlFederationService sut, string samlResponse, string? relayState = null) =>
        sut.ConsumeAssertionAsync(Slug, samlResponse, relayState ?? _lastIssuedRelayState);

    private string CertificateBase64(RSA key, DateTimeOffset notBefore, DateTimeOffset notAfter)
    {
        using var certificate = CreateCertificate(key, notBefore, notAfter);
        return Convert.ToBase64String(certificate.Export(X509ContentType.Cert));
    }

    private static X509Certificate2 CreateCertificate(RSA key, DateTimeOffset notBefore, DateTimeOffset notAfter)
    {
        var request = new CertificateRequest("CN=corp-idp-signing", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return request.CreateSelfSigned(notBefore, notAfter);
    }

    private SamlProviderOptions ProviderOptions(
        string[]? certificates = null,
        string[]? emailDomains = null,
        bool allowIdpInitiated = false,
        string? nameIdFormat = null,
        Dictionary<string, string>? claimMapping = null,
        string[]? mfaContexts = null,
        string? sloUrl = null) => new()
    {
        Enabled = true,
        SpEntityId = SpEntityId,
        AssertionConsumerServiceUrl = AcsUrl,
        IdpEntityId = IdpEntityId,
        IdpSigningCertificates = (certificates ?? [CertificateBase64(_idpKey, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1))]).ToList(),
        IdpSignInServiceUrl = IdpSignInUrl,
        IdpSingleLogoutServiceUrl = sloUrl,
        AllowIdpInitiated = allowIdpInitiated,
        NameIdFormat = nameIdFormat,
        ClaimMapping = claimMapping ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
        MfaAuthnContextClassRefs = (mfaContexts ?? SamlProviderOptions.DefaultMfaAuthnContextClassRefs.ToArray()).ToList(),
        EmailDomains = (emailDomains ?? []).ToList()
    };

    private AuthenticationOptions AuthOptionsWith(SamlProviderOptions options) => new()
    {
        EnableAuthentication = false,
        ExternalProviders = new ExternalProviderOptions
        {
            Saml = new Dictionary<string, SamlProviderOptions>(StringComparer.OrdinalIgnoreCase) { [Slug] = options }
        }
    };

    /// <summary>Issues a pending AuthnRequest through the service under test and returns its ID.</summary>
    private async Task<string> IssuePendingRequestIdAsync(SamlFederationService sut)
    {
        var challenge = await sut.BuildSignInRedirectAsync(Slug, null);
        _lastIssuedRelayState = challenge.RelayState;
        return challenge.RequestId;
    }

    // ── Sign-in redirect (AuthnRequest, HTTP-Redirect binding) ─────────────────

    [Fact]
    public async Task BuildSignInRedirectAsync_BuildsRedirectBindingUrlWithDeflatedAuthnRequest()
    {
        var sut = CreateSut(AuthOptionsWith(ProviderOptions()));

        var challenge = await sut.BuildSignInRedirectAsync(Slug, "relay-42");

        challenge.RelayState.Should().Be("relay-42");
        challenge.RequestId.Should().StartWith("_");
        challenge.RedirectUrl.Should().StartWith($"{IdpSignInUrl}?SAMLRequest=");
        challenge.RedirectUrl.Should().Contain($"RelayState={Uri.EscapeDataString("relay-42")}");

        var authnRequest = DeflateDecode(challenge.RedirectUrl);
        authnRequest.Should().Contain($"ID=\"{challenge.RequestId}\"");
        authnRequest.Should().Contain($"Destination=\"{IdpSignInUrl}\"");
        authnRequest.Should().Contain($"AssertionConsumerServiceURL=\"{AcsUrl}\"");
        authnRequest.Should().Contain($"<saml:Issuer>{SpEntityId}</saml:Issuer>");
    }

    [Fact]
    public async Task BuildSignInRedirectAsync_GeneratesRelayStateWhenMissing()
    {
        var sut = CreateSut(AuthOptionsWith(ProviderOptions()));

        var challenge = await sut.BuildSignInRedirectAsync(Slug, null);

        challenge.RelayState.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task BuildSignInRedirectAsync_UnknownSlug_FailsClosed()
    {
        var sut = CreateSut(AuthOptionsWith(ProviderOptions()));

        var act = () => sut.BuildSignInRedirectAsync("other-idp", null);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not configured*");
    }

    [Fact]
    public async Task BuildSignInRedirectAsync_DisabledSlug_FailsClosed()
    {
        var options = ProviderOptions();
        options.Enabled = false;
        var sut = CreateSut(AuthOptionsWith(options));

        var act = () => sut.BuildSignInRedirectAsync(Slug, null);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*disabled*");
    }

    // ── Callback: happy path + mapping ─────────────────────────────────────────

    [Fact]
    public async Task ConsumeAssertionAsync_ValidResponse_MapsIdentity()
    {
        var sut = CreateSut(AuthOptionsWith(ProviderOptions()));
        var requestId = await IssuePendingRequestIdAsync(sut);

        var identity = await Consume(sut, BuildResponse(o => o with { InResponseTo = requestId }));

        identity.Slug.Should().Be(Slug);
        identity.ProviderKey.Should().Be("jane@corp.example.test");
        identity.Email.Should().Be("jane@corp.example.test");
        identity.Name.Should().Be("Jane Corp");
        identity.EmailVerified.Should().BeFalse("TrustIdpEmailVerification defaults to false (fail closed)");
        identity.HasMfaProof.Should().BeFalse();
        identity.AuthnContextClassRefs.Should().Contain("urn:oasis:names:tc:SAML:2.0:ac:classes:PasswordProtectedTransport");
        identity.TenantAllowlist.Should().BeEmpty();
    }

    [Fact]
    public async Task ConsumeAssertionAsync_ResponseSignedOnly_AlsoAccepted()
    {
        var sut = CreateSut(AuthOptionsWith(ProviderOptions()));
        var requestId = await IssuePendingRequestIdAsync(sut);

        var identity = await Consume(sut, BuildResponse(o => o with { InResponseTo = requestId, SignAssertion = false, SignResponse = true }));

        identity.ProviderKey.Should().Be("jane@corp.example.test");
    }

    [Fact]
    public async Task ConsumeAssertionAsync_CustomClaimMapping_RemapsAttributes()
    {
        var mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["email"] = "mail",
            ["name"] = "displayName"
        };
        var sut = CreateSut(AuthOptionsWith(ProviderOptions(claimMapping: mapping)));
        var requestId = await IssuePendingRequestIdAsync(sut);

        // The IdP model emits the attribute names the mapping expects ("mail" and
        // "displayName"); the service must resolve them onto email/name.
        var identity = await Consume(sut, BuildResponse(o => o with
        {
            InResponseTo = requestId,
            EmailAttributeName = "mail",
            NameAttributeName = "displayName"
        }));

        identity.Email.Should().Be("jane@corp.example.test");
        identity.Name.Should().Be("Jane Corp");
    }

    [Fact]
    public async Task ConsumeAssertionAsync_TrustedProvider_ReportsVerifiedEmail()
    {
        var options = ProviderOptions();
        options.TrustIdpEmailVerification = true;
        var sut = CreateSut(AuthOptionsWith(options));
        var requestId = await IssuePendingRequestIdAsync(sut);

        var identity = await Consume(sut, BuildResponse(o => o with { InResponseTo = requestId }));

        identity.EmailVerified.Should().BeTrue();
    }

    // ── Signature failure modes ────────────────────────────────────────────────

    [Fact]
    public async Task ConsumeAssertionAsync_UnsignedResponse_Rejected()
    {
        var sut = CreateSut(AuthOptionsWith(ProviderOptions()));
        var requestId = await IssuePendingRequestIdAsync(sut);

        var act = () => Consume(sut, BuildResponse(o => o with { InResponseTo = requestId, SignAssertion = false }));

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*unsigned*");
    }

    [Fact]
    public async Task ConsumeAssertionAsync_ForeignKeySignature_Rejected()
    {
        var sut = CreateSut(AuthOptionsWith(ProviderOptions()));
        var requestId = await IssuePendingRequestIdAsync(sut);

        var act = () => Consume(sut, BuildResponse(o => o with { InResponseTo = requestId, SigningKey = _foreignKey }));

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*failed verification*");
    }

    [Fact]
    public async Task ConsumeAssertionAsync_ExpiredCertificate_Rejected()
    {
        // The response is signed by a certificate that is genuinely expired: the signature
        // would verify cryptographically, but the trust base is out of validity window.
        var options = ProviderOptions(certificates: [CertificateBase64(_idpKey, DateTimeOffset.UtcNow.AddDays(-60), DateTimeOffset.UtcNow.AddDays(-1))]);
        var sut = CreateSut(AuthOptionsWith(options));
        var requestId = await IssuePendingRequestIdAsync(sut);

        var act = () => Consume(sut, BuildResponse(o => o with { InResponseTo = requestId }));

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*expired*");
    }

    [Fact]
    public async Task ConsumeAssertionAsync_CertificateRotation_NewCertificateSignsWhileOldListed()
    {
        var options = ProviderOptions(certificates:
        [
            CertificateBase64(_idpKey, DateTimeOffset.UtcNow.AddDays(-30), DateTimeOffset.UtcNow.AddDays(-2)), // rotated out (expired)
            CertificateBase64(_rotationKey, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1)) // active
        ]);
        var sut = CreateSut(AuthOptionsWith(options));
        var requestId = await IssuePendingRequestIdAsync(sut);

        var identity = await Consume(sut, BuildResponse(o => o with { InResponseTo = requestId, SigningKey = _rotationKey }));

        identity.ProviderKey.Should().Be("jane@corp.example.test");
    }

    [Fact]
    public async Task ConsumeAssertionAsync_WrappedReferenceUri_Rejected()
    {
        // Signature-wrapping vector: a valid response-level signature whose reference
        // points at the assertion ID instead of the response ID. The cryptographic
        // signature itself is valid — only the ID anchoring rejects it.
        var sut = CreateSut(AuthOptionsWith(ProviderOptions()));
        var requestId = await IssuePendingRequestIdAsync(sut);

        var samlResponse = BuildResponse(o => o with { InResponseTo = requestId, SignAssertion = false, SignResponse = true }, referenceUriOverride: "assertion");

        var act = () => Consume(sut, samlResponse);

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*failed verification*");
    }

    // ── Issuer / audience / destination ────────────────────────────────────────

    [Fact]
    public async Task ConsumeAssertionAsync_WrongIssuer_Rejected()
    {
        var sut = CreateSut(AuthOptionsWith(ProviderOptions()));
        var requestId = await IssuePendingRequestIdAsync(sut);

        var act = () => Consume(sut, BuildResponse(o => o with { InResponseTo = requestId, Issuer = "https://evil.example.test" }));

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*issuer*");
    }

    [Fact]
    public async Task ConsumeAssertionAsync_WrongAudience_Rejected()
    {
        var sut = CreateSut(AuthOptionsWith(ProviderOptions()));
        var requestId = await IssuePendingRequestIdAsync(sut);

        var act = () => Consume(sut, BuildResponse(o => o with { InResponseTo = requestId, Audience = "https://other-sp.example.test" }));

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*audience*");
    }

    [Fact]
    public async Task ConsumeAssertionAsync_MissingAudienceRestriction_Rejected()
    {
        var sut = CreateSut(AuthOptionsWith(ProviderOptions()));
        var requestId = await IssuePendingRequestIdAsync(sut);

        var act = () => Consume(sut, BuildResponse(o => o with { InResponseTo = requestId, OmitAudienceRestriction = true }));

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*audience*");
    }

    [Theory]
    [InlineData("https://evil.example.test/acs")]
    [InlineData("")]
    public async Task ConsumeAssertionAsync_WrongOrMissingDestination_Rejected(string destination)
    {
        var sut = CreateSut(AuthOptionsWith(ProviderOptions()));
        var requestId = await IssuePendingRequestIdAsync(sut);

        var act = () => Consume(sut, BuildResponse(o => o with { InResponseTo = requestId, Destination = destination }));

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*Destination*");
    }

    // ── Temporal validity ──────────────────────────────────────────────────────

    [Fact]
    public async Task ConsumeAssertionAsync_ExpiredAssertion_Rejected()
    {
        var sut = CreateSut(AuthOptionsWith(ProviderOptions()));
        var requestId = await IssuePendingRequestIdAsync(sut);

        var act = () => Consume(sut, BuildResponse(o => o with
        {
            InResponseTo = requestId,
            ConditionsNotOnOrAfter = DateTimeOffset.UtcNow.AddMinutes(-5),
            SubjectNotOnOrAfter = DateTimeOffset.UtcNow.AddMinutes(-5)
        }));

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*expired*");
    }

    [Fact]
    public async Task ConsumeAssertionAsync_NotYetValidAssertion_Rejected()
    {
        var sut = CreateSut(AuthOptionsWith(ProviderOptions()));
        var requestId = await IssuePendingRequestIdAsync(sut);

        var act = () => Consume(sut, BuildResponse(o => o with { InResponseTo = requestId, ConditionsNotBefore = DateTimeOffset.UtcNow.AddHours(1) }));

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*not yet valid*");
    }

    [Fact]
    public async Task ConsumeAssertionAsync_MissingNotOnOrAfter_Rejected()
    {
        var sut = CreateSut(AuthOptionsWith(ProviderOptions()));
        var requestId = await IssuePendingRequestIdAsync(sut);

        var act = () => Consume(sut, BuildResponse(o => o with { InResponseTo = requestId, OmitConditionsNotOnOrAfter = true }));

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*NotOnOrAfter*");
    }

    [Fact]
    public async Task ConsumeAssertionAsync_WithinClockSkew_SmallSkewTolerated()
    {
        var options = ProviderOptions();
        options.AllowedClockSkewSeconds = 300;
        var sut = CreateSut(AuthOptionsWith(options));
        var requestId = await IssuePendingRequestIdAsync(sut);

        var identity = await Consume(sut, BuildResponse(o => o with
        {
            InResponseTo = requestId,
            ConditionsNotBefore = DateTimeOffset.UtcNow.AddSeconds(-1),
            ConditionsNotOnOrAfter = DateTimeOffset.UtcNow.AddSeconds(-120),
            SubjectNotOnOrAfter = DateTimeOffset.UtcNow.AddMinutes(5)
        }));

        // The conditions window expired 2 minutes ago but the 300-second skew still covers it.
        identity.ProviderKey.Should().Be("jane@corp.example.test");
    }

    // ── Correlation + replay ───────────────────────────────────────────────────

    [Fact]
    public async Task ConsumeAssertionAsync_UnknownInResponseTo_Rejected()
    {
        var sut = CreateSut(AuthOptionsWith(ProviderOptions()));
        await IssuePendingRequestIdAsync(sut);

        var act = () => Consume(sut, BuildResponse(o => o with { InResponseTo = "_never-issued" }));

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*no outstanding AuthnRequest*");
    }

    [Fact]
    public async Task ConsumeAssertionAsync_ReplayedResponse_Rejected()
    {
        var sut = CreateSut(AuthOptionsWith(ProviderOptions()));
        var requestId = await IssuePendingRequestIdAsync(sut);
        var samlResponse = BuildResponse(o => o with { InResponseTo = requestId });

        await Consume(sut, samlResponse);

        // Replay with a fresh pending request but the same assertion ID: still refused.
        var secondRequestId = await IssuePendingRequestIdAsync(sut);
        var act = () => Consume(sut, BuildResponse(o => o with { InResponseTo = secondRequestId, AssertionId = ExtractAssertionId(samlResponse) }));

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*replay*");
    }

    [Fact]
    public async Task ConsumeAssertionAsync_PendingRequestIsSingleUse()
    {
        var sut = CreateSut(AuthOptionsWith(ProviderOptions()));
        var requestId = await IssuePendingRequestIdAsync(sut);

        await Consume(sut, BuildResponse(o => o with { InResponseTo = requestId }));

        // Same InResponseTo again (with a fresh assertion) must fail: the request was consumed.
        var act = () => Consume(sut, BuildResponse(o => o with { InResponseTo = requestId }));

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*no outstanding AuthnRequest*");
    }

    [Fact]
    public async Task ConsumeAssertionAsync_ConcurrentReplayOfSameResponse_ExactlyOneSucceeds()
    {
        // Issue #268 acceptance, review item 3: two consumers racing with the very
        // same (genuinely signed) response — the atomic store must let exactly one
        // of them win; the loser is refused on single-use state, never duplicated.
        var sut = CreateSut(AuthOptionsWith(ProviderOptions()));
        var requestId = await IssuePendingRequestIdAsync(sut);
        var samlResponse = BuildResponse(o => o with { InResponseTo = requestId });
        var relayState = _lastIssuedRelayState;

        var attempts = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => TryConsume()));

        attempts.Count(success => success).Should().Be(1, "a signed SAML response is single-use however many consumers race");

        async Task<bool> TryConsume()
        {
            try
            {
                await sut.ConsumeAssertionAsync(Slug, samlResponse, relayState);
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }
    }

    [Fact]
    public async Task ConsumeAssertionAsync_IdpInitiated_DefaultRejected()
    {
        var sut = CreateSut(AuthOptionsWith(ProviderOptions()));

        var act = () => Consume(sut, BuildResponse(o => o with { InResponseTo = null }));

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*InResponseTo*");
    }

    [Fact]
    public async Task ConsumeAssertionAsync_IdpInitiated_AcceptedWhenExplicitlyAllowed()
    {
        var sut = CreateSut(AuthOptionsWith(ProviderOptions(allowIdpInitiated: true)));

        var identity = await Consume(sut, BuildResponse(o => o with { InResponseTo = null }));

        identity.ProviderKey.Should().Be("jane@corp.example.test");
    }

    // ── Signed subject correlation (Web SSO profile) ───────────────────────────

    [Fact]
    public async Task ConsumeAssertionAsync_SubjectCorrelationMismatch_Rejected()
    {
        // The outer Response says it answers this service's request, but the SIGNED
        // SubjectConfirmationData answers a different one — the authenticated
        // correlation disagrees with the unsigned outer attribute.
        var sut = CreateSut(AuthOptionsWith(ProviderOptions()));
        var requestId = await IssuePendingRequestIdAsync(sut);

        var act = () => Consume(sut, BuildResponse(o => o with
        {
            InResponseTo = requestId,
            SubjectInResponseTo = "_someone-elses-request"
        }));

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*does not match the response*");
    }

    [Fact]
    public async Task ConsumeAssertionAsync_SubjectCorrelationMissing_Rejected()
    {
        var sut = CreateSut(AuthOptionsWith(ProviderOptions()));
        var requestId = await IssuePendingRequestIdAsync(sut);

        var act = () => Consume(sut, BuildResponse(o => o with { InResponseTo = requestId, OmitSubjectInResponseTo = true }));

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*no subject-confirmation InResponseTo*");
    }

    [Fact]
    public async Task ConsumeAssertionAsync_SignedAssertionReplayedThroughUnsignedResponse_RejectedAndPendingPreserved()
    {
        // Signature-wrapping vector from the acceptance review: a genuinely-signed
        // assertion (minted for request A) is re-wrapped into a forged, unsigned outer
        // response whose InResponseTo points at the attacker's own fresh pending
        // request B. The response-level check alone would accept this; the signed
        // subject correlation refuses it — before B's pending state is consumed.
        var sut = CreateSut(AuthOptionsWith(ProviderOptions()));
        var requestIdA = await IssuePendingRequestIdAsync(sut);
        var relayA = _lastIssuedRelayState;
        var requestIdB = await IssuePendingRequestIdAsync(sut);
        var relayB = _lastIssuedRelayState;

        var act = () => Consume(sut, BuildResponse(o => o with
        {
            InResponseTo = requestIdB,
            SubjectInResponseTo = requestIdA,
            SignAssertion = true,
            SignResponse = false
        }), relayB);

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*does not match the response*");

        // Correlation is validated BEFORE pending state is consumed, so the attacker's
        // own pending request B is still intact and can still complete a legitimate flow.
        var identity = await Consume(sut, BuildResponse(o => o with { InResponseTo = requestIdB }), relayB);
        identity.ProviderKey.Should().Be("jane@corp.example.test");

        // Request A's pending state was never burned by the failed attack either: the
        // wrapped attempt never reached the store, so A can still complete legitimately.
        var identityA = await Consume(sut, BuildResponse(o => o with { InResponseTo = requestIdA }), relayA);
        identityA.ProviderKey.Should().Be("jane@corp.example.test");
    }

    [Fact]
    public async Task ConsumeAssertionAsync_IdpInitiated_WithSubjectCorrelation_Rejected()
    {
        // An unsolicited response must not carry any InResponseTo: an assertion minted
        // for some other request cannot be passed off as IdP-initiated.
        var sut = CreateSut(AuthOptionsWith(ProviderOptions(allowIdpInitiated: true)));

        var act = () => Consume(sut, BuildResponse(o => o with { InResponseTo = null, SubjectInResponseTo = "_some-request" }));

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*unsolicited but carries an InResponseTo*");
    }

    // ── RelayState callback binding ────────────────────────────────────────────

    [Fact]
    public async Task ConsumeAssertionAsync_ForeignRelayState_Rejected()
    {
        // State issued to another browser/provider round-trip: the RelayState posted
        // with the response does not match the one bound to the pending request.
        var sut = CreateSut(AuthOptionsWith(ProviderOptions()));
        var requestId = await IssuePendingRequestIdAsync(sut);

        var act = () => Consume(sut, BuildResponse(o => o with { InResponseTo = requestId }), "relay-from-another-browser");

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*no outstanding AuthnRequest*");
    }

    [Fact]
    public async Task ConsumeAssertionAsync_MissingRelayState_Rejected()
    {
        var sut = CreateSut(AuthOptionsWith(ProviderOptions()));
        var requestId = await IssuePendingRequestIdAsync(sut);

        var act = () => Consume(sut, BuildResponse(o => o with { InResponseTo = requestId }), string.Empty);

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*lacks the RelayState*");
    }

    [Fact]
    public async Task ConsumeAssertionAsync_WrongRelayStateDoesNotBurnPendingRequest()
    {
        // Like the production conditional DELETE, a failed RelayState match must not
        // consume the pending request — the legitimate browser can still complete.
        var sut = CreateSut(AuthOptionsWith(ProviderOptions()));
        var requestId = await IssuePendingRequestIdAsync(sut);
        var relayState = _lastIssuedRelayState;

        var wrongRelay = () => Consume(sut, BuildResponse(o => o with { InResponseTo = requestId, AssertionId = "_assert-first" }), "wrong-relay");
        await wrongRelay.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*no outstanding AuthnRequest*");

        var identity = await Consume(sut, BuildResponse(o => o with { InResponseTo = requestId, AssertionId = "_assert-second" }), relayState);
        identity.ProviderKey.Should().Be("jane@corp.example.test");
    }

    // ── Malformed payloads ─────────────────────────────────────────────────────

    [Theory]
    [InlineData("not base64 !!!")]
    public async Task ConsumeAssertionAsync_InvalidBase64_Rejected(string payload)
    {
        var sut = CreateSut(AuthOptionsWith(ProviderOptions()));

        var act = () => Consume(sut, payload);

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*Base64*");
    }

    [Fact]
    public async Task ConsumeAssertionAsync_MalformedXml_Rejected()
    {
        var sut = CreateSut(AuthOptionsWith(ProviderOptions()));

        var act = () => Consume(sut, Convert.ToBase64String("<not-saml/>"u8.ToArray()));

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*samlp:Response*");
    }

    [Fact]
    public async Task ConsumeAssertionAsync_DtdPayload_Rejected()
    {
        var sut = CreateSut(AuthOptionsWith(ProviderOptions()));
        var payload = "<?xml version=\"1.0\"?><!DOCTYPE samlp:Response [<!ENTITY xxe SYSTEM \"file:///etc/passwd\">]><samlp:Response xmlns:samlp=\"urn:oasis:names:tc:SAML:2.0:protocol\"/>";

        var act = () => Consume(sut, Convert.ToBase64String(Encoding.UTF8.GetBytes(payload)));

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*DTD*");
    }

    [Fact]
    public async Task ConsumeAssertionAsync_MultipleAssertions_Rejected()
    {
        var sut = CreateSut(AuthOptionsWith(ProviderOptions()));
        var requestId = await IssuePendingRequestIdAsync(sut);
        var samlResponse = BuildResponse(o => o with { InResponseTo = requestId }, duplicateAssertion: true);

        var act = () => Consume(sut, samlResponse);

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*exactly one assertion*");
    }

    // ── MFA context + email domain gate ────────────────────────────────────────

    [Fact]
    public async Task ConsumeAssertionAsync_MfaAuthnContext_DetectedAsMfaProof()
    {
        var sut = CreateSut(AuthOptionsWith(ProviderOptions()));
        var requestId = await IssuePendingRequestIdAsync(sut);

        var identity = await Consume(sut, BuildResponse(o => o with
        {
            InResponseTo = requestId,
            AuthnContextClassRef = "urn:oasis:names:tc:SAML:2.0:ac:classes:TimeSyncToken"
        }));

        identity.HasMfaProof.Should().BeTrue();
    }

    [Fact]
    public async Task ConsumeAssertionAsync_CustomMfaContextList_OnlyConfiguredClassesCount()
    {
        var sut = CreateSut(AuthOptionsWith(ProviderOptions(mfaContexts: ["urn:corp:acr:custom-mfa"])));
        var requestId = await IssuePendingRequestIdAsync(sut);

        var standardMfa = await Consume(sut, BuildResponse(o => o with
        {
            InResponseTo = requestId,
            AuthnContextClassRef = "urn:oasis:names:tc:SAML:2.0:ac:classes:TimeSyncToken"
        }));
        standardMfa.HasMfaProof.Should().BeFalse("the standard class is not in this provider's configured MFA set");

        var secondRequestId = await IssuePendingRequestIdAsync(sut);
        var customMfa = await Consume(sut, BuildResponse(o => o with
        {
            InResponseTo = secondRequestId,
            AuthnContextClassRef = "urn:corp:acr:custom-mfa"
        }));
        customMfa.HasMfaProof.Should().BeTrue();
    }

    [Fact]
    public async Task ConsumeAssertionAsync_EmailOutsideConfiguredDomains_Rejected()
    {
        var sut = CreateSut(AuthOptionsWith(ProviderOptions(emailDomains: ["corp.example.test"])));
        var requestId = await IssuePendingRequestIdAsync(sut);

        var act = () => Consume(sut, BuildResponse(o => o with
        {
            InResponseTo = requestId,
            NameId = "intruder@attacker.example.test",
            Email = "intruder@attacker.example.test"
        }));

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*not served by SAML provider*");
    }

    [Fact]
    public async Task ConsumeAssertionAsync_NameIdFormatMismatch_Rejected()
    {
        var sut = CreateSut(AuthOptionsWith(ProviderOptions(nameIdFormat: "urn:oasis:names:tc:SAML:1.1:nameid-format:persistent")));
        var requestId = await IssuePendingRequestIdAsync(sut);

        var act = () => Consume(sut, BuildResponse(o => o with { InResponseTo = requestId }));

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*NameID format*");
    }

    // ── SP metadata + logout forwarding ────────────────────────────────────────

    [Fact]
    public void BuildServiceProviderMetadata_ContainsSpDescriptor()
    {
        var sut = CreateSut(AuthOptionsWith(ProviderOptions()));

        var metadata = sut.BuildServiceProviderMetadata(Slug);

        metadata.Should().Contain($"entityID=\"{SpEntityId}\"");
        metadata.Should().Contain("WantAssertionsSigned=\"true\"");
        metadata.Should().Contain($"Location=\"{AcsUrl}\"");
        metadata.Should().Contain("urn:oasis:names:tc:SAML:2.0:bindings:HTTP-POST");
        metadata.Should().Contain("urn:oasis:names:tc:SAML:1.1:nameid-format:unspecified");
    }

    [Fact]
    public void BuildServiceProviderMetadata_ConfiguredNameIdFormat_Used()
    {
        var sut = CreateSut(AuthOptionsWith(ProviderOptions(nameIdFormat: "urn:oasis:names:tc:SAML:1.1:nameid-format:emailAddress")));

        var metadata = sut.BuildServiceProviderMetadata(Slug);

        metadata.Should().Contain("urn:oasis:names:tc:SAML:1.1:nameid-format:emailAddress");
    }

    [Fact]
    public void BuildLogoutRedirectUrl_WithoutSloService_ReturnsNull()
    {
        var sut = CreateSut(AuthOptionsWith(ProviderOptions()));

        sut.BuildLogoutRedirectUrl(Slug, null).Should().BeNull();
    }

    [Fact]
    public void BuildLogoutRedirectUrl_WithSloService_BuildsLogoutRequestRedirect()
    {
        var sut = CreateSut(AuthOptionsWith(ProviderOptions(sloUrl: "https://sts.corp.example.test/slo")));

        var url = sut.BuildLogoutRedirectUrl(Slug, "https://gameguild.example.test/bye");

        url.Should().StartWith("https://sts.corp.example.test/slo?SAMLRequest=");
        url.Should().Contain($"RelayState={Uri.EscapeDataString("https://gameguild.example.test/bye")}");
        DeflateDecode(url!).Should().Contain("<samlp:LogoutRequest").And.Contain($"<saml:Issuer>{SpEntityId}</saml:Issuer>");
    }

    // ── Email-domain discovery ─────────────────────────────────────────────────

    [Fact]
    public void FindProvidersForEmailDomain_MatchesEnabledSamlProvidersCaseInsensitively()
    {
        var sut = CreateSut(AuthOptionsWith(ProviderOptions(emailDomains: ["Corp.Example.Test"])));

        var providers = sut.FindProvidersForEmailDomain("JANE@CORP.example.test");

        providers.Should().ContainSingle().Which.Should().Match<OidcDiscoveredProvider>(provider =>
            provider.Slug == Slug &&
            provider.DisplayName == Slug &&
            provider.Protocol == "saml");
    }

    [Fact]
    public void FindProvidersForEmailDomain_DisabledProviderOrUnknownDomain_ReturnsEmpty()
    {
        var disabled = ProviderOptions(emailDomains: ["corp.example.test"]);
        disabled.Enabled = false;
        var disabledSut = CreateSut(AuthOptionsWith(disabled));
        disabledSut.FindProvidersForEmailDomain("jane@corp.example.test").Should().BeEmpty();

        var enabledSut = CreateSut(AuthOptionsWith(ProviderOptions(emailDomains: ["corp.example.test"])));
        enabledSut.FindProvidersForEmailDomain("jane@unknown.example.test").Should().BeEmpty();
        enabledSut.FindProvidersForEmailDomain("   ").Should().BeEmpty();
    }

    // ── Test identity provider (signed SAML response builder) ──────────────────

    private sealed record ResponseScenario(
        string? InResponseTo = "_pending",
        bool SignAssertion = true,
        bool SignResponse = false,
        RSA? SigningKey = null,
        string Issuer = IdpEntityId,
        string Audience = SpEntityId,
        string Destination = AcsUrl,
        string NameId = "jane@corp.example.test",
        string? Email = "jane@corp.example.test",
        string EmailAttributeName = "email",
        string? Name = "Jane Corp",
        string NameAttributeName = "name",
        DateTimeOffset? ConditionsNotBefore = null,
        DateTimeOffset? ConditionsNotOnOrAfter = null,
        DateTimeOffset? SubjectNotOnOrAfter = null,
        bool OmitConditionsNotOnOrAfter = false,
        bool OmitAudienceRestriction = false,
        string AuthnContextClassRef = "urn:oasis:names:tc:SAML:2.0:ac:classes:PasswordProtectedTransport",
        string? AssertionId = null,
        string? SubjectInResponseTo = null,
        bool OmitSubjectInResponseTo = false);

    private string BuildResponse(Func<ResponseScenario, ResponseScenario> configure, string? referenceUriOverride = null, bool duplicateAssertion = false)
    {
        var scenario = configure(new ResponseScenario(
            ConditionsNotBefore: DateTimeOffset.UtcNow.AddMinutes(-2),
            ConditionsNotOnOrAfter: DateTimeOffset.UtcNow.AddMinutes(5),
            SubjectNotOnOrAfter: DateTimeOffset.UtcNow.AddMinutes(5)));
        return BuildResponse(scenario, referenceUriOverride, duplicateAssertion);
    }

    private string BuildResponse(ResponseScenario scenario, string? referenceUriOverride, bool duplicateAssertion)
    {
        var signingKey = scenario.SigningKey ?? _idpKey;
        var responseId = "_resp" + Guid.NewGuid().ToString("N");
        var assertionId = scenario.AssertionId ?? "_assert" + Guid.NewGuid().ToString("N");
        var issueInstant = XmlConvert.ToString(DateTimeOffset.UtcNow);
        var inResponseTo = scenario.InResponseTo is null ? string.Empty : $" InResponseTo=\"{scenario.InResponseTo}\"";
        var notBefore = scenario.ConditionsNotBefore is { } nb ? $" NotBefore=\"{XmlConvert.ToString(nb)}\"" : string.Empty;
        var notOnOrAfter = scenario.OmitConditionsNotOnOrAfter
            ? string.Empty
            : $" NotOnOrAfter=\"{XmlConvert.ToString(scenario.ConditionsNotOnOrAfter ?? DateTimeOffset.UtcNow.AddMinutes(5))}\"";
        var subjectInResponseTo = scenario.OmitSubjectInResponseTo
            ? string.Empty
            : (scenario.SubjectInResponseTo ?? scenario.InResponseTo) is { } correlation
                ? $" InResponseTo=\"{correlation}\""
                : string.Empty;
        var audienceXml = scenario.OmitAudienceRestriction
            ? string.Empty
            : $"<saml:AudienceRestriction><saml:Audience>{scenario.Audience}</saml:Audience></saml:AudienceRestriction>";
        var attributes = new StringBuilder();
        if (scenario.Email is not null)
        {
            attributes.Append($"<saml:Attribute Name=\"{scenario.EmailAttributeName}\"><saml:AttributeValue>{scenario.Email}</saml:AttributeValue></saml:Attribute>");
        }

        if (scenario.Name is not null)
        {
            attributes.Append($"<saml:Attribute Name=\"{scenario.NameAttributeName}\"><saml:AttributeValue>{scenario.Name}</saml:AttributeValue></saml:Attribute>");
        }

        var xml = $"""
<samlp:Response xmlns:samlp="urn:oasis:names:tc:SAML:2.0:protocol" xmlns:saml="urn:oasis:names:tc:SAML:2.0:assertion" ID="{responseId}" Version="2.0" IssueInstant="{issueInstant}" Destination="{scenario.Destination}"{inResponseTo}>
  <saml:Issuer>{scenario.Issuer}</saml:Issuer>
  <samlp:Status><samlp:StatusCode Value="urn:oasis:names:tc:SAML:2.0:status:Success"/></samlp:Status>
  <saml:Assertion ID="{assertionId}" Version="2.0" IssueInstant="{issueInstant}">
    <saml:Issuer>{scenario.Issuer}</saml:Issuer>
    <saml:Subject>
      <saml:NameID Format="urn:oasis:names:tc:SAML:1.1:nameid-format:emailAddress">{scenario.NameId}</saml:NameID>
      <saml:SubjectConfirmation Method="urn:oasis:names:tc:SAML:2.0:cm:bearer">
        <saml:SubjectConfirmationData{subjectInResponseTo} NotOnOrAfter="{XmlConvert.ToString(scenario.SubjectNotOnOrAfter ?? DateTimeOffset.UtcNow.AddMinutes(5))}" Recipient="{AcsUrl}"/>
      </saml:SubjectConfirmation>
    </saml:Subject>
    <saml:Conditions{notBefore}{notOnOrAfter}>{audienceXml}</saml:Conditions>
    <saml:AuthnStatement AuthnInstant="{issueInstant}">
      <saml:AuthnContext><saml:AuthnContextClassRef>{scenario.AuthnContextClassRef}</saml:AuthnContextClassRef></saml:AuthnContext>
    </saml:AuthnStatement>
    <saml:AttributeStatement>{attributes}</saml:AttributeStatement>
  </saml:Assertion>
</samlp:Response>
""";

        var document = new XmlDocument { PreserveWhitespace = true };
        document.LoadXml(xml);
        var responseElement = document.DocumentElement!;
        var assertionElement = (XmlElement)responseElement.GetElementsByTagName("Assertion", "urn:oasis:names:tc:SAML:2.0:assertion")[0]!;

        if (duplicateAssertion)
        {
            // A distinct ID on the duplicate: XML-DSig signing refuses same-ID elements,
            // and a real multi-assertion response would never reuse an assertion ID.
            var duplicate = (XmlElement)assertionElement.CloneNode(true);
            duplicate.SetAttribute("ID", "_assert-dup" + Guid.NewGuid().ToString("N"));
            responseElement.InsertAfter(duplicate, assertionElement);
        }

        if (scenario.SignAssertion)
        {
            SignElement(assertionElement, signingKey, "#" + assertionId);
        }

        if (scenario.SignResponse)
        {
            SignElement(responseElement, signingKey, referenceUriOverride == "assertion" ? "#" + assertionId : "#" + responseId);
        }

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(document.OuterXml));
    }

    private static void SignElement(XmlElement element, RSA key, string referenceUri)
    {
        var signedXml = new SignedXml(element) { SigningKey = key };
        signedXml.SignedInfo!.CanonicalizationMethod = SignedXml.XmlDsigExcC14NTransformUrl;
        signedXml.SignedInfo!.SignatureMethod = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";

        var reference = new Reference(referenceUri) { DigestMethod = "http://www.w3.org/2001/04/xmlenc#sha256" };
        reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
        reference.AddTransform(new XmlDsigExcC14NTransform());
        signedXml.AddReference(reference);
        signedXml.ComputeSignature();

        var signature = signedXml.GetXml();
        var issuer = element.ChildNodes.Cast<XmlNode>().FirstOrDefault(node => node.LocalName == "Issuer");
        element.InsertAfter(element.OwnerDocument!.ImportNode(signature, true), issuer);
    }

    private static string ExtractAssertionId(string samlResponseBase64)
    {
        var document = new XmlDocument();
        document.LoadXml(Encoding.UTF8.GetString(Convert.FromBase64String(samlResponseBase64)));
        var assertion = document.DocumentElement!.GetElementsByTagName("Assertion", "urn:oasis:names:tc:SAML:2.0:assertion")[0]!;
        return ((XmlElement)assertion).GetAttribute("ID");
    }

    private static string DeflateDecode(string redirectUrl)
    {
        var query = redirectUrl[(redirectUrl.LastIndexOf('?') + 1)..];
        var samlRequest = query
            .Split('&')
            .Single(parameter => parameter.StartsWith("SAMLRequest=", StringComparison.Ordinal))
            ["SAMLRequest=".Length..];
        // The redirect binding percent-encodes the standard Base64 deflated payload.
        var encoded = Uri.UnescapeDataString(samlRequest);

        using var compressed = new MemoryStream(Convert.FromBase64String(encoded));
        using var inflater = new DeflateStream(compressed, CompressionMode.Decompress);
        using var decompressed = new MemoryStream();
        inflater.CopyTo(decompressed);
        return Encoding.UTF8.GetString(decompressed.ToArray());
    }
}
