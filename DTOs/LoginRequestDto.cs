using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace dailypracticeASPDotNet.DTOs
{
    /// <summary>
    /// Login Request DTO strictly taking only Email and Password.
    /// Supports both "email" and "email_id" in the request JSON payload.
    /// </summary>
    public class LoginRequestDto
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("email_id")]
        public string? EmailId { get; set; }

        /// <summary>
        /// Normalized Email property prioritizing whichever property was supplied.
        /// </summary>
        [JsonIgnore]
        public string NormalizedEmail => !string.IsNullOrWhiteSpace(Email)
            ? Email.Trim()
            : (!string.IsNullOrWhiteSpace(EmailId) ? EmailId.Trim() : string.Empty);

        [Required(ErrorMessage = "Password is required.")]
        [JsonPropertyName("password")]
        public required string Password { get; set; }
    }
}
