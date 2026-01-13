using FluentValidation;

namespace GameGuild.Programs;

public class GetProgramByIdQueryValidator : AbstractValidator<GetProgramByIdQuery> {
    public GetProgramByIdQueryValidator() {
        RuleFor(x => x.Id)
          .NotEmpty().WithMessage("Program ID is required");
    }
}
