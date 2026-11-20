using dailypracticeASPDotNet.Models.Domain;

namespace dailypracticeASPDotNet.Repositories
{
    /// <summary>
    /// User repository interface defining asynchronous operations for User data access with numeric integer IDs.
    /// Follows the Repository Pattern and enforces async/await methods.
    /// </summary>
    public interface IUserRepository
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByIdAsync(int id);
        Task<User> CreateAsync(User user);
        Task<User?> UpdateAsync(int id, User user);
        Task<User?> DeleteAsync(int id);
        Task<bool> ExistsByEmailAsync(string email);
        Task<bool> ExistsByEmailExcludingUserAsync(string email, int userId);
        Task<IEnumerable<User>> GetAllAsync();
        Task<Role?> GetRoleByIdAsync(int roleId);
        Task<IEnumerable<Role>> GetAllRolesAsync();
        Task<User?> UpdatePhotoAsync(int id, byte[]? photoData, string? contentType, string? fileName, string? photoUrl);
    }
}
