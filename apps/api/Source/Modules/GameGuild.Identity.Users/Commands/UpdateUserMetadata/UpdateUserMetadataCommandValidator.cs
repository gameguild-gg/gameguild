using FluentValidation;

namespace GameGuild.Identity.Users;

/// <summary>
///     Validator for UpdateUserMetadataCommand
/// </summary>
public class UpdateUserMetadataCommandValidator : AbstractValidator<UpdateUserMetadataCommand>
{
    public UpdateUserMetadataCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");

        RuleFor(x => x.Request)
            .NotNull()
            .WithMessage("Request body is required");

        RuleFor(x => x.Request)
            .Must(r => r.CustomFields != null || r.TagsToAdd != null || r.TagsToRemove != null || r.ExternalReferences != null)
            .WithMessage("At least one field must be provided for update");
    }
}
