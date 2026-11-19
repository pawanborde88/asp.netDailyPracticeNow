using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace dailypracticeASPDotNet.DTOs
{
    /// <summary>
    /// Request DTO for verifying the OTP code and completing login.
    /// </summary>
    public class VerifyOtpRequestDto
    {
        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [MaxLength(150)]
        [JsonPropertyName("email")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "OTP code is required.")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "OTP must be exactly 6 numeric digits.")]
        [JsonPropertyName("otp")]
        public required string Otp { get; set; }
    }
}

