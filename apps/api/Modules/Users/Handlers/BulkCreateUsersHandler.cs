using GameGuild.CQRS;
using GameGuild.Database;

namespace GameGuild.Modules.Users;

/// <summary> Handler for bulk creating users </summary>
public class BulkCreateUsersHandler(ApplicationDbContext context, ILogger<BulkCreateUsersHandler> logger, IMediator mediator) : IResultCommandHandler<BulkCreateUsersCommand, BulkOperationResult>
{
    public async Task<Result<BulkOperationResult>> Handle(BulkCreateUsersCommand request, CancellationToken cancellationToken)
    {
        var createdUsers = new List<User>();
        var errors = new List<string>();
        var successfulCount = 0;

        foreach (CreateUserRequest userDto in request.Users)
        {
            try
            {
                // Check if user with email already exists
                string normalizedEmail = userDto.Email.ToLowerInvariant();
                User? existingUser = await context.Users.FirstOrDefaultAsync(u => u.EmailAddress != null && u.EmailAddress.Value == normalizedEmail, cancellationToken);

                if (existingUser != null)
                {
                    errors.Add($"User with email {userDto.Email} already exists");

                    continue;
                }

                // Generate unique username from name using slugify
                string fullName = $"{userDto.GivenName ?? ""} {userDto.FamilyName ?? ""}".Trim();
                string baseUsername = string.IsNullOrWhiteSpace(fullName) ? userDto.Email.Split('@')[0] : fullName.ToSlugCase();
                var existingUsernames = await context.Users.Where(u => u.Username.StartsWith(baseUsername)).Select(u => u.Username).ToListAsync(cancellationToken);

                string uniqueUsername = SlugCase.GenerateUnique(string.IsNullOrWhiteSpace(fullName) ? userDto.Email : fullName, existingUsernames, 50);

                var user = new User { GivenName = userDto.GivenName, FamilyName = userDto.FamilyName, Username = uniqueUsername, Email = userDto.Email, IsActive = userDto.IsActive };

                context.Users.Add(user);
                createdUsers.Add(user);
                successfulCount++;
            }
            catch (Exception ex)
            {
                errors.Add($"Failed to create user with email {userDto.Email}: {ex.Message}");
                logger.LogError(ex, "Failed to create user with email {Email}", userDto.Email);
            }
        }

        if (createdUsers.Count != 0)
        {
            await context.SaveChangesAsync(cancellationToken);

            // Publish domain events for created users
            foreach (User user in createdUsers) await mediator.Publish(new UserCreatedEvent(user.Id, user.Email, user.GivenName, user.FamilyName, user.CreatedAt), cancellationToken);
        }

        var result = new BulkOperationResult(request.Users.Count, successfulCount, errors.Count);

        foreach (string error in errors) result.AddError(error);

        logger.LogInformation("Bulk create completed: {Successful}/{Total} users created. Reason: {Reason}", successfulCount, request.Users.Count, request.Reason ?? "Not specified");

        return Result.Success(result);
    }
}
