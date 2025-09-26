namespace GameGuild.Modules.Users;

public class UserService(IUserRepository userRepository) : IUserService
{
    private readonly IUserRepository _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));

    public async Task<IEnumerable<User>> GetAllUsersAsync() { return await _userRepository.GetAllAsync(); }

    public async Task<User?> GetUserByIdAsync(Guid id) { return await _userRepository.GetByIdAsync(id); }

    public async Task<User?> GetByEmailAsync(string email) { return await _userRepository.GetByEmailAsync(email); }

    public async Task<User> CreateUserAsync(User user)
    {
        // Check if email already exists
        var existingUser = await GetByEmailAsync(user.Email);

        if (existingUser != null) throw new InvalidOperationException($"A user with email '{user.Email}' already exists.");

        return await _userRepository.AddAsync(user);
    }

    public async Task<User> CreateUserAsync(string name, string email, bool isActive = true, CancellationToken cancellationToken = default)
    {
        // Check if email already exists
        var existingUser = await GetByEmailAsync(email);

        if (existingUser != null) throw new InvalidOperationException($"A user with email '{email}' already exists.");

        // Generate unique username from name using slugify
        var baseUsername = name.ToSlugCase();
        var existingUsernames = await _userRepository.GetUsernamesStartingWithAsync(baseUsername, cancellationToken);

        var uniqueUsername = SlugCase.GenerateUnique(name, existingUsernames, 50);

        var user = new User { Name = name, Username = uniqueUsername, Email = email, IsActive = isActive };

        return await _userRepository.AddAsync(user);
    }

    public async Task<User?> UpdateUserAsync(Guid id, User user)
    {
        var existingUser = await _userRepository.GetByIdAsync(id);

        if (existingUser == null) return null;

        existingUser.Name = user.Name;
        existingUser.Email = user.Email;
        existingUser.IsActive = user.IsActive;

        return await _userRepository.UpdateAsync(existingUser);
    }

    public async Task<bool> DeleteUserAsync(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id, includeDeleted : true);

        if (user == null) return false;

        await _userRepository.RemoveAsync(id);

        return true;
    }

    public async Task<bool> SoftDeleteUserAsync(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id);

        if (user == null) return false;

        await _userRepository.SoftDeleteAsync(id);

        return true;
    }

    public async Task<bool> RestoreUserAsync(Guid id)
    {
        var deletedUsers = await _userRepository.GetDeletedAsync();
        var user = deletedUsers.FirstOrDefault(u => u.Id == id);

        if (user == null) return false;

        await _userRepository.RestoreAsync(id);

        return true;
    }

    public async Task<IEnumerable<User>> GetDeletedUsersAsync() { return await _userRepository.GetDeletedAsync(); }
}
