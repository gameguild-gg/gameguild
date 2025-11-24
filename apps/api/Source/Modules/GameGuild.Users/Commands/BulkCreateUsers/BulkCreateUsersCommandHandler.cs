using GameGuild.CQRS;
using GameGuild.Users.Abstractions;
using GameGuild.Users.Entities;

namespace GameGuild.Users.Commands;

/// <summary>
///     Command handler for bulk creating users
/// </summary>
public class BulkCreateUsersCommandHandler(IUserRepository userRepository) : ICommandHandler<BulkCreateUsersCommand, BulkCreateUsersResult>
{
    public async Task<BulkCreateUsersResult> Handle(BulkCreateUsersCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var createdUserIds = new List<Guid>();
        var failedEmails = new List<string>();
        var usersToCreate = new List<User>();

        // Validate all emails don't already exist
        var emails = request.Users.Select(u => u.Email).ToList();
        var existingUsers = await userRepository.GetByEmailsAsync(emails, cancellationToken).ConfigureAwait(false);
        var existingEmails = existingUsers.Select(u => u.Email).ToHashSet();

        foreach (var userRequest in request.Users)
        {
            if (existingEmails.Contains(userRequest.Email))
            {
                failedEmails.Add(userRequest.Email);

                continue;
            }

            try
            {
                // Create new user
                var user = User.Create(userRequest.Email, userRequest.Name, userRequest.PhoneNumber);
                usersToCreate.Add(user);
                createdUserIds.Add(user.Id);
            }
            catch { failedEmails.Add(userRequest.Email); }
        }

        // Add all users to repository
        foreach (var user in usersToCreate) { await userRepository.AddAsync(user, cancellationToken).ConfigureAwait(false); }

        await userRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new BulkCreateUsersResult(createdUserIds, failedEmails);
    }
}
