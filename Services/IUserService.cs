using dailypracticeASPDotNet.DTOs;
using dailypracticeASPDotNet.DTOs.Common;

namespace dailypracticeASPDotNet.Services
{
    /// <summary>
    /// Service interface for User business logic, dynamic search, filtering, and pagination.
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// Retrieves a paginated, sorted, and filtered list of users with metadata.
        /// Executes at the database level with EF Core without materializing entire tables.
        /// </summary>
        /// <param name="request">Pagination, sorting, and filter parameters</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>PaginationResponseDto containing UserDto list and complete pagination metadata</returns>
        Task<PaginationResponseDto<UserDto>> GetPaginatedUsersAsync(
            UserPaginationRequestDto request,
            CancellationToken cancellationToken = default);
    }
}
