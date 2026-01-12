using FluentValidation;

namespace GameGuild.Identity.Users;

public class ResetUserPrivacyPreferencesCommandValidator : AbstractValidator<ResetUserPrivacyPreferencesCommand>
{
    public ResetUserPrivacyPreferencesCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}
