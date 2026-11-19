namespace dailypracticeASPDotNet.Services
{
    /// <summary>
    /// Service for sending emails via Zoho CPaaS / ZeptoMail Transactional Email REST API.
    /// </summary>
    public interface IZeptoMailService
    {
        /// <summary>
        /// Sends an OTP login email to the recipient via Zoho CPaaS / ZeptoMail.
        /// </summary>
        /// <param name="toEmail">Recipient email address</param>
        /// <param name="toName">Recipient display name</param>
        /// <param name="otpCode">6-digit numeric OTP</param>
        /// <param name="expiryMinutes">Validity duration in minutes</param>
        /// <returns>A tuple with success indicator and diagnostic/status message</returns>
        Task<(bool Success, string Message)> SendOtpEmailAsync(string toEmail, string toName, string otpCode, int expiryMinutes = 5);
    }
}

