using FluentValidation;

namespace GameGuild.Programs;

/// <summary> Validator for UnpublishProgramCommand </summary>
public class UnpublishProgramCommandValidator : AbstractValidator<UnpublishProgramCommand> {
    public UnpublishProgramCommandValidator() {
        RuleFor(x => x.Id)
          .NotEmpty().WithMessage("Program ID is required");
    }
}
