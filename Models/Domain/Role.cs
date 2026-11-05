namespace dailypracticeASPDotNet.Models.Domain
{
    /// <summary>
    /// Domain Model representing a User Role in the system (e.g., Admin, User, Manager).
    /// </summary>
    public class Role
    {
        public int Id { get; set; }

        public required string Name { get; set; }

        public string? Description { get; set; }
    }
}
