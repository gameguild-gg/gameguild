using FluentValidation;

namespace GameGuild.Identity.Users;

public sealed class ResetUserPrivacyPreferencesCommandValidator : AbstractValidator<ResetUserPrivacyPreferencesCommand>
{
    public ResetUserPrivacyPreferencesCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}
