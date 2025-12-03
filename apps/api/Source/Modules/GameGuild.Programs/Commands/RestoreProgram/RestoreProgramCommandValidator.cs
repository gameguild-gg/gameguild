using FluentValidation;

namespace GameGuild.Modules.Programs.Commands;

/// <summary> Validator for RestoreProgramCommand </summary>
public class RestoreProgramCommandValidator : AbstractValidator<RestoreProgramCommand> {
    public RestoreProgramCommandValidator() {
        RuleFor(x => x.Id)
          .NotEmpty().WithMessage("Program ID is required");
    }
}
