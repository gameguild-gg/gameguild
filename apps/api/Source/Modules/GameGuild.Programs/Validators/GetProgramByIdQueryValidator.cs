using FluentValidation;
using GameGuild.Modules.Programs.Queries;

namespace GameGuild.Modules.Programs.Validators;

/// <summary> Validator for GetProgramByIdQuery </summary>
public class GetProgramByIdQueryValidator : AbstractValidator<GetProgramByIdQuery> {
    public GetProgramByIdQueryValidator() {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Program ID is required");
    }
}