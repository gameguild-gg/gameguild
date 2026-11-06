namespace GameGuild.Identity.Authentication;

/// <summary>A certificate pin is already assigned to another service account.</summary>
public sealed class ServiceAccountCertificateBindingConflictException(Exception innerException)
    : Exception("The certificate is already bound to a service account.", innerException);
