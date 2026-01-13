using FluentValidation;

namespace GameGuild.Learning.Courses;

/// <summary> Validator for AddToWishlistCommand </summary>
public class AddToWishlistCommandValidator : AbstractValidator<AddToWishlistCommand> {
    public AddToWishlistCommandValidator() {
        RuleFor(x => x.ProgramId)
          .NotEmpty().WithMessage("Program ID is required");

        RuleFor(x => x.UserId)
          .NotEmpty().WithMessage("User ID is required");
    }
}
