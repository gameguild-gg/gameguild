using FluentValidation;

namespace GameGuild.Learning.Courses;

public sealed class RecordContentInteractionEventCommandValidator
    : AbstractValidator<RecordContentInteractionEventCommand>
{
    public RecordContentInteractionEventCommandValidator()
    {
        RuleFor(command => command.ProgramId).NotEmpty();
        RuleFor(command => command.InteractionId).NotEmpty();
        RuleFor(command => command.Type)
            .IsInEnum()
            .WithMessage("Interaction event type is invalid.");
        RuleFor(command => command.DurationSeconds)
            .GreaterThan(0)
            .When(command => command.DurationSeconds.HasValue);
        RuleFor(command => command.DurationSeconds)
            .NotNull()
            .When(command => command.Type == ContentInteractionEventType.Heartbeat)
            .WithMessage("Heartbeat events require a duration in seconds.");
        RuleFor(command => command.PositionSeconds)
            .GreaterThanOrEqualTo(0)
            .When(command => command.PositionSeconds.HasValue);
        RuleFor(command => command.ProgressPercentage)
            .InclusiveBetween(0, 100)
            .When(command => command.ProgressPercentage.HasValue);
        RuleFor(command => command.IdempotencyKey).MaximumLength(128);
    }
}
