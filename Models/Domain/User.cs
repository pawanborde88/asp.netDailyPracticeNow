namespace dailypracticeASPDotNet.Models.Domain
{
    /// <summary>
    /// Domain Model representing the User entity.
    /// Includes first_name, middle_name, last_name, role_id, password_hash, address, mobile_no, email_id, and photo_url.
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
        /// Relative URL or path to the user's uploaded profile photo (e.g. /uploads/user-photos/abc123.jpg).
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
