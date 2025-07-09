using MediatR;
using Microsoft.AspNetCore.Mvc;
using GameGuild.Modules.Users.Commands;
using GameGuild.Modules.Users.Queries;
using GameGuild.Modules.Users.Dtos;
using Web.Api.Endpoints;
using SharedKernel;

namespace Web.Api.Endpoints.Users;

/// <summary>
/// Users endpoints using Clean Architecture pattern with Results
/// </summary>
public class UsersEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users")
            .WithTags("Users")
            .WithOpenApi();

        group.MapGet("/", GetAllUsers)
            .WithName("GetAllUsers")
            .WithSummary("Get all users with pagination and filtering")
            .Produces<IEnumerable<UserResponseDto>>();

        group.MapGet("/{id:guid}", GetUserById)
            .WithName("GetUserById")
            .WithSummary("Get user by ID")
            .Produces<UserResponseDto>()
            .Produces(404);

        group.MapPost("/", CreateUser)
            .WithName("CreateUser")
            .WithSummary("Create a new user")
            .Produces<UserResponseDto>(201)
            .Produces<ValidationProblemDetails>(400);

        group.MapPut("/{id:guid}", UpdateUser)
            .WithName("UpdateUser")
            .WithSummary("Update user information")
            .Produces<UserResponseDto>()
            .Produces(404)
            .Produces(409);

        group.MapDelete("/{id:guid}", DeleteUser)
            .WithName("DeleteUser")
            .WithSummary("Delete user (soft delete by default)")
            .Produces(204)
            .Produces(404);

        group.MapPost("/{id:guid}/restore", RestoreUser)
            .WithName("RestoreUser")
            .WithSummary("Restore a soft-deleted user")
            .Produces(204)
            .Produces(404);
    }

    private static async Task<Results<Ok<IEnumerable<UserResponseDto>>, BadRequest<string>>> GetAllUsers(
        [FromQuery] bool includeDeleted,
        [FromQuery] bool? isActive,
        [FromQuery] int skip,
        [FromQuery] int take,
        [FromServices] IMediator mediator)
    {
        var query = new GetAllUsersQuery
        {
            IncludeDeleted = includeDeleted,
            IsActive = isActive,
            Skip = skip,
            Take = Math.Min(take, 100) // Limit to prevent abuse
        };

        try
        {
            var users = await mediator.Send(query);
            var userDtos = users.Select(u => new UserResponseDto
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email,
                IsActive = u.IsActive,
                Balance = u.Balance,
                AvailableBalance = u.AvailableBalance,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt,
                Version = u.Version
            });

            return TypedResults.Ok(userDtos);
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest($"Error retrieving users: {ex.Message}");
        }
    }

    private static async Task<Results<Ok<UserResponseDto>, NotFound>> GetUserById(
        Guid id,
        [FromQuery] bool includeDeleted,
        [FromServices] IMediator mediator)
    {
        var query = new GetUserByIdQuery
        {
            UserId = id,
            IncludeDeleted = includeDeleted
        };

        var user = await mediator.Send(query);
        if (user == null)
        {
            return TypedResults.NotFound();
        }

        var userDto = new UserResponseDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            IsActive = user.IsActive,
            Balance = user.Balance,
            AvailableBalance = user.AvailableBalance,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            Version = user.Version
        };

        return TypedResults.Ok(userDto);
    }

    private static async Task<Results<Created<UserResponseDto>, BadRequest<string>, ValidationProblem>> CreateUser(
        [FromBody] CreateUserDto createDto,
        [FromServices] IMediator mediator)
    {
        var command = new CreateUserCommand
        {
            Name = createDto.Name,
            Email = createDto.Email,
            IsActive = createDto.IsActive,
            InitialBalance = createDto.InitialBalance
        };

        try
        {
            var user = await mediator.Send(command);
            var userDto = new UserResponseDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                IsActive = user.IsActive,
                Balance = user.Balance,
                AvailableBalance = user.AvailableBalance,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                Version = user.Version
            };

            return TypedResults.Created($"/api/users/{user.Id}", userDto);
        }
        catch (ArgumentException ex)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["validation"] = [ex.Message]
            });
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }
    }

    private static async Task<Results<Ok<UserResponseDto>, NotFound, Conflict<string>>> UpdateUser(
        Guid id,
        [FromBody] UpdateUserDto updateDto,
        [FromHeader(Name = "If-Match")] int? ifMatch,
        [FromServices] IMediator mediator)
    {
        var command = new UpdateUserCommand
        {
            UserId = id,
            Name = updateDto.Name,
            Email = updateDto.Email,
            IsActive = updateDto.IsActive,
            ExpectedVersion = ifMatch
        };

        try
        {
            var user = await mediator.Send(command);
            var userDto = new UserResponseDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                IsActive = user.IsActive,
                Balance = user.Balance,
                AvailableBalance = user.AvailableBalance,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                Version = user.Version
            };

            return TypedResults.Ok(userDto);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Concurrency conflict"))
        {
            return TypedResults.Conflict(ex.Message);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return TypedResults.NotFound();
        }
    }

    private static async Task<Results<NoContent, NotFound>> DeleteUser(
        Guid id,
        [FromQuery] bool permanent,
        [FromServices] IMediator mediator)
    {
        var command = new DeleteUserCommand
        {
            UserId = id,
            SoftDelete = !permanent
        };

        var result = await mediator.Send(command);
        return result ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    private static async Task<Results<NoContent, NotFound>> RestoreUser(
        Guid id,
        [FromServices] IMediator mediator)
    {
        var command = new RestoreUserCommand
        {
            UserId = id
        };

        var result = await mediator.Send(command);
        return result ? TypedResults.NoContent() : TypedResults.NotFound();
    }
}
