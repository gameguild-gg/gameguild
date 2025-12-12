using FluentValidation;
using GameGuild.Modules.Programs.Commands;

namespace GameGuild.Modules.Programs.Validators;

/// <summary> Validator for PublishProgramCommand </summary>
public class PublishProgramCommandValidator : AbstractValidator<PublishProgramCommand> {
    public PublishProgramCommandValidator() {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Program ID is required");
    }
}