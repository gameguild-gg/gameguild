using FluentValidation;

namespace GameGuild.Identity.Users;

public class ReplaceUserPrivacyPreferencesCommandValidator : AbstractValidator<ReplaceUserPrivacyPreferencesCommand>
{
    public ReplaceUserPrivacyPreferencesCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Request).NotNull();
        RuleFor(x => x.Request.PrivacyPreferences).NotNull();
    }
}
