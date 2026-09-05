using MiniMart.BusinessLogic.Exceptions;
using MiniMart.BusinessLogic.Models;
using MiniMart.BusinessLogic.Repositories;
using MiniMart.BusinessLogic.Security;

namespace MiniMart.BusinessLogic.Services;

public class UserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public UserService(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
    }

    public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _userRepository.GetAllAsync(cancellationToken);

    public Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default) =>
        _userRepository.GetByIdAsync(userId, cancellationToken);

    public async Task<User> CreateAsync(
        string username,
        string password,
        UserRole role,
        CancellationToken cancellationToken = default)
    {
        ValidatePassword(password);

        var user = new User(username, _passwordHasher.Hash(password), role);

        if (await _userRepository.UsernameExistsAsync(user.Username, 0, cancellationToken).ConfigureAwait(false))
        {
            throw new BusinessRuleException($"The username '{user.Username}' is already taken.");
        }

        var id = await _userRepository.AddAsync(user, cancellationToken).ConfigureAwait(false);
        user.AssignIdentity(id);
        return user;
    }

    public async Task UpdateAsync(
        int userId,
        string username,
        UserRole role,
        bool isActive,
        string? newPassword = null,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false)
            ?? throw new BusinessRuleException("That user account no longer exists.");

        var losingAdminAccess = user.IsAdmin && user.IsActive && (role != UserRole.Admin || !isActive);
        if (losingAdminAccess)
        {
            await EnsureAnotherActiveAdminExistsAsync(cancellationToken).ConfigureAwait(false);
        }

        user.ChangeUsername(username);
        user.ChangeRole(role);

        if (isActive)
        {
            user.Activate();
        }
        else
        {
            user.Deactivate();
        }

        if (!string.IsNullOrWhiteSpace(newPassword))
        {
            ValidatePassword(newPassword);
            user.SetPasswordHash(_passwordHasher.Hash(newPassword));
        }

        if (await _userRepository.UsernameExistsAsync(user.Username, userId, cancellationToken).ConfigureAwait(false))
        {
            throw new BusinessRuleException($"The username '{user.Username}' is already taken.");
        }

        await _userRepository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> DeleteAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false)
            ?? throw new BusinessRuleException("That user account no longer exists.");

        if (user.IsAdmin && user.IsActive)
        {
            await EnsureAnotherActiveAdminExistsAsync(cancellationToken).ConfigureAwait(false);
        }

        if (await _userRepository.HasSalesHistoryAsync(userId, cancellationToken).ConfigureAwait(false))
        {
            user.Deactivate();
            await _userRepository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
            return false;
        }

        await _userRepository.DeleteAsync(userId, cancellationToken).ConfigureAwait(false);
        return true;
    }

    private static void ValidatePassword(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new BusinessRuleException("A password is required.");
        }

        if (password.Length < User.MinimumPasswordLength)
        {
            throw new BusinessRuleException(
                $"Password must be at least {User.MinimumPasswordLength} characters long.");
        }
    }

    private async Task EnsureAnotherActiveAdminExistsAsync(CancellationToken cancellationToken)
    {
        var activeAdmins = await _userRepository
            .CountActiveAdminsAsync(cancellationToken)
            .ConfigureAwait(false);

        if (activeAdmins <= 1)
        {
            throw new BusinessRuleException(
                "This is the only active administrator account. Create another administrator before " +
                "changing or removing this one.");
        }
    }
}
