namespace GameGuild.Modules.Billing.Exceptions;

/// <summary>
///     Exception thrown when webhook signature is invalid
/// </summary>
public class InvalidWebhookSignatureException : Exception
{
    public InvalidWebhookSignatureException() { }

    public InvalidWebhookSignatureException(string message) : base(message) { }

    public InvalidWebhookSignatureException(string message, Exception innerException) : base(message, innerException) { }
}

