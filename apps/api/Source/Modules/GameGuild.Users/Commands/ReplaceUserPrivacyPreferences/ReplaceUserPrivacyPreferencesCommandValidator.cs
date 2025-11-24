using FluentValidation;

namespace GameGuild.Users.Commands;

public class ReplaceUserPrivacyPreferencesCommandValidator : AbstractValidator<ReplaceUserPrivacyPreferencesCommand>
{
    public ReplaceUserPrivacyPreferencesCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Request).NotNull();
        RuleFor(x => x.Request.PrivacyPreferences).NotNull();
    }
}
