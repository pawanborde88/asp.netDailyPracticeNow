using Microsoft.EntityFrameworkCore;
using dailypracticeASPDotNet.DTOs.Common;

namespace dailypracticeASPDotNet.Extensions
{
    /// <summary>
    /// Centralized reusable pagination and safe sorting extension methods for IQueryable.
    /// Executes asynchronously at the database level with EF Core, avoiding in-memory materialization of entire datasets.
    /// </summary>
    public static class PaginationExtensions
    {
        /// <summary>
        /// Asynchronously executes database-level pagination on an IQueryable query,
        /// counting total records and applying Skip/Take before materializing items.
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="query">The IQueryable database query</param>
        /// <param name="request">Pagination request parameters</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>PaginationResponseDto containing paged items and metadata</returns>
        public static async Task<PaginationResponseDto<T>> ToPagedResponseAsync<T>(
            this IQueryable<T> query,
            PaginationRequestDto request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(query);
            request ??= new PaginationRequestDto();

            // 1. Database-level count query
            var totalRecords = await query.CountAsync(cancellationToken);

            if (totalRecords == 0)
            {
                return PaginationResponseDto<T>.Empty(request.PageNumber, request.PageSize);
            }

            // 2. Database-level Skip() and Take()
            var skipCount = (request.PageNumber - 1) * request.PageSize;
            var items = await query
                .Skip(skipCount)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            return new PaginationResponseDto<T>(items, totalRecords, request.PageNumber, request.PageSize);
        }

        /// <summary>
        /// Asynchronously executes database-level pagination and maps items to destination DTOs.
        /// </summary>
        /// <typeparam name="TSource">Database source entity type</typeparam>
        /// <typeparam name="TDestination">Destination DTO type</typeparam>
        /// <param name="query">The IQueryable database query</param>
        /// <param name="request">Pagination request parameters</param>
        /// <param name="mapper">Projection/mapping function</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>PaginationResponseDto containing mapped DTO items and metadata</returns>
        public static async Task<PaginationResponseDto<TDestination>> ToPagedResponseAsync<TSource, TDestination>(
            this IQueryable<TSource> query,
            PaginationRequestDto request,
            Func<TSource, TDestination> mapper,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(query);
            ArgumentNullException.ThrowIfNull(mapper);
            request ??= new PaginationRequestDto();

            // 1. Database-level count query
            var totalRecords = await query.CountAsync(cancellationToken);

            if (totalRecords == 0)
            {
                return PaginationResponseDto<TDestination>.Empty(request.PageNumber, request.PageSize);
            }

            // 2. Database-level Skip() and Take() pagination
            var skipCount = (request.PageNumber - 1) * request.PageSize;
            var sourceItems = await query
                .Skip(skipCount)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            // 3. In-memory projection of only the paged items (at most pageSize items)
            var mappedItems = sourceItems.Select(mapper).ToList();

            return new PaginationResponseDto<TDestination>(mappedItems, totalRecords, request.PageNumber, request.PageSize);
        }

        /// <summary>
        /// Applies safe, SQL-injection-proof sorting using a dictionary whitelist of allowed property sorters.
        /// If the requested sortBy column is null or not found in the whitelist, the default sorter is applied.
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="query">The IQueryable query</param>
        /// <param name="sortBy">User-supplied sort column name</param>
        /// <param name="isAscending">True for ascending, false for descending</param>
        /// <param name="sortMap">Case-insensitive dictionary of allowed sort functions</param>
        /// <param name="defaultSort">Fallback sort function</param>
        /// <returns>Sorted IQueryable query</returns>
        public static IQueryable<T> ApplySafeSort<T>(
            this IQueryable<T> query,
            string? sortBy,
            bool isAscending,
            IReadOnlyDictionary<string, Func<IQueryable<T>, bool, IQueryable<T>>> sortMap,
            Func<IQueryable<T>, bool, IQueryable<T>> defaultSort)
        {
            ArgumentNullException.ThrowIfNull(query);
            ArgumentNullException.ThrowIfNull(sortMap);
            ArgumentNullException.ThrowIfNull(defaultSort);

            if (!string.IsNullOrWhiteSpace(sortBy) && sortMap.TryGetValue(sortBy.Trim().ToLowerInvariant(), out var sorter))
            {
                return sorter(query, isAscending);
            }

            return defaultSort(query, isAscending);
        }
    }
}
