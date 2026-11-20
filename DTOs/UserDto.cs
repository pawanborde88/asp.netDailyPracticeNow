using System.Text.Json.Serialization;

namespace dailypracticeASPDotNet.DTOs
{
    /// <summary>
    /// Safe User response DTO returned by fetch_users, add_user, and edit_user APIs.
    /// Excludes sensitive details such as PasswordHash, and includes photo_url.
    /// </summary>
    public class UserDto
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("first_name")]
        public required string FirstName { get; set; }

        [JsonPropertyName("middle_name")]
        public string? MiddleName { get; set; }

        [JsonPropertyName("last_name")]
        public required string LastName { get; set; }

        [JsonPropertyName("full_name")]
        public string FullName => string.IsNullOrWhiteSpace(MiddleName)
            ? $"{FirstName} {LastName}"
            : $"{FirstName} {MiddleName} {LastName}";

        [JsonPropertyName("email_id")]
        public required string EmailId { get; set; }

        [JsonPropertyName("mobile_no")]
        public string? MobileNo { get; set; }

        [JsonPropertyName("address")]
        public string? Address { get; set; }

        [JsonPropertyName("role_id")]
        public int RoleId { get; set; }

        [JsonPropertyName("role_name")]
        public string? RoleName { get; set; }

        [JsonPropertyName("photo_url")]
        public string? PhotoUrl { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
