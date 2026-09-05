using MiniMart.BusinessLogic.Exceptions;
using MiniMart.BusinessLogic.Models;
using MiniMart.BusinessLogic.Repositories;
using MiniMart.BusinessLogic.Security;

namespace MiniMart.BusinessLogic.Services;

public class AuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public AuthService(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
    }

    public async Task<User> AuthenticateAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new BusinessRuleException("Please enter your username.");
        }

        if (string.IsNullOrEmpty(password))
        {
            throw new BusinessRuleException("Please enter your password.");
        }

        var user = await _userRepository
            .GetByUsernameAsync(username.Trim(), cancellationToken)
            .ConfigureAwait(false);

        if (user is null || !_passwordHasher.Verify(password, user.PasswordHash))
        {
            throw new BusinessRuleException("Invalid username or password.");
        }

        if (!user.IsActive)
        {
            throw new BusinessRuleException("This account has been deactivated. Please contact an administrator.");
        }

        return user;
    }

    public async Task<bool> AnyUsersExistAsync(CancellationToken cancellationToken = default)
    {
        var users = await _userRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return users.Count > 0;
    }
}
