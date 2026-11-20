using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace dailypracticeASPDotNet.DTOs.Common
{
    /// <summary>
    /// Centralized, reusable pagination request DTO for querying paged, sorted, and filtered data.
    /// Supports both query parameters (GET) and JSON request payloads (POST).
    /// Enforces constraints: pageNumber >= 1, pageSize clamped between 1 and 100, safe sort direction.
    /// </summary>
    public class PaginationRequestDto
    {
        public const int DefaultPageNumber = 1;
        public const int DefaultPageSize = 10;
        public const int MaxPageSize = 100;

        private int _pageNumber = DefaultPageNumber;
        private int _pageSize = DefaultPageSize;
        private string _sortDirection = "desc";
        private string? _searchTerm;
        private string? _sortBy;

        /// <summary>
        /// 1-based page number. Default = 1.
        /// If missing, zero, or negative, automatically falls back to 1.
        /// </summary>
        [JsonPropertyName("pageNumber")]
        [FromQuery(Name = "pageNumber")]
        public int PageNumber
        {
            get => _pageNumber;
            set => _pageNumber = value < 1 ? DefaultPageNumber : value;
        }

        /// <summary>
        /// Number of records per page. Default = 10. Maximum allowed = 100.
        /// If missing, zero, or negative, defaults to 10. If greater than 100, clamped to 100.
        /// </summary>
        [JsonPropertyName("pageSize")]
        [FromQuery(Name = "pageSize")]
        public int PageSize
        {
            get => _pageSize;
            set
            {
                if (value < 1)
                {
                    _pageSize = DefaultPageSize;
                }
                else if (value > MaxPageSize)
                {
                    _pageSize = MaxPageSize;
                }
                else
                {
                    _pageSize = value;
                }
            }
        }

        /// <summary>
        /// Optional search term for text search across supported entity fields.
        /// </summary>
        [JsonPropertyName("searchTerm")]
        [FromQuery(Name = "searchTerm")]
        public string? SearchTerm
        {
            get => _searchTerm;
            set => _searchTerm = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        /// <summary>
        /// Optional field name to sort by (e.g. "id", "firstName", "createdAt").
        /// Checked against a whitelist in the service layer to prevent SQL injection.
        /// </summary>
        [JsonPropertyName("sortBy")]
        [FromQuery(Name = "sortBy")]
        public string? SortBy
        {
            get => _sortBy;
            set => _sortBy = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        /// <summary>
        /// Sort direction: "asc" (ascending) or "desc" (descending). Default = "desc".
        /// </summary>
        [JsonPropertyName("sortDirection")]
        [FromQuery(Name = "sortDirection")]
        public string SortDirection
        {
            get => _sortDirection;
            set => _sortDirection = string.Equals(value?.Trim(), "asc", StringComparison.OrdinalIgnoreCase) ? "asc" : "desc";
        }

        /// <summary>
        /// Helper property returning true if SortDirection is ascending ("asc").
        /// </summary>
        [JsonIgnore]
        public bool IsAscending => string.Equals(SortDirection, "asc", StringComparison.OrdinalIgnoreCase);

        // =========================================================================
        // Snake_case aliases for client convenience
        // =========================================================================
        [JsonPropertyName("page_number")]
        [FromQuery(Name = "page_number")]
        public int? PageNumberAlias
        {
            set { if (value.HasValue) PageNumber = value.Value; }
        }

        [JsonPropertyName("page_size")]
        [FromQuery(Name = "page_size")]
        public int? PageSizeAlias
        {
            set { if (value.HasValue) PageSize = value.Value; }
        }

        [JsonPropertyName("search_term")]
        [FromQuery(Name = "search_term")]
        public string? SearchTermAlias
        {
            set { if (!string.IsNullOrWhiteSpace(value)) SearchTerm = value; }
        }

        [JsonPropertyName("sort_by")]
        [FromQuery(Name = "sort_by")]
        public string? SortByAlias
        {
            set { if (!string.IsNullOrWhiteSpace(value)) SortBy = value; }
        }

        [JsonPropertyName("sort_direction")]
        [FromQuery(Name = "sort_direction")]
        public string? SortDirectionAlias
        {
            set { if (!string.IsNullOrWhiteSpace(value)) SortDirection = value; }
        }
    }
}
