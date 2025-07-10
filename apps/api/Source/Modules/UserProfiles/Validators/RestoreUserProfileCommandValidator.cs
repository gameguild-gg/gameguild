using FluentValidation;
using GameGuild.Database;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.UserProfiles;

/// <summary>
/// FluentValidation validator for RestoreUserProfileCommand
/// </summary>
public class RestoreUserProfileCommandValidator : AbstractValidator<RestoreUserProfileCommand> {
  private readonly ApplicationDbContext _context;

  public RestoreUserProfileCommandValidator(ApplicationDbContext context) {
    _context = context;

    RuleFor(x => x.UserProfileId)
      .NotEmpty()
      .WithMessage("User profile ID is required")
      .MustAsync(DeletedUserProfileExists)
      .WithMessage("Deleted user profile not found");
  }

  private async Task<bool> DeletedUserProfileExists(Guid userProfileId, CancellationToken cancellationToken) {
    return await _context.Resources.OfType<UserProfile>()
                         .AnyAsync(x => x.Id == userProfileId && x.DeletedAt != null, cancellationToken);
  }
}

