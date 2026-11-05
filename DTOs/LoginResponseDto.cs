using System.Text.Json.Serialization;

namespace dailypracticeASPDotNet.DTOs
{
    /// <summary>
    /// Response returned upon successful authentication containing JWT Bearer Token.
    /// </summary>
    public class LoginResponseDto
    {
        [JsonPropertyName("token")]
        public required string Token { get; set; }

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = "Bearer";

        [JsonPropertyName("expires_at")]
        public DateTime ExpiresAt { get; set; }

        [JsonPropertyName("user")]
        public required UserDto User { get; set; }
    }
}
