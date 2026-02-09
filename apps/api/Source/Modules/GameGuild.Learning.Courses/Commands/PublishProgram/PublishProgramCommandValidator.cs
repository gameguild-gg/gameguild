using FluentValidation;

namespace GameGuild.Learning.Courses;

/// <summary> Validator for PublishProgramCommand </summary>
public sealed class PublishProgramCommandValidator : AbstractValidator<PublishProgramCommand> {
    public PublishProgramCommandValidator() {
        RuleFor(x => x.Id)
          .NotEmpty().WithMessage("Program ID is required");
    }
}
