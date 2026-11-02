using System.IO.Compression;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;
using FluentAssertions;

namespace GameGuild.Tests.Authentication.Integration;

/// <summary>
///     A real in-test SAML 2.0 identity provider (issue #268 acceptance): self-signed
///     X.509 signing certificate, genuine XML-DSig (RSA-SHA256, exclusive c14n) response
///     signing, and real AuthnRequest parsing — the exact artefacts a configured
///     enterprise IdP produces. Nothing about the transport or the signatures is mocked:
///     the API host validates the harness output end-to-end over HTTP.
/// </summary>
public sealed class TestSamlIdentityProvider : IDisposable
{
    public const string Slug = "corp-idp";
    public const string SecondSlug = "partner-idp";
    public const string SpEntityId = "https://gameguild.example.test/auth/saml";
    public const string AcsUrl = "https://gameguild.example.test/auth/saml/corp-idp/callback";
    public const string SecondAcsUrl = "https://gameguild.example.test/auth/saml/partner-idp/callback";
    public const string IdpEntityId = "https://sts.corp.example.test";
    public const string IdpSignInUrl = "https://sts.corp.example.test/sso";
    public const string IdpSingleLogoutUrl = "https://sts.corp.example.test/slo";
    public const string SubjectEmail = "jane@corp.example.test";
    public const string PasswordAuthnContext = "urn:oasis:names:tc:SAML:2.0:ac:classes:PasswordProtectedTransport";
    public const string MfaAuthnContext = "urn:oasis:names:tc:SAML:2.0:ac:classes:TimeSyncToken";

    private const string SamlpNamespace = "urn:oasis:names:tc:SAML:2.0:protocol";
    private const string SamlNamespace = "urn:oasis:names:tc:SAML:2.0:assertion";

    private readonly RSA _signingKey = RSA.Create(2048);

    public TestSamlIdentityProvider()
    {
        using var certificate = CreateCertificate(_signingKey, DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddYears(1));
        SigningCertificateBase64 = Convert.ToBase64String(certificate.Export(X509ContentType.Cert));
    }

    /// <summary>Base64 DER signing certificate to install in the provider configuration.</summary>
    public string SigningCertificateBase64 { get; }

    public void Dispose() => _signingKey.Dispose();

    /// <summary>The parsed AuthnRequest the browser carried to the identity provider.</summary>
    public sealed record ParsedAuthnRequest(string Id, string Destination, string AssertionConsumerServiceUrl, string Issuer);

    /// <summary>Scenario knobs for the signed response (defaults model a healthy IdP).</summary>
    public sealed record ResponseOptions(
        string? InResponseTo = null,
        string? SubjectInResponseTo = null,
        bool SignAssertion = true,
        bool SignResponse = false,
        string Destination = AcsUrl,
        string Recipient = AcsUrl,
        string NameId = SubjectEmail,
        string Email = SubjectEmail,
        string Name = "Jane Corp",
        string AuthnContextClassRef = PasswordAuthnContext,
        string? AssertionId = null,
        string Issuer = IdpEntityId,
        string Audience = SpEntityId,
        bool OmitSubjectInResponseTo = false)
    {
        /// <summary>Default options answering <paramref name="request" /> (correlation echoed in both places).</summary>
        public static ResponseOptions ForRequest(ParsedAuthnRequest request, string? assertionId = null) => new(
            InResponseTo: request.Id,
            SubjectInResponseTo: request.Id,
            Destination: request.AssertionConsumerServiceUrl,
            Recipient: request.AssertionConsumerServiceUrl,
            AssertionId: assertionId);
    }

    /// <summary>
    ///     Parses the identity provider sign-on redirect URL (the browser leg of the
    ///     HTTP-Redirect binding): inflates the <c>SAMLRequest</c> and reads the
    ///     AuthnRequest fields the provider needs to answer.
    /// </summary>
    public ParsedAuthnRequest ParseAuthnRequest(string redirectUrl)
    {
        var query = redirectUrl[(redirectUrl.LastIndexOf('?') + 1)..];
        var samlRequest = query
            .Split('&')
            .Single(parameter => parameter.StartsWith("SAMLRequest=", StringComparison.Ordinal))
            ["SAMLRequest=".Length..];

        using var compressed = new MemoryStream(Convert.FromBase64String(Uri.UnescapeDataString(samlRequest)));
        using var inflater = new DeflateStream(compressed, CompressionMode.Decompress);
        using var decompressed = new MemoryStream();
        inflater.CopyTo(decompressed);
        var xml = Encoding.UTF8.GetString(decompressed.ToArray());

        var document = new XmlDocument();
        document.LoadXml(xml);
        var authnRequest = document.DocumentElement!;
        authnRequest.LocalName.Should().Be("AuthnRequest");
        authnRequest.NamespaceURI.Should().Be(SamlpNamespace);

        return new ParsedAuthnRequest(
            Id: authnRequest.GetAttribute("ID"),
            Destination: authnRequest.GetAttribute("Destination"),
            AssertionConsumerServiceUrl: authnRequest.GetAttribute("AssertionConsumerServiceURL"),
            Issuer: authnRequest.SelectSingleNode("saml:Issuer", Ns(document))!.InnerText!);
    }

    /// <summary>
    ///     Builds the Base64 <c>SAMLResponse</c> the identity provider posts to the
    ///     assertion consumer service: a genuinely-signed SAML 2.0 Response answering
    ///     <paramref name="request" /> for <paramref name="relayState" />.
    /// </summary>
    public string BuildSignedResponse(ParsedAuthnRequest request, string relayState, ResponseOptions? options = null)
    {
        options ??= ResponseOptions.ForRequest(request);
        var effectiveInResponseTo = options.InResponseTo ?? request.Id;
        var subjectInResponseTo = options.OmitSubjectInResponseTo
            ? string.Empty
            : $" InResponseTo=\"{options.SubjectInResponseTo ?? effectiveInResponseTo}\"";
        var responseInResponseTo = options.InResponseTo is null ? string.Empty : $" InResponseTo=\"{options.InResponseTo}\"";
        var assertionId = options.AssertionId ?? "_assert" + Guid.NewGuid().ToString("N");
        var responseId = "_resp" + Guid.NewGuid().ToString("N");
        var now = DateTimeOffset.UtcNow;
        var issueInstant = XmlConvert.ToString(now);
        _ = relayState; // echoed by the browser, not embedded in the response document

        var xml = $"""
<samlp:Response xmlns:samlp="urn:oasis:names:tc:SAML:2.0:protocol" xmlns:saml="urn:oasis:names:tc:SAML:2.0:assertion" ID="{responseId}" Version="2.0" IssueInstant="{issueInstant}" Destination="{options.Destination}"{responseInResponseTo}>
  <saml:Issuer>{options.Issuer}</saml:Issuer>
  <samlp:Status><samlp:StatusCode Value="urn:oasis:names:tc:SAML:2.0:status:Success"/></samlp:Status>
  <saml:Assertion ID="{assertionId}" Version="2.0" IssueInstant="{issueInstant}">
    <saml:Issuer>{options.Issuer}</saml:Issuer>
    <saml:Subject>
      <saml:NameID Format="urn:oasis:names:tc:SAML:1.1:nameid-format:emailAddress">{options.NameId}</saml:NameID>
      <saml:SubjectConfirmation Method="urn:oasis:names:tc:SAML:2.0:cm:bearer">
        <saml:SubjectConfirmationData{subjectInResponseTo} NotOnOrAfter="{XmlConvert.ToString(now.AddMinutes(5))}" Recipient="{options.Recipient}"/>
      </saml:SubjectConfirmation>
    </saml:Subject>
    <saml:Conditions NotBefore="{XmlConvert.ToString(now.AddMinutes(-2))}" NotOnOrAfter="{XmlConvert.ToString(now.AddMinutes(5))}">
      <saml:AudienceRestriction><saml:Audience>{options.Audience}</saml:Audience></saml:AudienceRestriction>
    </saml:Conditions>
    <saml:AuthnStatement AuthnInstant="{issueInstant}">
      <saml:AuthnContext><saml:AuthnContextClassRef>{options.AuthnContextClassRef}</saml:AuthnContextClassRef></saml:AuthnContext>
    </saml:AuthnStatement>
    <saml:AttributeStatement>
      <saml:Attribute Name="email"><saml:AttributeValue>{options.Email}</saml:AttributeValue></saml:Attribute>
      <saml:Attribute Name="name"><saml:AttributeValue>{options.Name}</saml:AttributeValue></saml:Attribute>
    </saml:AttributeStatement>
  </saml:Assertion>
</samlp:Response>
""";

        var document = new XmlDocument { PreserveWhitespace = true };
        document.LoadXml(xml);
        var responseElement = document.DocumentElement!;
        var assertionElement = (XmlElement)responseElement.GetElementsByTagName("Assertion", SamlNamespace)[0]!;

        if (options.SignAssertion)
        {
            SignElement(assertionElement, "#" + assertionId);
        }

        if (options.SignResponse)
        {
            SignElement(responseElement, "#" + responseId);
        }

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(document.OuterXml));
    }

    private void SignElement(XmlElement element, string referenceUri)
    {
        var signedXml = new SignedXml(element) { SigningKey = _signingKey };
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

    private static X509Certificate2 CreateCertificate(RSA key, DateTimeOffset notBefore, DateTimeOffset notAfter)
    {
        var request = new CertificateRequest("CN=test-saml-idp-signing", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return request.CreateSelfSigned(notBefore, notAfter);
    }

    private static XmlNamespaceManager Ns(XmlDocument document)
    {
        var manager = new XmlNamespaceManager(document.NameTable);
        manager.AddNamespace("samlp", SamlpNamespace);
        manager.AddNamespace("saml", SamlNamespace);
        return manager;
    }
}
