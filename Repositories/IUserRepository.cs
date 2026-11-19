using dailypracticeASPDotNet.Models.Domain;

namespace dailypracticeASPDotNet.Repositories
{
    /// <summary>
    /// User repository interface defining asynchronous operations for User data access.
    /// Follows the Repository Pattern and enforces async/await methods.
    /// </summary>
    public interface IUserRepository
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByIdAsync(Guid id);
        Task<User> CreateAsync(User user);
        Task<User?> UpdateAsync(Guid id, User user);
        Task<User?> DeleteAsync(Guid id);
        Task<bool> ExistsByEmailAsync(string email);
        Task<bool> ExistsByEmailExcludingUserAsync(string email, Guid userId);
        Task<IEnumerable<User>> GetAllAsync();
        Task<Role?> GetRoleByIdAsync(int roleId);
        Task<IEnumerable<Role>> GetAllRolesAsync();
    }
}
