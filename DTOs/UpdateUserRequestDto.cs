using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace dailypracticeASPDotNet.DTOs
{
    /// <summary>
    /// Data Transfer Object for editing/updating an existing user via edit_user API.
    /// Supports: first_name, middle_name, last_name, role_id, email_id, mobile_no, address, optional password, and photo upload.
    /// </summary>
    public class UpdateUserRequestDto
    {
        [Required(ErrorMessage = "First name is required.")]
        [MaxLength(50)]
        [JsonPropertyName("first_name")]
        [FromForm(Name = "first_name")]
        public string FirstName { get; set; } = string.Empty;

        [MaxLength(50)]
        [JsonPropertyName("middle_name")]
        [FromForm(Name = "middle_name")]
        public string? MiddleName { get; set; }

        [Required(ErrorMessage = "Last name is required.")]
        [MaxLength(50)]
        [JsonPropertyName("last_name")]
        [FromForm(Name = "last_name")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Role ID is required.")]
        [JsonPropertyName("role_id")]
        [FromForm(Name = "role_id")]
        public int RoleId { get; set; } = 2; // Default to Role 2: User

        [MaxLength(250)]
        [JsonPropertyName("address")]
        [FromForm(Name = "address")]
        public string? Address { get; set; }

        [MaxLength(20)]
        [Phone(ErrorMessage = "Invalid mobile number format.")]
        [JsonPropertyName("mobile_no")]
        [FromForm(Name = "mobile_no")]
        public string? MobileNo { get; set; }

        [Required(ErrorMessage = "Email ID is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [MaxLength(150)]
        [JsonPropertyName("email_id")]
        [FromForm(Name = "email_id")]
        public string EmailId { get; set; } = string.Empty;

        /// <summary>
        /// Optional password field.
        /// If provided with length >= 6, password will be re-hashed and updated.
        /// If left empty or null, existing password hash is preserved.
        /// </summary>
        [MinLength(6, ErrorMessage = "If provided, password must be at least 6 characters.")]
        [JsonPropertyName("password")]
        [FromForm(Name = "password")]
        public string? Password { get; set; }

        /// <summary>
        /// Optional updated user photo file (.jpg, .jpeg, .png, .webp, .gif, max 5MB).
        /// </summary>
        [JsonPropertyName("photo")]
        [FromForm(Name = "photo")]
        public IFormFile? Photo { get; set; }

        /// <summary>
        /// Optional updated external photo URL.
        /// </summary>
        [MaxLength(500)]
        [JsonPropertyName("photo_url")]
        [FromForm(Name = "photo_url")]
        public string? PhotoUrl { get; set; }
    }
}
