using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace dailypracticeASPDotNet.DTOs
{
    /// <summary>
    /// Request DTO for deleting a user via POST api/Users/delete_user.
    /// Supports user_id (and id alias) in JSON and form payloads.
    /// </summary>
    public class DeleteUserRequestDto
    {
        [Required(ErrorMessage = "user_id is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "user_id must be a positive digit.")]
        [JsonPropertyName("user_id")]
        [FromForm(Name = "user_id")]
        public int UserId { get; set; }

        /// <summary>
        /// Optional alias accepting 'id' for maximum client compatibility.
        /// </summary>
        [JsonPropertyName("id")]
        [FromForm(Name = "id")]
        public int? Id
        {
            get => UserId;
            set
            {
                if (value.HasValue && UserId == 0)
                {
                    UserId = value.Value;
                }
            }
        }
    }
}

