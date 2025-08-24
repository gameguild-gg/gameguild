using FluentValidation;


namespace GameGuild.Modules.TestingLab.Validators;

public class SubmitFeedbackCommandValidator : AbstractValidator<SubmitFeedbackCommand> {
  public SubmitFeedbackCommandValidator() {
    RuleFor(x => x.TestingRequestId)
      .NotEmpty()
      .WithMessage("Testing Request ID is required.");

    RuleFor(x => x.UserId)
      .NotEmpty()
      .WithMessage("User ID is required.");

    RuleFor(x => x.Content)
      .NotEmpty()
      .MaximumLength(5000)
      .WithMessage("Feedback content is required and must be less than 5000 characters.");

    RuleFor(x => x.Rating)
      .InclusiveBetween(1, 10)
      .When(x => x.Rating.HasValue)
      .WithMessage("Rating must be between 1 and 10.");
  }
}
