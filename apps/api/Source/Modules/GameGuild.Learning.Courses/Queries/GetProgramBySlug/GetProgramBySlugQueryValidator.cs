using FluentValidation;

namespace GameGuild.Learning.Courses;

public class GetProgramBySlugQueryValidator : AbstractValidator<GetProgramBySlugQuery> {
    public GetProgramBySlugQueryValidator() {
        RuleFor(x => x.Slug)
          .NotEmpty().WithMessage("Program slug is required")
          .Length(3, 100).WithMessage("Program slug must be between 3 and 100 characters")
          .Matches(@"^[a-z0-9-]+$").WithMessage("Program slug must contain only lowercase letters, numbers, and hyphens");
    }
}
