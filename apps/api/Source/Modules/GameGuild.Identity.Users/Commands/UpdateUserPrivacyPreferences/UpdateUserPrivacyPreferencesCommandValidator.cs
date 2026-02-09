using FluentValidation;

namespace GameGuild.Identity.Users;

public sealed class UpdateUserPrivacyPreferencesCommandValidator : AbstractValidator<UpdateUserPrivacyPreferencesCommand>
{
    public UpdateUserPrivacyPreferencesCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Request).NotNull();
        RuleFor(x => x.Request.PrivacyPreferences).NotNull().NotEmpty();
    }
}
