using FluentValidation;

namespace GameGuild.Identity.Users;

/// <summary>
///     Validator for GetUserByIdQuery
/// </summary>
public class GetUserByIdQueryValidator : AbstractValidator<GetUserByIdQuery>
{
    public GetUserByIdQueryValidator() { RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required."); }
}
