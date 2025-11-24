using FluentValidation;

namespace GameGuild.Users.Commands;

public class ResetUserPrivacyPreferencesCommandValidator : AbstractValidator<ResetUserPrivacyPreferencesCommand>
{
    public ResetUserPrivacyPreferencesCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}
