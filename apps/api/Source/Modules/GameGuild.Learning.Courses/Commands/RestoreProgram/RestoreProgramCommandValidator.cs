using FluentValidation;

namespace GameGuild.Learning.Courses;

/// <summary> Validator for RestoreProgramCommand </summary>
public sealed class RestoreProgramCommandValidator : AbstractValidator<RestoreProgramCommand> {
    public RestoreProgramCommandValidator() {
        RuleFor(x => x.Id)
          .NotEmpty().WithMessage("Program ID is required");
    }
}
