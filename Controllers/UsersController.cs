using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using dailypracticeASPDotNet.DTOs;
using dailypracticeASPDotNet.Models.Domain;
using dailypracticeASPDotNet.Repositories;
using dailypracticeASPDotNet.Services;

namespace dailypracticeASPDotNet.Controllers
{
    /// <summary>
    /// Users Controller providing full CRUD and Photo management APIs with a standardized ApiResponse envelope:
    /// 1. Add User with Photo upload (POST api/Users/add_user or POST api/Users)
    /// 2. Edit User with Photo update (PUT api/Users/edit_user/{id} or PUT api/Users/{id})
    /// 3. Upload / Replace User Photo (POST api/Users/{id}/upload_photo)
    /// 4. Delete User Photo (DELETE api/Users/{id}/photo)
    /// 5. Delete User (DELETE api/Users/delete_user/{id} or DELETE api/Users/{id})
    /// 6. Fetch All Users (GET api/Users/fetch_users or GET api/Users)
    /// Format: { "success": true, "message": "...", "data": ..., "errors": null }
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
        // 1. ADD USER API (Supports Multipart Form Data with Photo Upload & JSON)
        // =========================================================================
        /// <summary>
        /// 1. Add User API - Creates a new user with validation, BCrypt password hashing, and optional photo upload.
        /// Accepts multipart/form-data with user details and photo file (.jpg, .jpeg, .png, .webp, .gif, max 5MB).
        /// </summary>
        /// <param name="addUserDto">Payload containing user details and optional photo</param>
        /// <returns>Standardized ApiResponse containing created UserDto</returns>
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
        /// Add User API (JSON Overload) - Supports clients sending application/json payloads.
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

            // 3. Process photo upload if provided in form data
            string? savedPhotoUrl = null;
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
                var (saveSuccess, photoUrl, errorMessage) = await _photoService.SavePhotoAsync(photoFile);
                if (!saveSuccess)
                {
                    return BadRequest(ApiResponse.ErrorResult(errorMessage ?? "Failed to save user photo."));
                }
                savedPhotoUrl = photoUrl;
            }
            else if (!string.IsNullOrWhiteSpace(addUserDto.PhotoUrl))
            {
                savedPhotoUrl = addUserDto.PhotoUrl.Trim();
            }

            // 4. Hash password securely using BCrypt
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(addUserDto.Password);

            // 5. Map DTO to User domain model using AutoMapper
            var userDomainModel = _mapper.Map<User>(addUserDto);
            userDomainModel.Id = Guid.NewGuid();
            userDomainModel.PasswordHash = passwordHash;
            userDomainModel.CreatedAt = DateTime.UtcNow;
            userDomainModel.PhotoUrl = savedPhotoUrl;

            // 6. Asynchronously persist to database via Repository Pattern
            try
            {
                await _userRepository.CreateAsync(userDomainModel);
            }
            catch (Exception ex)
            {
                // If database persistence fails, clean up the uploaded photo file
                if (!string.IsNullOrWhiteSpace(savedPhotoUrl))
                {
                    _photoService.DeletePhoto(savedPhotoUrl);
                }
                _logger.LogError(ex, "Failed to persist new user to database.");
                return StatusCode(StatusCodes.Status500InternalServerError, ApiResponse.ErrorResult("Failed to create user. Please try again."));
            }

            // 7. Map created domain model back to safe UserDto using AutoMapper
            var userDto = _mapper.Map<UserDto>(userDomainModel);

            _logger.LogInformation("New user created successfully: {Email} (ID: {Id}, Photo: {Photo})",
                userDomainModel.EmailId, userDomainModel.Id, userDomainModel.PhotoUrl ?? "None");

            var response = ApiResponse<UserDto>.SuccessResult(userDto, "User added successfully.");
            return CreatedAtAction(nameof(GetUserById), new { id = userDomainModel.Id }, response);
        }

        // =========================================================================
        // 2. EDIT / UPDATE USER API (Supports Multipart Form Data with Photo & JSON)
        // =========================================================================
        /// <summary>
        /// 2. Edit User API - Updates an existing user's details and optional photo by ID.
        /// </summary>
        /// <param name="id">Unique GUID identifier of the user</param>
        /// <param name="updateUserDto">Updated user information and optional photo file</param>
        /// <returns>Standardized ApiResponse containing updated UserDto</returns>
        [HttpPut("edit_user/{id:guid}")]
        [HttpPut("{id:guid}")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> EditUser([FromRoute] Guid id, [FromForm] UpdateUserRequestDto updateUserDto)
        {
            return await UpdateUserCoreAsync(id, updateUserDto);
        }

        /// <summary>
        /// Edit User API (JSON Overload) - Updates user details via application/json payload.
        /// </summary>
        [HttpPut("edit_user/{id:guid}")]
        [HttpPut("{id:guid}")]
        [Consumes("application/json")]
        [ApiExplorerSettings(IgnoreApi = true)]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> EditUserJson([FromRoute] Guid id, [FromBody] UpdateUserRequestDto updateUserDto)
        {
            return await UpdateUserCoreAsync(id, updateUserDto);
        }

        private async Task<IActionResult> UpdateUserCoreAsync(Guid id, UpdateUserRequestDto updateUserDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.ErrorResult("Validation failed.", ModelState));
            }

            // 1. Verify user exists
            var existingUser = await _userRepository.GetByIdAsync(id);
            if (existingUser == null)
            {
                return NotFound(ApiResponse.ErrorResult($"User with ID '{id}' was not found."));
            }

            // 2. Validate email uniqueness (ensure no other user already uses this email)
            var emailExistsOnOtherUser = await _userRepository.ExistsByEmailExcludingUserAsync(updateUserDto.EmailId, id);
            if (emailExistsOnOtherUser)
            {
                return Conflict(ApiResponse.ErrorResult($"Email '{updateUserDto.EmailId}' is already in use by another user."));
            }

            // 3. Validate role_id
            var role = await _userRepository.GetRoleByIdAsync(updateUserDto.RoleId);
            if (role == null)
            {
                return BadRequest(ApiResponse.ErrorResult($"Invalid role_id '{updateUserDto.RoleId}'. Please provide a valid role ID (1 = Admin, 2 = User, 3 = Manager)."));
            }

            // 4. Handle photo file update if provided
            var photoFile = updateUserDto.Photo
                ?? (Request.HasFormContentType && Request.Form.Files.Count > 0
                    ? (Request.Form.Files["photo"]
                       ?? Request.Form.Files["user_photo"]
                       ?? Request.Form.Files["profile_photo"]
                       ?? Request.Form.Files["file"]
                       ?? Request.Form.Files[0])
                    : null);

            string? finalPhotoUrl = existingUser.PhotoUrl;
            if (photoFile != null && photoFile.Length > 0)
            {
                var (saveSuccess, photoUrl, errorMessage) = await _photoService.SavePhotoAsync(photoFile);
                if (!saveSuccess)
                {
                    return BadRequest(ApiResponse.ErrorResult(errorMessage ?? "Failed to save updated photo."));
                }

                // Delete previous photo file from disk if different
                if (!string.IsNullOrWhiteSpace(existingUser.PhotoUrl))
                {
                    _photoService.DeletePhoto(existingUser.PhotoUrl);
                }

                finalPhotoUrl = photoUrl;
            }
            else if (!string.IsNullOrWhiteSpace(updateUserDto.PhotoUrl))
            {
                finalPhotoUrl = updateUserDto.PhotoUrl.Trim();
            }

            // 5. Map DTO to Domain Model
            var updatedDomainModel = _mapper.Map<User>(updateUserDto);
            updatedDomainModel.Id = id;
            updatedDomainModel.PhotoUrl = finalPhotoUrl;

            // 6. If new password provided, hash with BCrypt; otherwise keep existing hash
            if (!string.IsNullOrWhiteSpace(updateUserDto.Password))
            {
                updatedDomainModel.PasswordHash = BCrypt.Net.BCrypt.HashPassword(updateUserDto.Password);
            }
            else
            {
                updatedDomainModel.PasswordHash = existingUser.PasswordHash;
            }

            // 7. Asynchronously update via Repository
            var result = await _userRepository.UpdateAsync(id, updatedDomainModel);
            if (result == null)
            {
                return NotFound(ApiResponse.ErrorResult($"User with ID '{id}' was not found."));
            }

            // 8. Map to UserDto
            var userDto = _mapper.Map<UserDto>(result);

            _logger.LogInformation("User updated successfully: {Email} (ID: {Id})", result.EmailId, result.Id);

            return Ok(ApiResponse<UserDto>.SuccessResult(userDto, "User updated successfully."));
        }

        // =========================================================================
        // 3. DEDICATED PHOTO MANAGEMENT ENDPOINTS
        // =========================================================================
        /// <summary>
        /// Uploads or updates the profile photo for an existing user.
        /// </summary>
        /// <param name="id">GUID of the target user</param>
        /// <param name="photo">Image file (.jpg, .jpeg, .png, .webp, .gif, max 5MB)</param>
        [HttpPost("{id:guid}/upload_photo")]
        [HttpPost("{id:guid}/photo")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UploadUserPhoto([FromRoute] Guid id, IFormFile photo)
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

            var (saveSuccess, photoUrl, errorMessage) = await _photoService.SavePhotoAsync(photo);
            if (!saveSuccess)
            {
                return BadRequest(ApiResponse.ErrorResult(errorMessage ?? "Failed to save photo."));
            }

            // Delete old photo if existed
            if (!string.IsNullOrWhiteSpace(user.PhotoUrl))
            {
                _photoService.DeletePhoto(user.PhotoUrl);
            }

            var updatedUser = await _userRepository.UpdatePhotoAsync(id, photoUrl);
            var userDto = _mapper.Map<UserDto>(updatedUser);

            return Ok(ApiResponse<UserDto>.SuccessResult(userDto, "User photo uploaded successfully."));
        }

        /// <summary>
        /// Deletes the profile photo of an existing user and removes the file from disk.
        /// </summary>
        /// <param name="id">GUID of the target user</param>
        [HttpDelete("{id:guid}/photo")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteUserPhoto([FromRoute] Guid id)
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

            var updatedUser = await _userRepository.UpdatePhotoAsync(id, null);
            var userDto = _mapper.Map<UserDto>(updatedUser);

            return Ok(ApiResponse<UserDto>.SuccessResult(userDto, "User photo removed successfully."));
        }

        // =========================================================================
        // 4. DELETE USER API
        // =========================================================================
        /// <summary>
        /// 4. Delete User API - Permanently deletes a user from the system by ID and removes any associated photo file.
        /// </summary>
        /// <param name="id">Unique GUID identifier of the user</param>
        /// <returns>Standardized ApiResponse containing deleted UserDto</returns>
        [HttpDelete("delete_user/{id:guid}")]
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteUser([FromRoute] Guid id)
        {
            // 1. Verify user exists
            var existingUser = await _userRepository.GetByIdAsync(id);
            if (existingUser == null)
            {
                return NotFound(ApiResponse.ErrorResult($"User with ID '{id}' was not found."));
            }

            // 2. Clean up user's photo file from disk if present
            if (!string.IsNullOrWhiteSpace(existingUser.PhotoUrl))
            {
                _photoService.DeletePhoto(existingUser.PhotoUrl);
            }

            // 3. Delete asynchronously via Repository
            var deletedUser = await _userRepository.DeleteAsync(id);
            if (deletedUser == null)
            {
                return NotFound(ApiResponse.ErrorResult($"User with ID '{id}' was not found."));
            }

            var userDto = _mapper.Map<UserDto>(deletedUser);

            _logger.LogInformation("User deleted successfully: {Email} (ID: {Id})", deletedUser.EmailId, deletedUser.Id);

            return Ok(ApiResponse<UserDto>.SuccessResult(userDto, "User deleted successfully."));
        }

        // =========================================================================
        // 5. FETCH ALL USERS API
        // =========================================================================
        /// <summary>
        /// 5. Fetch All Users API - Retrieves all users with their roles and photo URLs.
        /// </summary>
        /// <returns>Standardized ApiResponse containing list of UserDto</returns>
        [HttpGet("fetch_users")]
        [HttpGet("")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<UserDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> FetchUsers()
        {
            // 1. Asynchronously retrieve all users via Repository Pattern
            var users = await _userRepository.GetAllAsync();

            // 2. Map domain models to UserDto collection using AutoMapper
            var userDtos = _mapper.Map<IEnumerable<UserDto>>(users);

            return Ok(ApiResponse<IEnumerable<UserDto>>.SuccessResult(userDtos, "Users fetched successfully."));
        }

        // =========================================================================
        // HELPER / DETAIL ENDPOINTS
        // =========================================================================
        /// <summary>
        /// Retrieves a single user by primary key GUID ID.
        /// </summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUserById([FromRoute] Guid id)
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
