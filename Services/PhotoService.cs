namespace dailypracticeASPDotNet.Services
{
    /// <summary>
    /// Implementation of IPhotoService providing secure file validation, unique naming,
    /// disk persistence in wwwroot/uploads/user-photos, and file cleanup.
    /// </summary>
    public class PhotoService : IPhotoService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<PhotoService> _logger;

        // Allowed image extensions (case-insensitive)
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp", ".gif"
        };

        // Allowed image MIME types
        private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/png", "image/webp", "image/gif", "image/pjpeg"
        };

        // Maximum allowed file size: 5 MB
        private const long MaxFileSizeInBytes = 5 * 1024 * 1024;

        // Relative folder path inside wwwroot
        private const string UploadSubFolder = "uploads/user-photos";

        public PhotoService(IWebHostEnvironment environment, ILogger<PhotoService> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        public async Task<(bool Success, string? PhotoUrl, string? ErrorMessage)> SavePhotoAsync(IFormFile photo)
        {
            if (photo == null || photo.Length == 0)
            {
                return (false, null, "No photo file was provided or file is empty.");
            }

            // 1. Validate file size
            if (photo.Length > MaxFileSizeInBytes)
            {
                return (false, null, $"Photo file exceeds the maximum allowed size of {MaxFileSizeInBytes / (1024 * 1024)} MB.");
            }

            // 2. Validate file extension
            var extension = Path.GetExtension(photo.FileName);
            if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
            {
                var allowedList = string.Join(", ", AllowedExtensions);
                return (false, null, $"Invalid file extension '{extension}'. Allowed image extensions are: {allowedList}");
            }

            // 3. Validate content type (MIME type)
            if (!string.IsNullOrWhiteSpace(photo.ContentType) && !AllowedMimeTypes.Contains(photo.ContentType))
            {
                return (false, null, $"Invalid content type '{photo.ContentType}'. Only image files are permitted.");
            }

            try
            {
                // 4. Resolve destination directory inside wwwroot
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

                // 5. Generate collision-resistant unique filename (GUID + lowercase extension)
                var uniqueFileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
                var fullDestinationPath = Path.Combine(targetDirectory, uniqueFileName);

                // 6. Asynchronously stream and save file to disk
                await using (var fileStream = new FileStream(fullDestinationPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await photo.CopyToAsync(fileStream);
                }

                // 7. Format normalized relative URL for client consumption (e.g. /uploads/user-photos/{fileName})
                var relativeUrl = $"/{UploadSubFolder}/{uniqueFileName}".Replace('\\', '/');
                _logger.LogInformation("User photo saved successfully to {Path} (URL: {Url})", fullDestinationPath, relativeUrl);

                return (true, relativeUrl, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while saving user photo '{FileName}'.", photo.FileName);
                return (false, null, "An unexpected error occurred while saving the photo. Please try again.");
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

                // Clean relative URL to avoid directory traversal
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
    }
}

