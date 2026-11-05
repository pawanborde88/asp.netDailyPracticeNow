using System.ComponentModel.DataAnnotations;

namespace dailypracticeASPDotNet.DTOs
{
    /// <summary>
    /// Data Transfer Object for user registration.
    /// </summary>
    public class RegisterRequestDto
    {
        [Required(ErrorMessage = "Full name is required.")]
        [MaxLength(100)]
        public required string FullName { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
        public required string Password { get; set; }

        public string Role { get; set; } = "User";
    }
}
