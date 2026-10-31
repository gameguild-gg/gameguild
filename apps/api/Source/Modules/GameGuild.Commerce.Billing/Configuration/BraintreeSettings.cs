using System.ComponentModel.DataAnnotations;

namespace GameGuild.Commerce.Billing;

/// <summary>
///     Braintree webhook verification settings (issue #408: Braintree webhook signature
///     verification owned by the billing ingress). Braintree signs webhook deliveries with
///     the gateway's public key: the <c>bt_signature</c> form field carries
///     <c>{public key}|{sha1(public key + "|" + bt_payload)}</c>, so verification is fully
///     offline — only the public key of the API key pair is needed here. The private key
///     stays with the Commerce.Payments processing adapter and is never read by billing.
/// </summary>
public class BraintreeSettings
{
    /// <summary>
    ///     Braintree environment the webhook endpoint serves: <c>sandbox</c> or <c>production</c>.
    ///     Informational today (verification itself is key-based, not endpoint-based); kept so
    ///     operators can assert the pairing of endpoint and key.
    /// </summary>
    [Required]
    public string Environment { get; set; } = "sandbox";

    /// <summary>
    ///     Public key of the Braintree API key pair whose webhook destination is this endpoint.
    ///     Deliveries whose <c>bt_signature</c> prefix a different key are rejected as not
    ///     addressed to this gateway. Fail-closed: when empty, every delivery fails verification.
    ///     Value comes from configuration (environment/secret store); never a literal in source.
    /// </summary>
    public string PublicKey { get; set; } = string.Empty;
}
