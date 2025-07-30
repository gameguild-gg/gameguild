using FluentValidation;
using GameGuild.Database;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.UserProfiles;

/// <summary>
/// FluentValidation validator for DeleteUserProfileCommand
/// </summary>
public class DeleteUserProfileCommandValidator : AbstractValidator<DeleteUserProfileCommand> {
  private readonly ApplicationDbContext _context;

  public DeleteUserProfileCommandValidator(ApplicationDbContext context) {
    _context = context;

    RuleFor(x => x.UserProfileId)
      .NotEmpty()
      .WithMessage("User profile ID is required")
      .MustAsync(UserProfileExists)
      .WithMessage("User profile not found");
  }

  private async Task<bool> UserProfileExists(Guid userProfileId, CancellationToken cancellationToken) {
    return await _context.Resources.OfType<UserProfile>()
                         .AnyAsync(x => x.Id == userProfileId && x.DeletedAt == null, cancellationToken);
  }
}
