using FluentValidation;

namespace GameGuild.Users.Commands;

public class UpdateUserPrivacyPreferencesCommandValidator : AbstractValidator<UpdateUserPrivacyPreferencesCommand>
{
    public UpdateUserPrivacyPreferencesCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Request).NotNull();
        RuleFor(x => x.Request.PrivacyPreferences).NotNull().NotEmpty();
    }
}
