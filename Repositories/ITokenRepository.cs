using dailypracticeASPDotNet.Models.Domain;

namespace dailypracticeASPDotNet.Repositories
{
    /// <summary>
    /// Interface for JWT token generation service following the Repository/Service Pattern.
    /// </summary>
    public interface ITokenRepository
    {
        string CreateJwtToken(User user, out DateTime expiresAt);
    }
}
