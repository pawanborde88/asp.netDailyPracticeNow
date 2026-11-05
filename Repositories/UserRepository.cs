using Microsoft.EntityFrameworkCore;
using dailypracticeASPDotNet.Data;
using dailypracticeASPDotNet.Models.Domain;

namespace dailypracticeASPDotNet.Repositories
{
    /// <summary>
    /// Entity Framework Core implementation of IUserRepository.
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
            // Asynchronously queries user by email including their Role
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.EmailId.ToLower() == normalizedEmail);
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            // Asynchronously queries user by primary key including their Role
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<User> CreateAsync(User user)
        {
            // Asynchronously adds entity and commits transaction to the database
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Load the associated role for the created user
            await _context.Entry(user).Reference(u => u.Role).LoadAsync();
            return user;
        }

        public async Task<bool> ExistsByEmailAsync(string email)
        {
            var normalizedEmail = email.Trim().ToLower();
            // Asynchronously checks if user with this email already exists
            return await _context.Users
                .AnyAsync(u => u.EmailId.ToLower() == normalizedEmail);
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            // Asynchronously retrieves all users with their roles
            return await _context.Users
                .Include(u => u.Role)
                .AsNoTracking()
                .OrderByDescending(u => u.CreatedAt)
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
