using Microsoft.AspNetCore.Http;

namespace dailypracticeASPDotNet.Services
{
    /// <summary>
    /// Service interface for handling user photo file validation, storage, and deletion.
    /// </summary>
    public interface IPhotoService
    {
        /// <summary>
        /// Validates and saves an uploaded user photo to disk.
        /// Returns relative URL path (e.g., /uploads/user-photos/guid.jpg) on success.
        /// </summary>
        /// <param name="photo">Uploaded IFormFile</param>
        /// <returns>Tuple indicating success, relative photo URL, and error message if any</returns>
        Task<(bool Success, string? PhotoUrl, string? ErrorMessage)> SavePhotoAsync(IFormFile photo);

        /// <summary>
        /// Deletes an existing user photo from disk if present.
        /// </summary>
        /// <param name="relativePhotoUrl">Relative URL stored in database (e.g., /uploads/user-photos/guid.jpg)</param>
        /// <returns>True if deleted or file didn't exist; false if error occurred</returns>
        bool DeletePhoto(string? relativePhotoUrl);
    }
}

