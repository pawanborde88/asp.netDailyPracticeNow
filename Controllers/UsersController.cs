using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using dailypracticeASPDotNet.DTOs;
using dailypracticeASPDotNet.Models.Domain;
using dailypracticeASPDotNet.Repositories;
using dailypracticeASPDotNet.Services;

namespace dailypracticeASPDotNet.Controllers
{
    /// <summary>
    /// Users Controller providing full CRUD and binary Photo management APIs with numeric digit user IDs (1, 2, 3...):
    /// 1. Add User with binary Photo upload (POST api/Users/add_user or POST api/Users)
    /// 2. Edit User with binary Photo update (PUT api/Users/edit_user/{id} or PUT api/Users/{id})
    /// 3. Get User Photo as raw binary file stream (GET api/Users/{id}/photo)
    /// 4. Upload / Replace User binary Photo (POST api/Users/{id}/upload_photo)
    /// 5. Delete User Photo (DELETE api/Users/{id}/photo)
    /// 6. Delete User (DELETE api/Users/delete_user/{id} or DELETE api/Users/{id})
    /// 7. Fetch All Users (GET api/Users/fetch_users or GET api/Users)
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IPhotoService _photoService;
        private readonly IMapper _mapper;
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            IUserRepository userRepository,
            IPhotoService photoService,
            IMapper mapper,
            ILogger<UsersController> logger)
        {
            _userRepository = userRepository;
            _photoService = photoService;
            _mapper = mapper;
            _logger = logger;
        }

        // =========================================================================
        // 1. ADD USER API (Accepts binary Photo Upload via multipart/form-data)
        // =========================================================================
        /// <summary>
        /// 1. Add User API - Creates a new user with validation, BCrypt password hashing, and binary photo upload.
        /// Generates a numeric integer user ID (1, 2, 3...).
        /// </summary>
        /// <param name="addUserDto">Payload containing user details and optional binary photo</param>
        /// <returns>Standardized ApiResponse containing created UserDto with integer ID</returns>
        [HttpPost("add_user")]
        [HttpPost("")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> AddUser([FromForm] AddUserRequestDto addUserDto)
        {
            return await CreateUserCoreAsync(addUserDto);
        }

        /// <summary>
        /// Add User API (JSON Overload) - Supports clients sending application/json payloads with optional Base64 binary photo.
        /// </summary>
        [HttpPost("add_user")]
        [HttpPost("")]
        [Consumes("application/json")]
        [ApiExplorerSettings(IgnoreApi = true)]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> AddUserJson([FromBody] AddUserRequestDto addUserDto)
        {
            return await CreateUserCoreAsync(addUserDto);
        }

        private async Task<IActionResult> CreateUserCoreAsync(AddUserRequestDto addUserDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.ErrorResult("Validation failed.", ModelState));
            }

            // 1. Check if email already exists
            var exists = await _userRepository.ExistsByEmailAsync(addUserDto.EmailId);
            if (exists)
            {
                return Conflict(ApiResponse.ErrorResult($"A user with email '{addUserDto.EmailId}' already exists."));
            }

            // 2. Validate that role_id exists
            var role = await _userRepository.GetRoleByIdAsync(addUserDto.RoleId);
            if (role == null)
            {
                return BadRequest(ApiResponse.ErrorResult($"Invalid role_id '{addUserDto.RoleId}'. Please provide a valid role ID (1 = Admin, 2 = User, 3 = Manager)."));
            }

            // 3. Process binary photo upload
            PhotoProcessResult? photoResult = null;

            var photoFile = addUserDto.Photo
                ?? (Request.HasFormContentType && Request.Form.Files.Count > 0
                    ? (Request.Form.Files["photo"]
                       ?? Request.Form.Files["user_photo"]
                       ?? Request.Form.Files["profile_photo"]
                       ?? Request.Form.Files["file"]
                       ?? Request.Form.Files[0])
                    : null);

            if (photoFile != null && photoFile.Length > 0)
            {
                photoResult = await _photoService.ProcessPhotoAsync(photoFile);
                if (!photoResult.Success)
                {
                    return BadRequest(ApiResponse.ErrorResult(photoResult.ErrorMessage ?? "Failed to process photo binary data."));
                }
            }
            else if (!string.IsNullOrWhiteSpace(addUserDto.PhotoBase64))
            {
                photoResult = await _photoService.ProcessBase64PhotoAsync(addUserDto.PhotoBase64);
                if (!photoResult.Success)
                {
                    return BadRequest(ApiResponse.ErrorResult(photoResult.ErrorMessage ?? "Failed to process Base64 binary photo."));
                }
            }

            // 4. Hash password securely using BCrypt
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(addUserDto.Password);

            // 5. Map DTO to User domain model (Id will be auto-generated as integer 1, 2, 3...)
            var userDomainModel = _mapper.Map<User>(addUserDto);
            userDomainModel.PasswordHash = passwordHash;
            userDomainModel.CreatedAt = DateTime.UtcNow;

            if (photoResult?.Success == true)
            {
                userDomainModel.PhotoData = photoResult.BinaryData;
                userDomainModel.PhotoContentType = photoResult.ContentType;
                userDomainModel.PhotoFileName = photoResult.FileName;
                userDomainModel.PhotoUrl = photoResult.RelativeUrl;
            }

            // 6. Asynchronously persist to database via Repository Pattern
            try
            {
                await _userRepository.CreateAsync(userDomainModel);

                // Update PhotoUrl with numeric user ID endpoint if static path was not assigned
                if (string.IsNullOrWhiteSpace(userDomainModel.PhotoUrl) && userDomainModel.PhotoData != null)
                {
                    userDomainModel.PhotoUrl = $"/api/Users/{userDomainModel.Id}/photo";
                    await _userRepository.UpdatePhotoAsync(
                        userDomainModel.Id,
                        userDomainModel.PhotoData,
                        userDomainModel.PhotoContentType,
                        userDomainModel.PhotoFileName,
                        userDomainModel.PhotoUrl);
                }
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrWhiteSpace(userDomainModel.PhotoUrl))
                {
                    _photoService.DeletePhoto(userDomainModel.PhotoUrl);
                }
                _logger.LogError(ex, "Failed to persist new user to database.");
                return StatusCode(StatusCodes.Status500InternalServerError, ApiResponse.ErrorResult("Failed to create user. Please try again."));
            }

            // 7. Map created domain model back to safe UserDto
            var userDto = _mapper.Map<UserDto>(userDomainModel);

            _logger.LogInformation("New user created successfully: {Email} (Digit ID: {Id}, PhotoBytes: {Bytes})",
                userDomainModel.EmailId, userDomainModel.Id, userDomainModel.PhotoData?.Length ?? 0);

            var response = ApiResponse<UserDto>.SuccessResult(userDto, "User added successfully.");
            return CreatedAtAction(nameof(GetUserById), new { id = userDomainModel.Id }, response);
        }

        // =========================================================================
        // 2. EDIT / UPDATE USER API (Supports numeric ID and binary Photo Update)
        // =========================================================================
        /// <summary>
        /// 2. Edit User API - Updates an existing user's details and optional binary photo by numeric ID.
        /// </summary>
        [HttpPut("edit_user/{id:int}")]
        [HttpPut("{id:int}")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> EditUser([FromRoute] int id, [FromForm] UpdateUserRequestDto updateUserDto)
        {
            return await UpdateUserCoreAsync(id, updateUserDto);
        }

        /// <summary>
        /// Edit User API (JSON Overload) - Updates user details via application/json by numeric ID.
        /// </summary>
        [HttpPut("edit_user/{id:int}")]
        [HttpPut("{id:int}")]
        [Consumes("application/json")]
        [ApiExplorerSettings(IgnoreApi = true)]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> EditUserJson([FromRoute] int id, [FromBody] UpdateUserRequestDto updateUserDto)
        {
            return await UpdateUserCoreAsync(id, updateUserDto);
        }

        private async Task<IActionResult> UpdateUserCoreAsync(int id, UpdateUserRequestDto updateUserDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.ErrorResult("Validation failed.", ModelState));
            }

            var existingUser = await _userRepository.GetByIdAsync(id);
            if (existingUser == null)
            {
                return NotFound(ApiResponse.ErrorResult($"User with ID '{id}' was not found."));
            }

            var emailExistsOnOtherUser = await _userRepository.ExistsByEmailExcludingUserAsync(updateUserDto.EmailId, id);
            if (emailExistsOnOtherUser)
            {
                return Conflict(ApiResponse.ErrorResult($"Email '{updateUserDto.EmailId}' is already in use by another user."));
            }

            var role = await _userRepository.GetRoleByIdAsync(updateUserDto.RoleId);
            if (role == null)
            {
                return BadRequest(ApiResponse.ErrorResult($"Invalid role_id '{updateUserDto.RoleId}'. Please provide a valid role ID (1 = Admin, 2 = User, 3 = Manager)."));
            }

            var photoFile = updateUserDto.Photo
                ?? (Request.HasFormContentType && Request.Form.Files.Count > 0
                    ? (Request.Form.Files["photo"]
                       ?? Request.Form.Files["user_photo"]
                       ?? Request.Form.Files["profile_photo"]
                       ?? Request.Form.Files["file"]
                       ?? Request.Form.Files[0])
                    : null);

            PhotoProcessResult? photoResult = null;
            if (photoFile != null && photoFile.Length > 0)
            {
                photoResult = await _photoService.ProcessPhotoAsync(photoFile);
                if (!photoResult.Success)
                {
                    return BadRequest(ApiResponse.ErrorResult(photoResult.ErrorMessage ?? "Failed to process photo binary data."));
                }
            }
            else if (!string.IsNullOrWhiteSpace(updateUserDto.PhotoBase64))
            {
                photoResult = await _photoService.ProcessBase64PhotoAsync(updateUserDto.PhotoBase64);
                if (!photoResult.Success)
                {
                    return BadRequest(ApiResponse.ErrorResult(photoResult.ErrorMessage ?? "Failed to process Base64 binary photo."));
                }
            }

            var updatedDomainModel = _mapper.Map<User>(updateUserDto);
            updatedDomainModel.Id = id;

            if (photoResult?.Success == true)
            {
                if (!string.IsNullOrWhiteSpace(existingUser.PhotoUrl))
                {
                    _photoService.DeletePhoto(existingUser.PhotoUrl);
                }

                updatedDomainModel.PhotoData = photoResult.BinaryData;
                updatedDomainModel.PhotoContentType = photoResult.ContentType;
                updatedDomainModel.PhotoFileName = photoResult.FileName;
                updatedDomainModel.PhotoUrl = photoResult.RelativeUrl ?? $"/api/Users/{id}/photo";
            }
            else
            {
                updatedDomainModel.PhotoData = existingUser.PhotoData;
                updatedDomainModel.PhotoContentType = existingUser.PhotoContentType;
                updatedDomainModel.PhotoFileName = existingUser.PhotoFileName;
                updatedDomainModel.PhotoUrl = existingUser.PhotoUrl;
            }

            if (!string.IsNullOrWhiteSpace(updateUserDto.Password))
            {
                updatedDomainModel.PasswordHash = BCrypt.Net.BCrypt.HashPassword(updateUserDto.Password);
            }
            else
            {
                updatedDomainModel.PasswordHash = existingUser.PasswordHash;
            }

            var result = await _userRepository.UpdateAsync(id, updatedDomainModel);
            if (result == null)
            {
                return NotFound(ApiResponse.ErrorResult($"User with ID '{id}' was not found."));
            }

            var userDto = _mapper.Map<UserDto>(result);

            _logger.LogInformation("User updated successfully: {Email} (Digit ID: {Id})", result.EmailId, result.Id);

            return Ok(ApiResponse<UserDto>.SuccessResult(userDto, "User updated successfully."));
        }

        // =========================================================================
        // 3. GET USER PHOTO (Raw Binary Stream for <img> tags and browsers)
        // =========================================================================
        /// <summary>
        /// 3. Get User Photo API - Streams the raw binary image data directly to the browser or client by numeric user ID.
        /// Can be used directly in HTML image tags, e.g. &lt;img src="/api/Users/1/photo" /&gt;
        /// </summary>
        /// <param name="id">Numeric digit ID of the user (e.g. 1, 2, 3)</param>
        /// <returns>Binary file stream with appropriate image MIME content type (e.g. image/jpeg, image/png)</returns>
        [HttpGet("{id:int}/photo")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUserPhoto([FromRoute] int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound(ApiResponse.ErrorResult($"User with ID '{id}' was not found."));
            }

            if (user.PhotoData == null || user.PhotoData.Length == 0)
            {
                return NotFound(ApiResponse.ErrorResult("This user does not have a profile photo uploaded."));
            }

            var contentType = !string.IsNullOrWhiteSpace(user.PhotoContentType)
                ? user.PhotoContentType
                : "image/jpeg";

            var fileName = !string.IsNullOrWhiteSpace(user.PhotoFileName)
                ? user.PhotoFileName
                : "profile.jpg";

            return File(user.PhotoData, contentType, fileName);
        }

        // =========================================================================
        // 4. DEDICATED PHOTO MANAGEMENT ENDPOINTS
        // =========================================================================
        /// <summary>
        /// Uploads or updates the binary profile photo for an existing user by numeric ID.
        /// </summary>
        /// <param name="id">Numeric digit ID of the target user</param>
        /// <param name="photo">Binary image file (.jpg, .jpeg, .png, .webp, .gif, max 5MB)</param>
        [HttpPost("{id:int}/upload_photo")]
        [HttpPost("{id:int}/photo")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UploadUserPhoto([FromRoute] int id, IFormFile photo)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound(ApiResponse.ErrorResult($"User with ID '{id}' was not found."));
            }

            if (photo == null || photo.Length == 0)
            {
                return BadRequest(ApiResponse.ErrorResult("Please select an image file to upload."));
            }

            var photoResult = await _photoService.ProcessPhotoAsync(photo);
            if (!photoResult.Success)
            {
                return BadRequest(ApiResponse.ErrorResult(photoResult.ErrorMessage ?? "Failed to process photo binary data."));
            }

            if (!string.IsNullOrWhiteSpace(user.PhotoUrl))
            {
                _photoService.DeletePhoto(user.PhotoUrl);
            }

            var updatedUser = await _userRepository.UpdatePhotoAsync(
                id,
                photoResult.BinaryData,
                photoResult.ContentType,
                photoResult.FileName,
                photoResult.RelativeUrl ?? $"/api/Users/{id}/photo");

            var userDto = _mapper.Map<UserDto>(updatedUser);

            return Ok(ApiResponse<UserDto>.SuccessResult(userDto, "User binary photo uploaded and saved successfully."));
        }

        /// <summary>
        /// Deletes the profile photo of an existing user and removes binary data and disk cache by numeric ID.
        /// </summary>
        /// <param name="id">Numeric digit ID of the target user</param>
        [HttpDelete("{id:int}/photo")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteUserPhoto([FromRoute] int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound(ApiResponse.ErrorResult($"User with ID '{id}' was not found."));
            }

            if (!string.IsNullOrWhiteSpace(user.PhotoUrl))
            {
                _photoService.DeletePhoto(user.PhotoUrl);
            }

            var updatedUser = await _userRepository.UpdatePhotoAsync(id, null, null, null, null);
            var userDto = _mapper.Map<UserDto>(updatedUser);

            return Ok(ApiResponse<UserDto>.SuccessResult(userDto, "User photo removed successfully."));
        }

        // =========================================================================
        // 5. DELETE USER API
        // =========================================================================
        /// <summary>
        /// 5. Delete User API - Permanently deletes a user from the system by numeric digit ID.
        /// </summary>
        /// <param name="id">Numeric digit ID of the user</param>
        [HttpDelete("delete_user/{id:int}")]
        [HttpDelete("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteUser([FromRoute] int id)
        {
            var existingUser = await _userRepository.GetByIdAsync(id);
            if (existingUser == null)
            {
                return NotFound(ApiResponse.ErrorResult($"User with ID '{id}' was not found."));
            }

            if (!string.IsNullOrWhiteSpace(existingUser.PhotoUrl))
            {
                _photoService.DeletePhoto(existingUser.PhotoUrl);
            }

            var deletedUser = await _userRepository.DeleteAsync(id);
            if (deletedUser == null)
            {
                return NotFound(ApiResponse.ErrorResult($"User with ID '{id}' was not found."));
            }

            var userDto = _mapper.Map<UserDto>(deletedUser);

            _logger.LogInformation("User deleted successfully: {Email} (Digit ID: {Id})", deletedUser.EmailId, deletedUser.Id);

            return Ok(ApiResponse<UserDto>.SuccessResult(userDto, "User deleted successfully."));
        }

        // =========================================================================
        // 6. FETCH ALL USERS API
        // =========================================================================
        /// <summary>
        /// 6. Fetch All Users API - Retrieves all users with their numeric IDs, roles, and photo URLs.
        /// </summary>
        [HttpGet("fetch_users")]
        [HttpGet("")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<UserDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> FetchUsers()
        {
            var users = await _userRepository.GetAllAsync();
            var userDtos = _mapper.Map<IEnumerable<UserDto>>(users);

            return Ok(ApiResponse<IEnumerable<UserDto>>.SuccessResult(userDtos, "Users fetched successfully."));
        }

        // =========================================================================
        // HELPER / DETAIL ENDPOINTS
        // =========================================================================
        /// <summary>
        /// Retrieves a single user by numeric digit ID (e.g. 1, 2, 3...).
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUserById([FromRoute] int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound(ApiResponse.ErrorResult($"User with ID '{id}' was not found."));
            }

            var userDto = _mapper.Map<UserDto>(user);
            return Ok(ApiResponse<UserDto>.SuccessResult(userDto, "User fetched successfully."));
        }

        /// <summary>
        /// Retrieves all available roles in the system (1 = Admin, 2 = User, 3 = Manager).
        /// </summary>
        [HttpGet("roles")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<RoleDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRoles()
        {
            var roles = await _userRepository.GetAllRolesAsync();
            var roleDtos = _mapper.Map<IEnumerable<RoleDto>>(roles);
            return Ok(ApiResponse<IEnumerable<RoleDto>>.SuccessResult(roleDtos, "Roles fetched successfully."));
        }
    }
}
