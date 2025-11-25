using FluentValidation;
using GameGuild.Modules.Programs.Commands;

namespace GameGuild.Modules.Programs.Validators;

/// <summary> Validator for RemoveFromWishlistCommand </summary>
public class RemoveFromWishlistCommandValidator : AbstractValidator<RemoveFromWishlistCommand> {
    public RemoveFromWishlistCommandValidator() {
        RuleFor(x => x.ProgramId)
            .NotEmpty().WithMessage("Program ID is required");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");
    }
}