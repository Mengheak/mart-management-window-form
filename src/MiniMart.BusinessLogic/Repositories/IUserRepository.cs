using MiniMart.BusinessLogic.Models;

namespace MiniMart.BusinessLogic.Repositories;

public interface IUserRepository
{
    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default);

    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

    Task<int> AddAsync(User user, CancellationToken cancellationToken = default);

    Task<int> UpdateAsync(User user, CancellationToken cancellationToken = default);

    Task<int> DeleteAsync(int userId, CancellationToken cancellationToken = default);

    Task<bool> UsernameExistsAsync(
        string username,
        int excludeUserId = 0,
        CancellationToken cancellationToken = default);

    Task<int> CountActiveAdminsAsync(CancellationToken cancellationToken = default);

    Task<bool> HasSalesHistoryAsync(int userId, CancellationToken cancellationToken = default);
}
