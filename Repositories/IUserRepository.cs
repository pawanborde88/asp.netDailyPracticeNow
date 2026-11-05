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
        Task<bool> ExistsByEmailAsync(string email);
        Task<IEnumerable<User>> GetAllAsync();
        Task<Role?> GetRoleByIdAsync(int roleId);
        Task<IEnumerable<Role>> GetAllRolesAsync();
    }
}
