using FluentValidation;

namespace GameGuild.Users.Commands;

public class ReplaceUserProfileCommandValidator : AbstractValidator<ReplaceUserProfileCommand>
{
    public ReplaceUserProfileCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Request).NotNull();
        RuleFor(x => x.Request.ProfileVisibility).NotEmpty();
    }
}
