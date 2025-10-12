using FluentValidation;
using GameGuild.Modules.Users;

namespace GameGuild.Modules.UserProfiles;

/// <summary>
/// FluentValidation validator for CreateUserProfileCommand
/// </summary>
public class CreateUserProfileCommandValidator : AbstractValidator<CreateUserProfileCommand>
{
    private readonly IUserProfileRepository _userProfileRepository;

    private readonly IUserRepository _userRepository;

    public CreateUserProfileCommandValidator(IUserProfileRepository userProfileRepository, IUserRepository userRepository)
    {
        _userProfileRepository = userProfileRepository;
        _userRepository = userRepository;

        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .WithMessage("Display name is required")
            .Length(2, 100)
            .WithMessage("Display name must be between 2 and 100 characters")
            .MustAsync(BeUniqueDisplayName)
            .WithMessage("Display name must be unique");

        RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required").MustAsync(BeValidUser).WithMessage("User does not exist").MustAsync(NotHaveExistingProfile).WithMessage("User already has a profile");
    }

    private async Task<bool> BeUniqueDisplayName(string displayName, CancellationToken cancellationToken) { return await _userProfileRepository.IsDisplayNameUniqueAsync(displayName, null, cancellationToken); }

    private async Task<bool> BeValidUser(Guid userId, CancellationToken cancellationToken)
    {
        User? user = await _userRepository.GetByIdAsync(userId, cancellationToken);

        return user != null;
    }

    private async Task<bool> NotHaveExistingProfile(Guid userId, CancellationToken cancellationToken) { return !await _userProfileRepository.ExistsForUserAsync(userId, cancellationToken); }
}
