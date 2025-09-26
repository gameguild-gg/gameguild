using FluentValidation;

namespace GameGuild.Modules.UserProfiles;

/// <summary> FluentValidation validator for RestoreUserProfileCommand </summary>
public class RestoreUserProfileCommandValidator : AbstractValidator<RestoreUserProfileCommand>
{
    private readonly IUserProfileRepository _userProfileRepository;

    public RestoreUserProfileCommandValidator(IUserProfileRepository userProfileRepository)
    {
        _userProfileRepository = userProfileRepository;

        RuleFor(x => x.UserProfileId).NotEmpty().WithMessage("User profile ID is required").MustAsync(DeletedUserProfileExists).WithMessage("Deleted user profile not found");
    }

    private async Task<bool> DeletedUserProfileExists(Guid userProfileId, CancellationToken cancellationToken) { return await _userProfileRepository.DeletedExistsAsync(userProfileId, cancellationToken); }
}
