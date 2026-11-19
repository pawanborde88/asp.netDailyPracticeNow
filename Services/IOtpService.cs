namespace dailypracticeASPDotNet.Services
{
    /// <summary>
    /// Service for generating, caching, validating, and invalidating One-Time Passwords (OTP).
    /// </summary>
    public interface IOtpService
    {
        /// <summary>
        /// Generates a cryptographically secure 6-digit numeric OTP for the given email,
        /// caches it with expiration, and returns the generated code.
        /// </summary>
        string GenerateOtp(string email, int expiryMinutes = 5);

        /// <summary>
        /// Validates the provided OTP for the given email against the cached value.
        /// Enforces expiration, exact match, and maximum attempts.
        /// </summary>
        (bool IsValid, string Message) ValidateOtp(string email, string otp);

        /// <summary>
        /// Immediately removes/consumes the OTP for the given email to prevent replay.
        /// </summary>
        void InvalidateOtp(string email);
    }
}

