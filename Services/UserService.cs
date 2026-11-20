using AutoMapper;
using Microsoft.EntityFrameworkCore;
using dailypracticeASPDotNet.DTOs;
using dailypracticeASPDotNet.DTOs.Common;
using dailypracticeASPDotNet.Extensions;
using dailypracticeASPDotNet.Models.Domain;
using dailypracticeASPDotNet.Repositories;

namespace dailypracticeASPDotNet.Services
{
    /// <summary>
    /// User service implementation responsible for building IQueryable,
    /// applying dynamic text search, role filtering, safe column sorting,
    /// and calling the centralized pagination extension.
    /// </summary>
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<UserService> _logger;

        // Whitelist of allowed sort columns mapped to strongly-typed Linq expressions
        private static readonly Dictionary<string, Func<IQueryable<User>, bool, IQueryable<User>>> AllowedSortColumns =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["id"] = (q, asc) => asc ? q.OrderBy(u => u.Id) : q.OrderByDescending(u => u.Id),
                ["firstname"] = (q, asc) => asc ? q.OrderBy(u => u.FirstName) : q.OrderByDescending(u => u.FirstName),
                ["first_name"] = (q, asc) => asc ? q.OrderBy(u => u.FirstName) : q.OrderByDescending(u => u.FirstName),
                ["lastname"] = (q, asc) => asc ? q.OrderBy(u => u.LastName) : q.OrderByDescending(u => u.LastName),
                ["last_name"] = (q, asc) => asc ? q.OrderBy(u => u.LastName) : q.OrderByDescending(u => u.LastName),
                ["fullname"] = (q, asc) => asc ? q.OrderBy(u => u.FirstName).ThenBy(u => u.LastName) : q.OrderByDescending(u => u.FirstName).ThenByDescending(u => u.LastName),
                ["name"] = (q, asc) => asc ? q.OrderBy(u => u.FirstName).ThenBy(u => u.LastName) : q.OrderByDescending(u => u.FirstName).ThenByDescending(u => u.LastName),
                ["email"] = (q, asc) => asc ? q.OrderBy(u => u.EmailId) : q.OrderByDescending(u => u.EmailId),
                ["emailid"] = (q, asc) => asc ? q.OrderBy(u => u.EmailId) : q.OrderByDescending(u => u.EmailId),
                ["email_id"] = (q, asc) => asc ? q.OrderBy(u => u.EmailId) : q.OrderByDescending(u => u.EmailId),
                ["mobileno"] = (q, asc) => asc ? q.OrderBy(u => u.MobileNo) : q.OrderByDescending(u => u.MobileNo),
                ["mobile_no"] = (q, asc) => asc ? q.OrderBy(u => u.MobileNo) : q.OrderByDescending(u => u.MobileNo),
                ["roleid"] = (q, asc) => asc ? q.OrderBy(u => u.RoleId) : q.OrderByDescending(u => u.RoleId),
                ["role_id"] = (q, asc) => asc ? q.OrderBy(u => u.RoleId) : q.OrderByDescending(u => u.RoleId),
                ["role"] = (q, asc) => asc ? q.OrderBy(u => u.Role != null ? u.Role.Name : string.Empty) : q.OrderByDescending(u => u.Role != null ? u.Role.Name : string.Empty),
                ["rolename"] = (q, asc) => asc ? q.OrderBy(u => u.Role != null ? u.Role.Name : string.Empty) : q.OrderByDescending(u => u.Role != null ? u.Role.Name : string.Empty),
                ["role_name"] = (q, asc) => asc ? q.OrderBy(u => u.Role != null ? u.Role.Name : string.Empty) : q.OrderByDescending(u => u.Role != null ? u.Role.Name : string.Empty),
                ["createdat"] = (q, asc) => asc ? q.OrderBy(u => u.CreatedAt) : q.OrderByDescending(u => u.CreatedAt),
                ["created_at"] = (q, asc) => asc ? q.OrderBy(u => u.CreatedAt) : q.OrderByDescending(u => u.CreatedAt),
                ["createddate"] = (q, asc) => asc ? q.OrderBy(u => u.CreatedAt) : q.OrderByDescending(u => u.CreatedAt),
                ["date"] = (q, asc) => asc ? q.OrderBy(u => u.CreatedAt) : q.OrderByDescending(u => u.CreatedAt)
            };

        private static readonly Func<IQueryable<User>, bool, IQueryable<User>> DefaultSort =
            (q, asc) => asc ? q.OrderBy(u => u.Id) : q.OrderByDescending(u => u.Id);

        public UserService(
            IUserRepository userRepository,
            IMapper mapper,
            ILogger<UserService> logger)
        {
            _userRepository = userRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<PaginationResponseDto<UserDto>> GetPaginatedUsersAsync(
            UserPaginationRequestDto request,
            CancellationToken cancellationToken = default)
        {
            request ??= new UserPaginationRequestDto();

            // 1. Build initial unmaterialized IQueryable with AsNoTracking()
            var query = _userRepository.GetUsersQuery();

            // 2. Apply dynamic multi-field text search BEFORE pagination
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim().ToLower();
                query = query.Where(u =>
                    u.FirstName.ToLower().Contains(term) ||
                    (u.MiddleName != null && u.MiddleName.ToLower().Contains(term)) ||
                    u.LastName.ToLower().Contains(term) ||
                    u.EmailId.ToLower().Contains(term) ||
                    (u.MobileNo != null && u.MobileNo.Contains(term)) ||
                    (u.Address != null && u.Address.ToLower().Contains(term)) ||
                    (u.Role != null && u.Role.Name.ToLower().Contains(term)));
            }

            // 3. Apply multiple filtering conditions BEFORE pagination
            if (request.RoleId.HasValue && request.RoleId.Value > 0)
            {
                query = query.Where(u => u.RoleId == request.RoleId.Value);
            }

            // 4. Apply safe, SQL-injection-proof sorting BEFORE Skip() and Take()
            query = query.ApplySafeSort(
                request.SortBy,
                request.IsAscending,
                AllowedSortColumns,
                DefaultSort);

            // 5. Centralized pagination executes CountAsync, Skip, Take, and ToListAsync at the database level
            return await query.ToPagedResponseAsync(
                request,
                user => _mapper.Map<UserDto>(user),
                cancellationToken);
        }
    }
}
