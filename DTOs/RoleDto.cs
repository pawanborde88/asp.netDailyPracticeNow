using System.Text.Json.Serialization;

namespace dailypracticeASPDotNet.DTOs
{
    public class RoleDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public required string Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }
}
