using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace dailypracticeASPDotNet.DTOs
{
    /// <summary>
    /// Request DTO for requesting an OTP code to login.
    /// Flexible binding supports 'email', 'email_id', and 'emailId' keys.
    /// </summary>
    public class SendOtpRequestDto : IValidatableObject
    {
        private string _email = string.Empty;

        [JsonPropertyName("email")]
        public string Email
        {
            get => _email;
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    _email = value.Trim();
                }
            }
        }

        [JsonPropertyName("email_id")]
        public string? EmailId
        {
            get => _email;
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    _email = value.Trim();
                }
            }
        }

        [JsonPropertyName("emailId")]
        public string? EmailIdCamel
        {
            get => _email;
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    _email = value.Trim();
                }
            }
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(_email))
            {
                yield return new ValidationResult("Email address is required.", new[] { nameof(Email) });
            }
            else if (!new EmailAddressAttribute().IsValid(_email))
            {
                yield return new ValidationResult("Invalid email format.", new[] { nameof(Email) });
            }
        }
    }
}
