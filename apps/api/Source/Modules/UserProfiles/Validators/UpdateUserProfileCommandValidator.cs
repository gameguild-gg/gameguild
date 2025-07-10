using FluentValidation;
using GameGuild.Database;
using GameGuild.Modules.UserProfiles.Commands;
using GameGuild.Modules.UserProfiles.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Modules.UserProfiles.Validators;

/// <summary>
/// FluentValidation validator for UpdateUserProfileCommand
/// </summary>
public class UpdateUserProfileCommandValidator : AbstractValidator<UpdateUserProfileCommand>
{
    private readonly ApplicationDbContext _context;

    public UpdateUserProfileCommandValidator(ApplicationDbContext context)
    {
        _context = context;

        RuleFor(x => x.UserProfileId)
            .NotEmpty().WithMessage("User profile ID is required")
            .MustAsync(UserProfileExists).WithMessage("User profile not found");

        RuleFor(x => x.GivenName)
            .Length(1, 100).WithMessage("Given name must be between 1 and 100 characters")
            .Matches(@"^[a-zA-Z\s\-'\.]+$").WithMessage("Given name can only contain letters, spaces, hyphens, apostrophes, and periods")
            .When(x => !string.IsNullOrEmpty(x.GivenName));

        RuleFor(x => x.FamilyName)
            .Length(1, 100).WithMessage("Family name must be between 1 and 100 characters")
            .Matches(@"^[a-zA-Z\s\-'\.]+$").WithMessage("Family name can only contain letters, spaces, hyphens, apostrophes, and periods")
            .When(x => !string.IsNullOrEmpty(x.FamilyName));

        RuleFor(x => x.DisplayName)
            .Length(2, 100).WithMessage("Display name must be between 2 and 100 characters")
            .MustAsync(BeUniqueDisplayNameForUpdate).WithMessage("Display name must be unique")
            .When(x => !string.IsNullOrEmpty(x.DisplayName));

        RuleFor(x => x.Title)
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters")
            .When(x => !string.IsNullOrEmpty(x.Title));

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }

    private async Task<bool> UserProfileExists(Guid userProfileId, CancellationToken cancellationToken)
    {
        return await _context.Resources.OfType<UserProfile>()
            .AnyAsync(x => x.Id == userProfileId && x.DeletedAt == null, cancellationToken);
    }

    private async Task<bool> BeUniqueDisplayNameForUpdate(UpdateUserProfileCommand command, string displayName, CancellationToken cancellationToken)
    {
        return !await _context.Resources.OfType<UserProfile>()
            .AnyAsync(x => x.DisplayName == displayName && x.Id != command.UserProfileId && x.DeletedAt == null, cancellationToken);
    }
}
