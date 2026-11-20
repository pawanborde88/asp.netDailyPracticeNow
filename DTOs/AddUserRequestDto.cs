using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace dailypracticeASPDotNet.DTOs
{
    /// <summary>
    /// Data Transfer Object for creating a new user via add_user API.
    /// Supports: first_name, middle_name, last_name, role_id, password, address, mobile_no, email_id,
    /// and binary user photo file upload.
    /// </summary>
    public class AddUserRequestDto
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

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
        [JsonPropertyName("password")]
        [FromForm(Name = "password")]
        public string Password { get; set; } = string.Empty;

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
        /// User profile photo binary file (.jpg, .jpeg, .png, .webp, .gif, max 5MB).
        /// Uploaded via multipart/form-data.
        /// </summary>
        [JsonPropertyName("photo")]
        [FromForm(Name = "photo")]
        public IFormFile? Photo { get; set; }

        /// <summary>
        /// Optional Base64 encoded binary photo string (used when clients send application/json).
        /// </summary>
        [JsonPropertyName("photo_base64")]
        public string? PhotoBase64 { get; set; }
    }
}
