using FluentValidation;

namespace GameGuild.Programs;

/// <summary> Validator for RemoveFromWishlistCommand </summary>
public class RemoveFromWishlistCommandValidator : AbstractValidator<RemoveFromWishlistCommand> {
    public RemoveFromWishlistCommandValidator() {
        RuleFor(x => x.ProgramId)
          .NotEmpty().WithMessage("Program ID is required");

        RuleFor(x => x.UserId)
          .NotEmpty().WithMessage("User ID is required");
    }
}
