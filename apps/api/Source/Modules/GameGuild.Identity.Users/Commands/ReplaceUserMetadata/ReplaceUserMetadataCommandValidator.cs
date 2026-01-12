using FluentValidation;

namespace GameGuild.Identity.Users;

/// <summary>
///     Validator for ReplaceUserMetadataCommand
/// </summary>
public class ReplaceUserMetadataCommandValidator : AbstractValidator<ReplaceUserMetadataCommand>
{
    public ReplaceUserMetadataCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");

        RuleFor(x => x.Request)
            .NotNull()
            .WithMessage("Request body is required");

        RuleFor(x => x.Request.CustomFields)
            .NotNull()
            .WithMessage("Custom fields are required for replace operation");

        RuleFor(x => x.Request.Tags)
            .NotNull()
            .WithMessage("Tags are required for replace operation");

        RuleFor(x => x.Request.ExternalReferences)
            .NotNull()
            .WithMessage("External references are required for replace operation");
    }
}
