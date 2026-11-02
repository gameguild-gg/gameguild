using FluentValidation;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Validator for <see cref="BindServiceAccountCertificateCommand" /> (issue #1146).
///     The thumbprint must normalize to 40 (SHA-1) or 64 (SHA-256) hex characters; the
///     optional SPKI pin must normalize to exactly 64 hex characters (SHA-256).
/// </summary>
public sealed class BindServiceAccountCertificateCommandValidator : AbstractValidator<BindServiceAccountCertificateCommand>
{
    public BindServiceAccountCertificateCommandValidator()
    {
        RuleFor(command => command.ServiceAccountId)
            .NotEmpty().WithMessage("Service account id is required");

        RuleFor(command => command.Thumbprint)
            .NotEmpty().WithMessage("Certificate thumbprint is required")
            .Must(BeHexPinOfLength40Or64).WithMessage(
                "Certificate thumbprint must be 40 (SHA-1) or 64 (SHA-256) hex characters, optionally separated by colons or spaces");

        RuleFor(command => command.SpkiSha256)
            .Must(BeHexPinOfLength64).When(command => command.SpkiSha256 is not null).WithMessage(
                "SPKI pin must be 64 hex characters (SHA-256), optionally separated by colons or spaces");
    }

    internal static bool BeHexPinOfLength40Or64(string? value) =>
        ServiceAccountService.NormalizeHexPin(value ?? string.Empty, 40) is not null ||
        ServiceAccountService.NormalizeHexPin(value ?? string.Empty, 64) is not null;

    internal static bool BeHexPinOfLength64(string? value) =>
        value is null || ServiceAccountService.NormalizeHexPin(value, 64) is not null;
}
