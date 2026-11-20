using System.Text.Json.Serialization;

namespace dailypracticeASPDotNet.DTOs.Common
{
    /// <summary>
    /// Generic, centralized pagination response DTO wrapping paginated items along with complete pagination metadata.
    /// Reusable across all entities and API modules.
    /// </summary>
    /// <typeparam name="T">Type of items in the page payload</typeparam>
    public class PaginationResponseDto<T>
    {
        /// <summary>
        /// The list of items retrieved for the current page.
        /// </summary>
        [JsonPropertyName("data")]
        public IReadOnlyList<T> Data { get; init; } = Array.Empty<T>();

        /// <summary>
        /// Total number of records matching the search/filter criteria in the database.
        /// </summary>
        [JsonPropertyName("totalRecords")]
        public int TotalRecords { get; init; }

        /// <summary>
        /// Current 1-based page number.
        /// </summary>
        [JsonPropertyName("pageNumber")]
        public int PageNumber { get; init; }

        /// <summary>
        /// Number of records requested per page.
        /// </summary>
        [JsonPropertyName("pageSize")]
        public int PageSize { get; init; }

        /// <summary>
        /// Total number of pages calculated from TotalRecords and PageSize.
        /// </summary>
        [JsonPropertyName("totalPages")]
        public int TotalPages { get; init; }

        /// <summary>
        /// Indicates whether there is a previous page available.
        /// </summary>
        [JsonPropertyName("hasPreviousPage")]
        public bool HasPreviousPage { get; init; }

        /// <summary>
        /// Indicates whether there is a subsequent page available.
        /// </summary>
        [JsonPropertyName("hasNextPage")]
        public bool HasNextPage { get; init; }

        public PaginationResponseDto()
        {
        }

        public PaginationResponseDto(IReadOnlyList<T> data, int totalRecords, int pageNumber, int pageSize)
        {
            Data = data ?? Array.Empty<T>();
            TotalRecords = totalRecords < 0 ? 0 : totalRecords;
            PageNumber = pageNumber < 1 ? 1 : pageNumber;
            PageSize = pageSize < 1 ? 10 : pageSize;

            TotalPages = TotalRecords > 0
                ? (int)Math.Ceiling(TotalRecords / (double)PageSize)
                : 0;

            HasPreviousPage = PageNumber > 1 && (TotalPages == 0 || PageNumber <= TotalPages + 1);
            HasNextPage = TotalPages > 0 && PageNumber < TotalPages;
        }

        /// <summary>
        /// Factory helper returning an empty pagination response.
        /// </summary>
        public static PaginationResponseDto<T> Empty(int pageNumber = 1, int pageSize = 10)
        {
            return new PaginationResponseDto<T>(Array.Empty<T>(), 0, pageNumber, pageSize);
        }
    }
}
