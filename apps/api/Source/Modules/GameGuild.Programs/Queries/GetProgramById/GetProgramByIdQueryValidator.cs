using FluentValidation;

namespace GameGuild.Modules.Programs.Queries;

public class GetProgramByIdQueryValidator : AbstractValidator<GetProgramByIdQuery> {
    public GetProgramByIdQueryValidator() {
        RuleFor(x => x.Id)
          .NotEmpty().WithMessage("Program ID is required");
    }
}
