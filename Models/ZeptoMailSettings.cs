namespace dailypracticeASPDotNet.Models
{
    /// <summary>
    /// Configuration settings for Zoho CPaaS / ZeptoMail (supports both SMTP and REST API).
    /// </summary>
    public class ZeptoMailSettings
    {
        public const string SectionName = "ZeptoMail";

        /// <summary>
        /// Choose sending mode: true for SMTP, false for REST API.
        /// </summary>
        public bool UseSmtp { get; set; } = true;

        // ==========================================
        // SMTP Configuration
        // ==========================================
        /// <summary>
        /// Zoho CPaaS / ZeptoMail SMTP Host.
        /// India: smtp.zeptomail.in
        /// US / Global: smtp.zeptomail.com
        /// </summary>
        public string SmtpHost { get; set; } = "smtp.zeptomail.in";

        /// <summary>
        /// Standard TLS/STARTTLS Port: 587 (or SSL Port: 465)
        /// </summary>
        public int SmtpPort { get; set; } = 587;

        /// <summary>
        /// Fixed username displayed in Zoho CPaaS: "emailapikey"
        /// </summary>
        public string SmtpUsername { get; set; } = "emailapikey";

        /// <summary>
        /// SMTP Password: Password 1 from Zoho CPaaS or the Send Mail Token.
        /// If omitted, falls back to SendMailToken.
        /// </summary>
        public string SmtpPassword { get; set; } = string.Empty;

        /// <summary>
        /// Enables SSL/TLS for SMTP connection (always true for ZeptoMail).
        /// </summary>
        public bool SmtpEnableSsl { get; set; } = true;

        // ==========================================
        // REST API Configuration
        // ==========================================
        /// <summary>
        /// API endpoint URL.
        /// India: https://api.zeptomail.in/v1.1/email
        /// US / Global: https://api.zeptomail.com/v1.1/email
        /// </summary>
        public string ApiUrl { get; set; } = "https://api.zeptomail.in/v1.1/email";

        /// <summary>
        /// Send Mail Token from Zoho CPaaS agent (e.g. Zoho-enczapikey ...).
        /// </summary>
        public string SendMailToken { get; set; } = string.Empty;

        // ==========================================
        // Sender Details
        // ==========================================
        /// <summary>
        /// Verified sender email configured on your domain in Zoho CPaaS (e.g. noreply@rbdevelops.co.in).
        /// </summary>
        public string FromEmail { get; set; } = "noreply@rbdevelops.co.in";

        /// <summary>
        /// Sender display name.
        /// </summary>
        public string FromName { get; set; } = "Daily Practice App";

        /// <summary>
        /// When true, logs the OTP to logger without calling ZeptoMail.
        /// </summary>
        public bool MockSendingInDevelopment { get; set; } = false;
    }
}
