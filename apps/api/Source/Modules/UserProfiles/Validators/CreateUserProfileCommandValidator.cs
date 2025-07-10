using FluentValidation;
using GameGuild.Database;
using GameGuild.Modules.Users;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.UserProfiles;

/// <summary>
/// FluentValidation validator for CreateUserProfileCommand
/// </summary>
public class CreateUserProfileCommandValidator : AbstractValidator<CreateUserProfileCommand> {
  private readonly ApplicationDbContext _context;

  public CreateUserProfileCommandValidator(ApplicationDbContext context) {
    _context = context;

    RuleFor(x => x.GivenName)
      .NotEmpty()
      .WithMessage("Given name is required")
      .Length(1, 100)
      .WithMessage("Given name must be between 1 and 100 characters")
      .Matches(@"^[a-zA-Z\s\-'\.]+$")
      .WithMessage("Given name can only contain letters, spaces, hyphens, apostrophes, and periods");

    RuleFor(x => x.FamilyName)
      .NotEmpty()
      .WithMessage("Family name is required")
      .Length(1, 100)
      .WithMessage("Family name must be between 1 and 100 characters")
      .Matches(@"^[a-zA-Z\s\-'\.]+$")
      .WithMessage("Family name can only contain letters, spaces, hyphens, apostrophes, and periods");

    RuleFor(x => x.DisplayName)
      .NotEmpty()
      .WithMessage("Display name is required")
      .Length(2, 100)
      .WithMessage("Display name must be between 2 and 100 characters")
      .MustAsync(BeUniqueDisplayName)
      .WithMessage("Display name must be unique");

    RuleFor(x => x.Title)
      .MaximumLength(200)
      .WithMessage("Title cannot exceed 200 characters");

    RuleFor(x => x.Description)
      .MaximumLength(1000)
      .WithMessage("Description cannot exceed 1000 characters");

    RuleFor(x => x.UserId)
      .NotEmpty()
      .WithMessage("User ID is required")
      .MustAsync(BeValidUser)
      .WithMessage("User does not exist")
      .MustAsync(NotHaveExistingProfile)
      .WithMessage("User already has a profile");
  }

  private async Task<bool> BeUniqueDisplayName(string displayName, CancellationToken cancellationToken) {
    return !await _context.Resources.OfType<UserProfile>()
                          .AnyAsync(x => x.DisplayName == displayName && x.DeletedAt == null, cancellationToken);
  }

  private async Task<bool> BeValidUser(Guid userId, CancellationToken cancellationToken) {
    return await _context.Users
                         .AnyAsync(x => x.Id == userId && x.DeletedAt == null, cancellationToken);
  }

  private async Task<bool> NotHaveExistingProfile(Guid userId, CancellationToken cancellationToken) {
    return !await _context.Resources.OfType<UserProfile>()
                          .AnyAsync(x => x.Id == userId && x.DeletedAt == null, cancellationToken);
  }
}
