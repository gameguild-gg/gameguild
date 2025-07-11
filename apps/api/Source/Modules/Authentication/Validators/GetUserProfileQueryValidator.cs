using FluentValidation;


namespace GameGuild.Modules.Authentication.Validators;

/// <summary>
/// Validator for GetUserProfileQuery following CQRS and DRY principles
/// </summary>
public class GetUserProfileQueryValidator : AbstractValidator<GetUserProfileQuery>
{
    public GetUserProfileQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required")
            .NotEqual(Guid.Empty).WithMessage("User ID must be a valid GUID");
    }
}
