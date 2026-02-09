using FluentValidation;

namespace GameGuild.Learning.Courses;

public sealed class GetProgramByIdQueryValidator : AbstractValidator<GetProgramByIdQuery> {
    public GetProgramByIdQueryValidator() {
        RuleFor(x => x.Id)
          .NotEmpty().WithMessage("Program ID is required");
    }
}
