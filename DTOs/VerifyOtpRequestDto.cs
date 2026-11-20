using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace dailypracticeASPDotNet.DTOs
{
    /// <summary>
    /// Request DTO for verifying the OTP code and completing login.
    /// Flexible binding supports 'email', 'email_id', 'emailId' and 'otp', 'otp_code', 'otpCode', 'code'.
    /// </summary>
    public class VerifyOtpRequestDto : IValidatableObject
    {
        private string _email = string.Empty;
        private string _otp = string.Empty;

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

        [JsonPropertyName("otp")]
        public string Otp
        {
            get => _otp;
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    _otp = value.Trim();
                }
            }
        }

        [JsonPropertyName("otp_code")]
        public string? OtpCode
        {
            get => _otp;
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    _otp = value.Trim();
                }
            }
        }

        [JsonPropertyName("otpCode")]
        public string? OtpCodeCamel
        {
            get => _otp;
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    _otp = value.Trim();
                }
            }
        }

        [JsonPropertyName("code")]
        public string? Code
        {
            get => _otp;
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    _otp = value.Trim();
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

            if (string.IsNullOrWhiteSpace(_otp))
            {
                yield return new ValidationResult("OTP code is required.", new[] { nameof(Otp) });
            }
            else if (!System.Text.RegularExpressions.Regex.IsMatch(_otp, @"^\d{6}$"))
            {
                yield return new ValidationResult("OTP must be exactly 6 numeric digits.", new[] { nameof(Otp) });
            }
        }
    }
}
