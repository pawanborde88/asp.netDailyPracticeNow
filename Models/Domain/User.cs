namespace dailypracticeASPDotNet.Models.Domain
{
    /// <summary>
    /// Domain Model representing the User entity.
    /// Includes first_name, middle_name, last_name, role_id, password_hash, address, mobile_no, email_id,
    /// and raw binary photo data (byte[]).
    /// </summary>
    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public required string FirstName { get; set; }

        public string? MiddleName { get; set; }

        public required string LastName { get; set; }

        public required string EmailId { get; set; }

        public required string PasswordHash { get; set; }

        public string? MobileNo { get; set; }

        public string? Address { get; set; }

        public int RoleId { get; set; }

        /// <summary>
        /// Raw binary image data of the user photo stored directly in database.
        /// </summary>
        public byte[]? PhotoData { get; set; }

        /// <summary>
        /// MIME Content Type of the photo (e.g. image/jpeg, image/png).
        /// </summary>
        public string? PhotoContentType { get; set; }

        /// <summary>
        /// Original filename of the uploaded photo.
        /// </summary>
        public string? PhotoFileName { get; set; }

        /// <summary>
        /// Relative URL or endpoint path to access the photo (e.g. /api/Users/{id}/photo).
        /// </summary>
        public string? PhotoUrl { get; set; }

        // Navigation property
        public Role? Role { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Computed Full Name helper
        /// </summary>
        public string FullName => string.IsNullOrWhiteSpace(MiddleName)
            ? $"{FirstName} {LastName}"
            : $"{FirstName} {MiddleName} {LastName}";
    }
}
