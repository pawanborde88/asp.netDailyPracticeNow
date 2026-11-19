using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;

namespace dailypracticeASPDotNet.Services
{
    /// <summary>
    /// Thread-safe in-memory OTP management using ASP.NET Core IMemoryCache.
    /// Manages generation, strict expiration, rate limiting, and brute-force protection.
    /// </summary>
    public class OtpService : IOtpService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<OtpService> _logger;

        private class CachedOtpEntry
        {
            public required string Code { get; set; }
            public DateTime ExpiresAtUtc { get; set; }
            public int AttemptsLeft { get; set; } = 3;
        }

        public OtpService(IMemoryCache memoryCache, ILogger<OtpService> logger)
        {
            _memoryCache = memoryCache;
            _logger = logger;
        }

        private static string GetCacheKey(string email) =>
            $"login_otp_{email.Trim().ToLowerInvariant()}";

        public string GenerateOtp(string email, int expiryMinutes = 5)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            var cacheKey = GetCacheKey(normalizedEmail);

            // Generate cryptographically strong 6-digit integer in range [100000, 999999]
            var otpValue = RandomNumberGenerator.GetInt32(100000, 1000000);
            var otpCode = otpValue.ToString("D6");

            var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

            var entry = new CachedOtpEntry
            {
                Code = otpCode,
                ExpiresAtUtc = expiresAt,
                AttemptsLeft = 3
            };

            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(expiryMinutes)
            };

            _memoryCache.Set(cacheKey, entry, cacheOptions);

            _logger.LogInformation(
                "[OTP SERVICE] Generated OTP for '{Email}': {OtpCode} (Expires in {Minutes} min at {ExpiresUtc} UTC)",
                normalizedEmail, otpCode, expiryMinutes, expiresAt);

            return otpCode;
        }

        public (bool IsValid, string Message) ValidateOtp(string email, string otp)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            var cacheKey = GetCacheKey(normalizedEmail);

            if (!_memoryCache.TryGetValue<CachedOtpEntry>(cacheKey, out var entry) || entry == null)
            {
                _logger.LogWarning("[OTP SERVICE] Validation failed: OTP expired or not requested for '{Email}'.", normalizedEmail);
                return (false, "OTP has expired or was not requested. Please request a new OTP.");
            }

            if (DateTime.UtcNow > entry.ExpiresAtUtc)
            {
                _memoryCache.Remove(cacheKey);
                _logger.LogWarning("[OTP SERVICE] Validation failed: OTP for '{Email}' is expired.", normalizedEmail);
                return (false, "OTP has expired. Please request a new OTP.");
            }

            if (entry.AttemptsLeft <= 0)
            {
                _memoryCache.Remove(cacheKey);
                _logger.LogWarning("[OTP SERVICE] Validation failed: Max attempts exceeded for '{Email}'.", normalizedEmail);
                return (false, "Too many incorrect attempts. This OTP has been invalidated. Please request a new OTP.");
            }

            if (!string.Equals(entry.Code, otp.Trim(), StringComparison.Ordinal))
            {
                entry.AttemptsLeft--;
                _logger.LogWarning(
                    "[OTP SERVICE] Validation failed: Incorrect OTP for '{Email}'. Attempts remaining: {Remaining}",
                    normalizedEmail, entry.AttemptsLeft);

                if (entry.AttemptsLeft <= 0)
                {
                    _memoryCache.Remove(cacheKey);
                    return (false, "Incorrect OTP. Maximum attempts exceeded. Please request a new OTP.");
                }

                return (false, $"Invalid OTP code. {entry.AttemptsLeft} attempt(s) remaining.");
            }

            _logger.LogInformation("[OTP SERVICE] OTP successfully validated for '{Email}'.", normalizedEmail);
            return (true, "OTP verified successfully.");
        }

        public void InvalidateOtp(string email)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            var cacheKey = GetCacheKey(normalizedEmail);
            _memoryCache.Remove(cacheKey);
            _logger.LogInformation("[OTP SERVICE] OTP consumed and invalidated for '{Email}'.", normalizedEmail);
        }
    }
}

