using FluentValidation;


namespace GameGuild.Modules.TestingLab;

public class CreateTestingSessionCommandValidator : AbstractValidator<CreateTestingSessionCommand> {
  public CreateTestingSessionCommandValidator() {
    RuleFor(x => x.TestingRequestId)
      .NotEmpty()
      .WithMessage("Testing Request ID is required.");

    RuleFor(x => x.Title)
      .NotEmpty()
      .MaximumLength(255)
      .WithMessage("Title is required and must be less than 255 characters.");

    RuleFor(x => x.ScheduledDate)
      .NotEmpty()
      .GreaterThan(DateTime.UtcNow)
      .WithMessage("Scheduled date must be in the future.");

    RuleFor(x => x.Duration)
      .NotEmpty()
      .GreaterThan(TimeSpan.Zero)
      .LessThanOrEqualTo(TimeSpan.FromHours(8))
      .WithMessage("Duration must be between 0 and 8 hours.");

    RuleFor(x => x.MaxParticipants)
      .GreaterThan(0)
      .LessThanOrEqualTo(100)
      .WithMessage("Max participants must be between 1 and 100.");

    RuleFor(x => x.LocationId)
      .NotEmpty()
      .When(x => x.Mode == TestingMode.InPerson || x.Mode == TestingMode.Hybrid)
      .WithMessage("Location ID is required for in-person or hybrid sessions.");
  }
}
