using FluentValidation;

namespace GameGuild.Commerce.Orders;

/// <summary>
/// Validator for CreateOrderCommand
/// </summary>
public sealed class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.IdempotencyKey)
            .NotEmpty()
            .WithMessage("Idempotency key is required for duplicate prevention.")
            .MinimumLength(8)
            .WithMessage("Idempotency key must be at least 8 characters.")
            .MaximumLength(100)
            .WithMessage("Idempotency key cannot exceed 100 characters.")
            .Matches(@"^[A-Za-z0-9_\-]+$")
            .WithMessage("Idempotency key can only contain letters, numbers, hyphens, and underscores.");

    }
}
