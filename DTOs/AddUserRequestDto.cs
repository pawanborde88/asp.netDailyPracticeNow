using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace dailypracticeASPDotNet.DTOs
{
    /// <summary>
    /// Data Transfer Object for creating a new user via add_user API.
    /// Supports: first_name, middle_name, last_name, role_id, password, address, mobile_no, and email_id.
    /// </summary>
    public class AddUserRequestDto
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

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
        [JsonPropertyName("password")]
        public required string Password { get; set; }

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
    }
}
