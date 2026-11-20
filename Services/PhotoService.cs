using System.Text.RegularExpressions;

namespace dailypracticeASPDotNet.Services
{
    /// <summary>
    /// Implementation of IPhotoService providing binary extraction, validation,
    /// disk caching in wwwroot/uploads/user-photos, and base64 handling.
    /// </summary>
    public partial class PhotoService : IPhotoService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<PhotoService> _logger;

        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp", ".gif"
        };

        private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/png", "image/webp", "image/gif", "image/pjpeg"
        };

        // Maximum allowed file size: 5 MB
        private const long MaxFileSizeInBytes = 5 * 1024 * 1024;
        private const string UploadSubFolder = "uploads/user-photos";

        public PhotoService(IWebHostEnvironment environment, ILogger<PhotoService> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        public async Task<PhotoProcessResult> ProcessPhotoAsync(IFormFile photo)
        {
            if (photo == null || photo.Length == 0)
            {
                return PhotoProcessResult.Fail("No photo file was provided or file is empty.");
            }

            // 1. Validate file size
            if (photo.Length > MaxFileSizeInBytes)
            {
                return PhotoProcessResult.Fail($"Photo file exceeds the maximum allowed size of {MaxFileSizeInBytes / (1024 * 1024)} MB.");
            }

            // 2. Validate extension
            var extension = Path.GetExtension(photo.FileName);
            if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
            {
                var allowedList = string.Join(", ", AllowedExtensions);
                return PhotoProcessResult.Fail($"Invalid file extension '{extension}'. Allowed image extensions are: {allowedList}");
            }

            // 3. Determine and validate content type
            var contentType = photo.ContentType;
            if (string.IsNullOrWhiteSpace(contentType) || !AllowedMimeTypes.Contains(contentType))
            {
                contentType = GetMimeTypeFromExtension(extension);
            }

            try
            {
                // 4. Read raw binary bytes into memory
                byte[] binaryData;
                using (var memoryStream = new MemoryStream())
                {
                    await photo.CopyToAsync(memoryStream);
                    binaryData = memoryStream.ToArray();
                }

                // 5. Save static file copy in wwwroot for high-performance direct serving
                var webRoot = _environment.WebRootPath;
                if (string.IsNullOrWhiteSpace(webRoot))
                {
                    webRoot = Path.Combine(_environment.ContentRootPath, "wwwroot");
                }

                var targetDirectory = Path.Combine(webRoot, "uploads", "user-photos");
                if (!Directory.Exists(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                }

                var uniqueFileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
                var fullDestinationPath = Path.Combine(targetDirectory, uniqueFileName);

                await File.WriteAllBytesAsync(fullDestinationPath, binaryData);

                var relativeUrl = $"/{UploadSubFolder}/{uniqueFileName}".Replace('\\', '/');
                _logger.LogInformation("Binary user photo processed successfully ({Bytes} bytes). Saved to {Path}", binaryData.Length, fullDestinationPath);

                return PhotoProcessResult.Ok(binaryData, contentType, photo.FileName, relativeUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process uploaded binary photo '{FileName}'.", photo.FileName);
                return PhotoProcessResult.Fail("An error occurred while processing the photo binary data.");
            }
        }

        public async Task<PhotoProcessResult> ProcessBase64PhotoAsync(string base64String, string? suggestedFileName = null)
        {
            if (string.IsNullOrWhiteSpace(base64String))
            {
                return PhotoProcessResult.Fail("Base64 photo string is empty.");
            }

            try
            {
                // Strip data URI scheme prefix if present (e.g., data:image/png;base64,...)
                var match = DataUriRegex().Match(base64String);
                var contentType = "image/jpeg";
                var cleanBase64 = base64String;

                if (match.Success)
                {
                    contentType = match.Groups["mime"].Value;
                    cleanBase64 = match.Groups["data"].Value;
                }

                var binaryData = Convert.FromBase64String(cleanBase64);
                if (binaryData.Length > MaxFileSizeInBytes)
                {
                    return PhotoProcessResult.Fail($"Base64 photo data exceeds the maximum allowed size of {MaxFileSizeInBytes / (1024 * 1024)} MB.");
                }

                var extension = GetExtensionFromMimeType(contentType);
                var fileName = !string.IsNullOrWhiteSpace(suggestedFileName) ? suggestedFileName : $"photo{extension}";

                // Save static copy
                var webRoot = _environment.WebRootPath;
                if (string.IsNullOrWhiteSpace(webRoot))
                {
                    webRoot = Path.Combine(_environment.ContentRootPath, "wwwroot");
                }

                var targetDirectory = Path.Combine(webRoot, "uploads", "user-photos");
                if (!Directory.Exists(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                }

                var uniqueFileName = $"{Guid.NewGuid():N}{extension}";
                var fullDestinationPath = Path.Combine(targetDirectory, uniqueFileName);
                await File.WriteAllBytesAsync(fullDestinationPath, binaryData);

                var relativeUrl = $"/{UploadSubFolder}/{uniqueFileName}".Replace('\\', '/');

                return PhotoProcessResult.Ok(binaryData, contentType, fileName, relativeUrl);
            }
            catch (FormatException)
            {
                return PhotoProcessResult.Fail("The provided photo string is not valid Base64 binary data.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process base64 photo.");
                return PhotoProcessResult.Fail("Failed to process Base64 binary photo.");
            }
        }

        public bool DeletePhoto(string? relativePhotoUrl)
        {
            if (string.IsNullOrWhiteSpace(relativePhotoUrl))
            {
                return true;
            }

            try
            {
                var webRoot = _environment.WebRootPath;
                if (string.IsNullOrWhiteSpace(webRoot))
                {
                    webRoot = Path.Combine(_environment.ContentRootPath, "wwwroot");
                }

                var sanitizedRelative = relativePhotoUrl.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar);
                var fullPath = Path.Combine(webRoot, sanitizedRelative);

                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    _logger.LogInformation("Deleted old user photo file: {Path}", fullPath);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete user photo file at '{RelativeUrl}'.", relativePhotoUrl);
                return false;
            }
        }

        private static string GetMimeTypeFromExtension(string extension) => extension.ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            _ => "image/jpeg"
        };

        private static string GetExtensionFromMimeType(string mimeType) => mimeType.ToLowerInvariant() switch
        {
            "image/png" => ".png",
            "image/webp" => ".webp",
            "image/gif" => ".gif",
            _ => ".jpg"
        };

        [GeneratedRegex(@"^data:(?<mime>[\w/\-\.\+]+);base64,(?<data>.+)$")]
        private static partial Regex DataUriRegex();
    }
}
