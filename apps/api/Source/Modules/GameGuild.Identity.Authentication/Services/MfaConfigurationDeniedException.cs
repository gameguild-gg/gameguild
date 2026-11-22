namespace GameGuild.Identity.Authentication;

/// <summary>A known MFA configuration denial, distinct from storage or cryptographic failures.</summary>
internal sealed class MfaConfigurationDeniedException(string message) : InvalidOperationException(message);
