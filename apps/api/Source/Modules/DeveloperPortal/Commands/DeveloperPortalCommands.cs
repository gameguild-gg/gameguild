using GameGuild.CQRS;
using MediatR;
using GameGuild.Common;
using System.ComponentModel.DataAnnotations;

namespace GameGuild.Modules.DeveloperPortal.Commands;

public record GenerateApiKeyCommand(
    [Required] Guid DeveloperId,
    [Required][MaxLength(200)] string Name,
    Guid? TenantId,
    List<string>? Scopes,
    DateTime? ExpiresAt
) : IRequest<Result<Guid>>;

public record RevokeApiKeyCommand(
    [Required] Guid ApiKeyId
) : IRequest<Result<Unit>>;

public record RotateApiKeyCommand(
    [Required] Guid ApiKeyId
) : IRequest<Result<Guid>>;

public record LogApiUsageCommand(
    [Required] Guid ApiKeyId,
    [Required][MaxLength(500)] string Endpoint,
    [Required][MaxLength(10)] string Method,
    [Required] int StatusCode,
    [Required] long ResponseTimeMs
) : IRequest<Result<Unit>>;

public record StartOnboardingCommand(
    [Required] Guid DeveloperId,
    Guid? TenantId
) : IRequest<Result<Guid>>;

public record CompleteOnboardingCommand(
    [Required] Guid DeveloperId
) : IRequest<Result<Unit>>;

public record UpdateOnboardingProgressCommand(
    [Required] Guid DeveloperId,
    [Required][MaxLength(100)] string StepKey,
    [Required] bool Completed
) : IRequest<Result<Unit>>;
