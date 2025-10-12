using FluentValidation;

namespace GameGuild.Modules.UserProfiles;

/// <summary> FluentValidation validator for DeleteUserProfileCommand </summary>
public class DeleteUserProfileCommandValidator : AbstractValidator<DeleteUserProfileCommand>
{
    private readonly IUserProfileRepository _userProfileRepository;

    public DeleteUserProfileCommandValidator(IUserProfileRepository userProfileRepository)
    {
        _userProfileRepository = userProfileRepository;

        RuleFor(x => x.UserProfileId).NotEmpty().WithMessage("User profile ID is required").MustAsync(UserProfileExists).WithMessage("User profile not found");
    }

    private async Task<bool> UserProfileExists(Guid userProfileId, CancellationToken cancellationToken) { return await _userProfileRepository.ExistsAsync(userProfileId, cancellationToken); }
}
