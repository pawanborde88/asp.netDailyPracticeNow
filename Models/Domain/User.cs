namespace dailypracticeASPDotNet.Models.Domain
{
    /// <summary>
    /// Domain Model representing the User entity.
    /// Includes first_name, middle_name, last_name, role_id, password_hash, address, mobile_no, and email_id.
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
