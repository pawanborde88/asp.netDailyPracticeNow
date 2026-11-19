using System.Net;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using dailypracticeASPDotNet.Models;

namespace dailypracticeASPDotNet.Services
{
    /// <summary>
    /// Implementation of IZeptoMailService supporting both SMTP and REST API.
    /// Default mode is SMTP using Zoho CPaaS credentials (host: smtp.zeptomail.in, port: 587, user: emailapikey).
    /// </summary>
    public class ZeptoMailService : IZeptoMailService
    {
        private readonly HttpClient _httpClient;
        private readonly ZeptoMailSettings _settings;
        private readonly ILogger<ZeptoMailService> _logger;

        public ZeptoMailService(
            HttpClient httpClient,
            IOptions<ZeptoMailSettings> options,
            ILogger<ZeptoMailService> logger)
        {
            _httpClient = httpClient;
            _settings = options.Value;
            _logger = logger;
        }

        public async Task<(bool Success, string Message)> SendOtpEmailAsync(
            string toEmail,
            string toName,
            string otpCode,
            int expiryMinutes = 5)
        {
            var normalizedEmail = toEmail.Trim();
            var recipientName = string.IsNullOrWhiteSpace(toName) ? "User" : toName.Trim();

            // 1. Mock Mode
            if (_settings.MockSendingInDevelopment)
            {
                _logger.LogInformation(
                    "[ZeptoMail] Mock mode active: Simulated OTP email to '{Email}' with code '{OtpCode}'.",
                    normalizedEmail, otpCode);
                return (true, "OTP email simulated in development mode.");
            }

            // 2. Generate Email Templates
            var (htmlBody, textBody) = BuildEmailTemplates(recipientName, otpCode, expiryMinutes);

            // 3. Dispatch via SMTP or REST API based on configuration
            if (_settings.UseSmtp)
            {
                return await SendViaSmtpAsync(normalizedEmail, recipientName, otpCode, expiryMinutes, htmlBody, textBody);
            }
            else
            {
                return await SendViaRestApiAsync(normalizedEmail, recipientName, otpCode, htmlBody, textBody);
            }
        }

        // =========================================================================
        // SMTP SENDER (System.Net.Mail)
        // =========================================================================
        private async Task<(bool Success, string Message)> SendViaSmtpAsync(
            string toEmail,
            string toName,
            string otpCode,
            int expiryMinutes,
            string htmlBody,
            string textBody)
        {
            var host = !string.IsNullOrWhiteSpace(_settings.SmtpHost) ? _settings.SmtpHost : "smtp.zeptomail.in";
            var port = _settings.SmtpPort > 0 ? _settings.SmtpPort : 587;
            var username = !string.IsNullOrWhiteSpace(_settings.SmtpUsername) ? _settings.SmtpUsername : "emailapikey";

            // Password priority: SmtpPassword > SendMailToken
            var password = !string.IsNullOrWhiteSpace(_settings.SmtpPassword)
                ? _settings.SmtpPassword
                : _settings.SendMailToken;

            if (string.IsNullOrWhiteSpace(password) || password.Contains("YOUR_ZEPTOMAIL_SEND_MAIL_TOKEN", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "[ZeptoMail SMTP] Password or SendMailToken is missing. Generated OTP: {OtpCode}", otpCode);
                return (false, "ZeptoMail SMTP Password is not configured in appsettings.json. Check server logs for OTP.");
            }

            try
            {
                _logger.LogInformation(
                    "[ZeptoMail SMTP] Connecting to {Host}:{Port} with username '{Username}' to send to '{ToEmail}'...",
                    host, port, username, toEmail);

                using var client = new SmtpClient(host, port)
                {
                    EnableSsl = _settings.SmtpEnableSsl,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(username, password),
                    Timeout = 15000
                };

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(_settings.FromEmail, _settings.FromName),
                    Subject = $"Your Login OTP Code: {otpCode} - {_settings.FromName}",
                    Body = htmlBody,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(new MailAddress(toEmail, toName));

                // Add alternative plain-text view
                var plainTextView = AlternateView.CreateAlternateViewFromString(textBody, Encoding.UTF8, "text/plain");
                mailMessage.AlternateViews.Add(plainTextView);

                await client.SendMailAsync(mailMessage);

                _logger.LogInformation("[ZeptoMail SMTP] OTP email sent successfully to '{Email}'.", toEmail);
                return (true, "OTP email dispatched successfully via ZeptoMail SMTP.");
            }
            catch (SmtpException ex)
            {
                _logger.LogError(ex,
                    "[ZeptoMail SMTP] SmtpException while sending to '{Email}'. Status: {StatusCode}, Message: {Message}",
                    toEmail, ex.StatusCode, ex.Message);

                return (false, $"ZeptoMail SMTP Error ({ex.StatusCode}): {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ZeptoMail SMTP] Exception while sending email to '{Email}'.", toEmail);
                return (false, $"SMTP connection failed: {ex.Message}");
            }
        }

        // =========================================================================
        // REST API SENDER (HttpClient)
        // =========================================================================
        private async Task<(bool Success, string Message)> SendViaRestApiAsync(
            string toEmail,
            string toName,
            string otpCode,
            string htmlBody,
            string textBody)
        {
            if (string.IsNullOrWhiteSpace(_settings.SendMailToken) ||
                _settings.SendMailToken.Contains("YOUR_ZEPTOMAIL_SEND_MAIL_TOKEN", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("[ZeptoMail REST] SendMailToken is missing. Generated OTP: {OtpCode}", otpCode);
                return (false, "ZeptoMail SendMailToken is not configured yet. Check server logs for OTP code.");
            }

            var authToken = _settings.SendMailToken.Trim();
            if (!authToken.StartsWith("Zoho-enczapikey", StringComparison.OrdinalIgnoreCase))
            {
                authToken = $"Zoho-enczapikey {authToken}";
            }

            var payload = new
            {
                from = new
                {
                    address = _settings.FromEmail,
                    name = _settings.FromName
                },
                to = new object[]
                {
                    new
                    {
                        email_address = new
                        {
                            address = toEmail,
                            name = toName
                        }
                    }
                },
                subject = $"Your Login OTP Code: {otpCode} - {_settings.FromName}",
                htmlbody = htmlBody,
                textbody = textBody
            };

            var jsonContent = JsonSerializer.Serialize(payload);

            using var request = new HttpRequestMessage(HttpMethod.Post, _settings.ApiUrl)
            {
                Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
            };

            request.Headers.Add("Authorization", authToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            try
            {
                _logger.LogInformation("[ZeptoMail REST] Sending OTP email to '{Email}' via {Url}...", toEmail, _settings.ApiUrl);
                var response = await _httpClient.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("[ZeptoMail REST] OTP email sent successfully to '{Email}'.", toEmail);
                    return (true, "OTP email dispatched successfully via ZeptoMail API.");
                }
                else
                {
                    _logger.LogError("[ZeptoMail REST] HTTP {StatusCode}: {ResponseBody}", response.StatusCode, responseBody);
                    return (false, $"ZeptoMail error (HTTP {(int)response.StatusCode}): {responseBody}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ZeptoMail REST] Exception sending to '{Email}'.", toEmail);
                return (false, $"Client error sending email: {ex.Message}");
            }
        }

        // =========================================================================
        // TEMPLATE GENERATOR
        // =========================================================================
        private static (string html, string text) BuildEmailTemplates(string recipientName, string otpCode, int expiryMinutes)
        {
            var html = $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset='utf-8'>
  <meta name='viewport' content='width=device-width, initial-scale=1.0'>
  <title>Your Login OTP</title>
</head>
<body style='margin:0; padding:0; background-color:#f4f6f9; font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, Helvetica, Arial, sans-serif;'>
  <table role='presentation' width='100%' cellspacing='0' cellpadding='0' border='0' style='background-color:#f4f6f9; padding: 40px 15px;'>
    <tr>
      <td align='center'>
        <table role='presentation' width='100%' style='max-width: 520px; background:#ffffff; border-radius: 12px; box-shadow: 0 4px 14px rgba(0,0,0,0.08); overflow: hidden;' cellspacing='0' cellpadding='0' border='0'>
          <tr>
            <td style='background: linear-gradient(135deg, #4f46e5 0%, #7c3aed 100%); padding: 30px 40px; text-align: center;'>
              <h1 style='color: #ffffff; margin: 0; font-size: 24px; font-weight: 700; letter-spacing: -0.5px;'>Daily Practice App</h1>
              <p style='color: #e0e7ff; margin: 6px 0 0; font-size: 14px;'>Secure Account Verification</p>
            </td>
          </tr>
          <tr>
            <td style='padding: 36px 40px 24px;'>
              <p style='margin: 0 0 16px; font-size: 16px; color: #1f2937;'>Hello <strong>{recipientName}</strong>,</p>
              <p style='margin: 0 0 24px; font-size: 15px; color: #4b5563; line-height: 1.6;'>
                You requested a One-Time Password (OTP) to log in to your account. Use the verification code below:
              </p>
              <div style='background: #f8fafc; border: 2px dashed #cbd5e1; border-radius: 8px; padding: 22px; text-align: center; margin: 24px 0;'>
                <span style='font-size: 36px; font-weight: 800; letter-spacing: 8px; color: #4f46e5; font-family: monospace;'>{otpCode}</span>
              </div>
              <p style='margin: 0 0 12px; font-size: 14px; color: #6b7280; text-align: center;'>
                ⏱️ This code is valid for <strong>{expiryMinutes} minutes</strong>.
              </p>
              <p style='margin: 20px 0 0; font-size: 13px; color: #9ca3af; line-height: 1.5; border-top: 1px solid #e5e7eb; padding-top: 16px;'>
                🔒 Never share your OTP with anyone. If you did not request this, please ignore this email.
              </p>
            </td>
          </tr>
          <tr>
            <td style='background: #f9fafb; padding: 18px 40px; text-align: center; border-top: 1px solid #f3f4f6;'>
              <p style='margin: 0; font-size: 12px; color: #9ca3af;'>
                Sent automatically via Zoho CPaaS / ZeptoMail SMTP.
              </p>
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>";

            var text = $"Hello {recipientName},\n\nYour login verification OTP is: {otpCode}\nThis code is valid for {expiryMinutes} minutes.\n\nIf you did not request this, please ignore this email.";

            return (html, text);
        }
    }
}
