using MiniMart.BusinessLogic.Exceptions;

namespace MiniMart.BusinessLogic.Models;

public class User
{
    public const int UsernameMaxLength = 50;

    public const int MinimumPasswordLength = 6;

    private string _username = string.Empty;
    private string _passwordHash = string.Empty;

    public int UserId { get; private set; }

    public string Username
    {
        get => _username;
        private set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new BusinessRuleException("Username is required.");
            }

            var trimmed = value.Trim();
            if (trimmed.Length > UsernameMaxLength)
            {
                throw new BusinessRuleException($"Username cannot exceed {UsernameMaxLength} characters.");
            }

            _username = trimmed;
        }
    }

    public string PasswordHash
    {
        get => _passwordHash;
        private set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new BusinessRuleException("Password hash is required.");
            }

            _passwordHash = value;
        }
    }

    public UserRole Role { get; private set; }

    public bool IsActive { get; private set; } = true;

    public DateTime CreatedAt { get; private set; } = DateTime.Now;

    public bool IsAdmin => Role == UserRole.Admin;

    public User(string username, string passwordHash, UserRole role, bool isActive = true)
    {
        Username = username;
        PasswordHash = passwordHash;
        Role = role;
        IsActive = isActive;
    }

    public static User FromDatabase(
        int userId,
        string username,
        string passwordHash,
        string role,
        bool isActive,
        DateTime createdAt)
    {
        return new User(username, passwordHash, ParseRole(role), isActive)
        {
            UserId = userId,
            CreatedAt = createdAt
        };
    }

    public static UserRole ParseRole(string role)
    {
        if (Enum.TryParse<UserRole>(role, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed))
        {
            return parsed;
        }

        throw new BusinessRuleException($"'{role}' is not a recognised user role.");
    }

    public void SetPasswordHash(string passwordHash) => PasswordHash = passwordHash;

    public void ChangeUsername(string username) => Username = username;

    public void ChangeRole(UserRole role) => Role = role;

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

    public void AssignIdentity(int userId) => UserId = userId;

    public override string ToString() => Username;
}
