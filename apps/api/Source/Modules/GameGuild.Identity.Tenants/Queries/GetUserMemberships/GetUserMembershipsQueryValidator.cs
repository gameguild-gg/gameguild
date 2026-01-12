using FluentValidation;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Validator for GetUserMembershipsQuery
/// </summary>
public sealed class GetUserMembershipsQueryValidator : AbstractValidator<GetUserMembershipsQuery>
{
    public GetUserMembershipsQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");
    }
}
