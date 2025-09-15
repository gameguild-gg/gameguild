using GameGuild.CQRS;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Modules.Users;

/// <summary>
/// Enhanced User controller demonstrating Result<T> pattern usage
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class UsersResultController(GameGuild.CQRS.IMediator mediator, ILogger<UsersResultController> logger) : ControllerBase
{
    /// <summary>
    /// Creates a new user using Result<T> pattern
    /// </summary>
    /// <param name="command">User creation command</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created user or appropriate error response</returns>
    [HttpPost]
    [ProducesResponseType<User>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<User>> CreateUser([FromBody] CreateUserResultCommand command, CancellationToken cancellationToken)
    {
        logger.LogDebug("Creating user with email {Email}", command.Email);

        var result = await mediator.Send(command, cancellationToken);

        return result.Match(
            user =>
            {
                logger.LogInformation("Successfully created user {UserId} with email {Email}", user.Id, user.Email);
                return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
            }
        );
    }

    /// <summary>
    /// Gets a user by ID using Result<T> pattern
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User or appropriate error response</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType<User>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<User>> GetUser(int id, CancellationToken cancellationToken)
    {
        logger.LogDebug("Getting user with ID {UserId}", id);

        var query = new GetUserByIdResultQuery(id);
        var result = await mediator.Send(query, cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>
    /// Example of handling Result without value
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success or error response</returns>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DeleteUser(int id, CancellationToken cancellationToken)
    {
        logger.LogDebug("Deleting user with ID {UserId}", id);

        // This would be a DeleteUserResultCommand that returns Result (without value)
        // var command = new DeleteUserResultCommand(id);
        // var result = await mediator.Send(command, cancellationToken);
        // return result.ToActionResult();

        // For now, return NotImplemented
        return StatusCode(StatusCodes.Status501NotImplemented, new ProblemDetails
        {
            Status = StatusCodes.Status501NotImplemented,
            Title = "Not Implemented",
            Detail = "Delete user functionality is not yet implemented with Result pattern"
        });
    }
}
