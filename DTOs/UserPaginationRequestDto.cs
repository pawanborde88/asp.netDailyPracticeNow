using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using dailypracticeASPDotNet.DTOs.Common;

namespace dailypracticeASPDotNet.DTOs
{
    /// <summary>
    /// User-specific pagination request DTO extending the centralized PaginationRequestDto
    /// with module-specific filter properties (such as role_id).
    /// </summary>
    public class UserPaginationRequestDto : PaginationRequestDto
    {
        /// <summary>
        /// Optional filter by Role ID (1 = Admin, 2 = User, 3 = Manager).
        /// </summary>
        [JsonPropertyName("role_id")]
        [FromQuery(Name = "role_id")]
        public int? RoleId { get; set; }

        [JsonPropertyName("roleId")]
        [FromQuery(Name = "roleId")]
        public int? RoleIdAlias
        {
            set { if (value.HasValue) RoleId = value.Value; }
        }
    }
}
