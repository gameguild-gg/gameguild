namespace GameGuild.Commerce.Billing;

public sealed class InvalidWebhookPayloadException(string message, Exception? innerException = null)
    : Exception(message, innerException);
