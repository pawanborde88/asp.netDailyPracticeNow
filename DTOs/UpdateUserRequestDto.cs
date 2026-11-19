using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace dailypracticeASPDotNet.DTOs
{
    /// <summary>
    /// Data Transfer Object for editing/updating an existing user via edit_user API.
    /// Supports: first_name, middle_name, last_name, role_id, email_id, mobile_no, address, and optional password.
    /// </summary>
    public class UpdateUserRequestDto
    {
        [Required(ErrorMessage = "First name is required.")]
        [MaxLength(50)]
        [JsonPropertyName("first_name")]
        public required string FirstName { get; set; }

        [MaxLength(50)]
        [JsonPropertyName("middle_name")]
        public string? MiddleName { get; set; }

        [Required(ErrorMessage = "Last name is required.")]
        [MaxLength(50)]
        [JsonPropertyName("last_name")]
        public required string LastName { get; set; }

        [Required(ErrorMessage = "Role ID is required.")]
        [JsonPropertyName("role_id")]
        public int RoleId { get; set; } = 2; // Default to Role 2: User

        [MaxLength(250)]
        [JsonPropertyName("address")]
        public string? Address { get; set; }

        [MaxLength(20)]
        [Phone(ErrorMessage = "Invalid mobile number format.")]
        [JsonPropertyName("mobile_no")]
        public string? MobileNo { get; set; }

        [Required(ErrorMessage = "Email ID is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [MaxLength(150)]
        [JsonPropertyName("email_id")]
        public required string EmailId { get; set; }

        /// <summary>
        /// Optional password field.
        /// If provided with length >= 6, password will be re-hashed and updated.
        /// If left empty or null, existing password hash is preserved.
        /// </summary>
        [MinLength(6, ErrorMessage = "If provided, password must be at least 6 characters.")]
        [JsonPropertyName("password")]
        public string? Password { get; set; }
    }
}

