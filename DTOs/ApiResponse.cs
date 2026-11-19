using System.Text.Json.Serialization;

namespace dailypracticeASPDotNet.DTOs
{
    /// <summary>
    /// Standardized API Response envelope returned by all endpoints.
    /// Format:
    /// {
    ///   "success": true/false,
    ///   "message": "Human-readable message",
    ///   "data": { ... } or [ ... ] or null,
    ///   "errors": { ... } or null
    /// }
    /// </summary>
    /// <typeparam name="T">Type of the data payload</typeparam>
    public class ApiResponse<T>
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public T? Data { get; set; }

        [JsonPropertyName("errors")]
        public object? Errors { get; set; }

        public ApiResponse()
        {
        }

        public ApiResponse(bool success, string message, T? data = default, object? errors = null)
        {
            Success = success;
            Message = message;
            Data = data;
            Errors = errors;
        }

        public static ApiResponse<T> SuccessResult(T data, string message = "Operation completed successfully.")
        {
            return new ApiResponse<T>(true, message, data, null);
        }

        public static ApiResponse<T> ErrorResult(string message, object? errors = null)
        {
            return new ApiResponse<T>(false, message, default, errors);
        }
    }

    /// <summary>
    /// Non-generic convenience variant of ApiResponse for untyped or void responses.
    /// </summary>
    public class ApiResponse : ApiResponse<object>
    {
        public ApiResponse()
        {
        }

        public ApiResponse(bool success, string message, object? data = null, object? errors = null)
            : base(success, message, data, errors)
        {
        }

        public static ApiResponse SuccessResult(string message = "Operation completed successfully.", object? data = null)
        {
            return new ApiResponse(true, message, data, null);
        }

        public static new ApiResponse ErrorResult(string message, object? errors = null)
        {
            return new ApiResponse(false, message, null, errors);
        }
    }
}

