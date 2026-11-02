using FluentValidation;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Validators for the email-MFA enrollment and challenge commands (issue #1152).
/// </summary>
public sealed class StartEmailMfaEnrollmentCommandValidator : AbstractValidator<StartEmailMfaEnrollmentCommand>
{
    public StartEmailMfaEnrollmentCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty().WithMessage("User id is required");
    }
}

public sealed class CompleteEmailMfaEnrollmentCommandValidator : AbstractValidator<CompleteEmailMfaEnrollmentCommand>
{
    private static readonly System.Text.RegularExpressions.Regex SixDigitCode =
        new("^[0-9]{6}$", System.Text.RegularExpressions.RegexOptions.Compiled);

    public CompleteEmailMfaEnrollmentCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty().WithMessage("User id is required");

        RuleFor(command => command.Code)
            .NotEmpty().WithMessage("Verification code is required")
            .Matches(SixDigitCode).WithMessage("Verification code must be exactly six digits");
    }
}

public sealed class SendEmailMfaChallengeCommandValidator : AbstractValidator<SendEmailMfaChallengeCommand>
{
    public SendEmailMfaChallengeCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty().WithMessage("User id is required");
    }
}
