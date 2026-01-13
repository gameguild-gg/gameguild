using FluentValidation;

namespace GameGuild.Programs;

/// <summary> Validator for PublishProgramCommand </summary>
public class PublishProgramCommandValidator : AbstractValidator<PublishProgramCommand> {
    public PublishProgramCommandValidator() {
        RuleFor(x => x.Id)
          .NotEmpty().WithMessage("Program ID is required");
    }
}
