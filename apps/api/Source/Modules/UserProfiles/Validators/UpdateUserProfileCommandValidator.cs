using FluentValidation;

namespace GameGuild.Modules.UserProfiles;

/// <summary>
/// FluentValidation validator for UpdateUserProfileCommand
/// </summary>
public class UpdateUserProfileCommandValidator : AbstractValidator<UpdateUserProfileCommand>
{
    private readonly IUserProfileRepository _userProfileRepository;

    public UpdateUserProfileCommandValidator(IUserProfileRepository userProfileRepository)
    {
        _userProfileRepository = userProfileRepository;

        RuleFor(x => x.UserProfileId).NotEmpty().WithMessage("User profile ID is required").MustAsync(UserProfileExists).WithMessage("User profile not found");

        RuleFor(x => x.DisplayName)
            .Length(2, 100)
            .WithMessage("Display name must be between 2 and 100 characters")
            .MustAsync(BeUniqueDisplayNameForUpdate)
            .WithMessage("Display name must be unique")
            .When(x => !string.IsNullOrEmpty(x.DisplayName));
    }

    private async Task<bool> UserProfileExists(Guid userProfileId, CancellationToken cancellationToken) { return await _userProfileRepository.ExistsAsync(userProfileId, cancellationToken); }

    private async Task<bool> BeUniqueDisplayNameForUpdate(UpdateUserProfileCommand command, string displayName, CancellationToken cancellationToken)
    {
        return await _userProfileRepository.IsDisplayNameUniqueAsync(displayName, command.UserProfileId, cancellationToken);
    }
}
