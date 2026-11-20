using Microsoft.EntityFrameworkCore;
using dailypracticeASPDotNet.Data;
using dailypracticeASPDotNet.Models.Domain;

namespace dailypracticeASPDotNet.Repositories
{
    /// <summary>
    /// Entity Framework Core implementation of IUserRepository supporting numeric digit user IDs.
    /// Incorporates async/await pattern and eager loading of related entities.
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;

        public UserRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            var normalizedEmail = email.Trim().ToLower();
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.EmailId.ToLower() == normalizedEmail);
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<User> CreateAsync(User user)
        {
            // Database automatically generates the numeric integer ID upon insertion
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Load associated role
            await _context.Entry(user).Reference(u => u.Role).LoadAsync();
            return user;
        }

        public async Task<User?> UpdateAsync(int id, User user)
        {
            var existingUser = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (existingUser == null)
            {
                return null;
            }

            // Update scalar properties
            existingUser.FirstName = user.FirstName;
            existingUser.MiddleName = user.MiddleName;
            existingUser.LastName = user.LastName;
            existingUser.EmailId = user.EmailId;
            existingUser.MobileNo = user.MobileNo;
            existingUser.Address = user.Address;
            existingUser.RoleId = user.RoleId;

            // Update photo binary data if provided
            if (user.PhotoData != null && user.PhotoData.Length > 0)
            {
                existingUser.PhotoData = user.PhotoData;
                existingUser.PhotoContentType = user.PhotoContentType;
                existingUser.PhotoFileName = user.PhotoFileName;
                existingUser.PhotoUrl = user.PhotoUrl;
            }

            // Update password hash only if a new non-empty hash was assigned
            if (!string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                existingUser.PasswordHash = user.PasswordHash;
            }

            await _context.SaveChangesAsync();

            // Reload associated role in case RoleId was updated
            await _context.Entry(existingUser).Reference(u => u.Role).LoadAsync();

            return existingUser;
        }

        public async Task<User?> UpdatePhotoAsync(int id, byte[]? photoData, string? contentType, string? fileName, string? photoUrl)
        {
            var existingUser = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (existingUser == null)
            {
                return null;
            }

            existingUser.PhotoData = photoData;
            existingUser.PhotoContentType = contentType;
            existingUser.PhotoFileName = fileName;
            existingUser.PhotoUrl = photoUrl;

            await _context.SaveChangesAsync();
            return existingUser;
        }

        public async Task<User?> DeleteAsync(int id)
        {
            var existingUser = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (existingUser == null)
            {
                return null;
            }

            _context.Users.Remove(existingUser);
            await _context.SaveChangesAsync();

            return existingUser;
        }

        public async Task<bool> ExistsByEmailAsync(string email)
        {
            var normalizedEmail = email.Trim().ToLower();
            return await _context.Users
                .AnyAsync(u => u.EmailId.ToLower() == normalizedEmail);
        }

        public async Task<bool> ExistsByEmailExcludingUserAsync(string email, int userId)
        {
            var normalizedEmail = email.Trim().ToLower();
            return await _context.Users
                .AnyAsync(u => u.EmailId.ToLower() == normalizedEmail && u.Id != userId);
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _context.Users
                .Include(u => u.Role)
                .AsNoTracking()
                .OrderBy(u => u.Id)
                .ToListAsync();
        }

        public async Task<Role?> GetRoleByIdAsync(int roleId)
        {
            return await _context.Roles.FindAsync(roleId);
        }

        public async Task<IEnumerable<Role>> GetAllRolesAsync()
        {
            return await _context.Roles
                .AsNoTracking()
                .OrderBy(r => r.Id)
                .ToListAsync();
        }
    }
}
