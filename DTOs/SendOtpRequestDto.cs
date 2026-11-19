using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace dailypracticeASPDotNet.DTOs
{
    /// <summary>
    /// Request DTO for requesting an OTP code to login.
    /// The email MUST already exist in the database.
    /// </summary>
    public class SendOtpRequestDto
    {
        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [MaxLength(150)]
        [JsonPropertyName("email")]
        public required string Email { get; set; }
    }
}

