using Microsoft.AspNetCore.Http;

namespace dailypracticeASPDotNet.Services
{
    /// <summary>
    /// Result model containing processed binary photo information.
    /// </summary>
    public class PhotoProcessResult
    {
        public bool Success { get; set; }
        public byte[]? BinaryData { get; set; }
        public string? ContentType { get; set; }
        public string? FileName { get; set; }
        public string? RelativeUrl { get; set; }
        public string? ErrorMessage { get; set; }

        public static PhotoProcessResult Ok(byte[] binaryData, string contentType, string fileName, string? relativeUrl = null)
        {
            return new PhotoProcessResult
            {
                Success = true,
                BinaryData = binaryData,
                ContentType = contentType,
                FileName = fileName,
                RelativeUrl = relativeUrl
            };
        }

        public static PhotoProcessResult Fail(string errorMessage)
        {
            return new PhotoProcessResult
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }
    }

    /// <summary>
    /// Service interface for handling binary user photo validation, binary extraction, disk cache storage, and deletion.
    /// </summary>
    public interface IPhotoService
    {
        /// <summary>
        /// Validates an uploaded IFormFile, extracts its raw binary bytes, and optionally caches it to disk.
        /// </summary>
        Task<PhotoProcessResult> ProcessPhotoAsync(IFormFile photo);

        /// <summary>
        /// Validates a base64 encoded binary string and converts it to raw binary bytes.
        /// </summary>
        Task<PhotoProcessResult> ProcessBase64PhotoAsync(string base64String, string? suggestedFileName = null);

        /// <summary>
        /// Deletes an existing user photo from disk cache if present.
        /// </summary>
        bool DeletePhoto(string? relativePhotoUrl);
    }
}
