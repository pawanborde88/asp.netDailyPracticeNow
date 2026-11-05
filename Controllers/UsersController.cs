using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using dailypracticeASPDotNet.DTOs;
using dailypracticeASPDotNet.Models.Domain;
using dailypracticeASPDotNet.Repositories;

namespace dailypracticeASPDotNet.Controllers
{
    /// <summary>
    /// Users Controller providing add_user and fetch_users APIs.
    /// Demonstrates Repository Pattern, AutoMapper, and async/await methods.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            IUserRepository userRepository,
            IMapper mapper,
            ILogger<UsersController> logger)
        {
            _userRepository = userRepository;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// add_user API - Creates a new user with first_name, middle_name, last_name, role_id, password, address, mobile_no, and email_id.
        /// Hashes password with BCrypt and persists asynchronously via repository.
        /// </summary>
        /// <param name="addUserDto">Payload containing user details</param>
        /// <returns>Created user representation without sensitive fields</returns>
        [HttpPost("add_user")]
        [HttpPost("")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> AddUser([FromBody] AddUserRequestDto addUserDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // 1. Check if email already exists
            var exists = await _userRepository.ExistsByEmailAsync(addUserDto.EmailId);
            if (exists)
            {
                return Conflict(new { message = $"A user with email '{addUserDto.EmailId}' already exists." });
            }

            // 2. Validate that role_id exists
            var role = await _userRepository.GetRoleByIdAsync(addUserDto.RoleId);
            if (role == null)
            {
                return BadRequest(new { message = $"Invalid role_id '{addUserDto.RoleId}'. Please provide a valid role ID (1 = Admin, 2 = User, 3 = Manager)." });
            }

            // 3. Hash password securely using BCrypt
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(addUserDto.Password);

            // 4. Map DTO to User domain model using AutoMapper
            var userDomainModel = _mapper.Map<User>(addUserDto);
            userDomainModel.Id = Guid.NewGuid();
            userDomainModel.PasswordHash = passwordHash;
            userDomainModel.CreatedAt = DateTime.UtcNow;

            // 5. Asynchronously persist to database via Repository Pattern
            await _userRepository.CreateAsync(userDomainModel);

            // 6. Map created domain model back to safe UserDto using AutoMapper
            var userDto = _mapper.Map<UserDto>(userDomainModel);

            _logger.LogInformation("New user created successfully: {Email} (ID: {Id})", userDomainModel.EmailId, userDomainModel.Id);

            return CreatedAtAction(nameof(GetUserById), new { id = userDomainModel.Id }, userDto);
        }

        /// <summary>
        /// fetch_users API - Asynchronously retrieves all users with their roles from SQL Server.
        /// </summary>
        /// <returns>List of users</returns>
        [HttpGet("fetch_users")]
        [HttpGet("")]
        [ProducesResponseType(typeof(IEnumerable<UserDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> FetchUsers()
        {
            // 1. Asynchronously retrieve all users via Repository Pattern
            var users = await _userRepository.GetAllAsync();

            // 2. Map domain models to UserDto collection using AutoMapper
            var userDtos = _mapper.Map<IEnumerable<UserDto>>(users);

            return Ok(userDtos);
        }

        /// <summary>
        /// Retrieves a single user by primary key ID.
        /// </summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUserById([FromRoute] Guid id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { message = $"User with ID '{id}' was not found." });
            }

            var userDto = _mapper.Map<UserDto>(user);
            return Ok(userDto);
        }

        /// <summary>
        /// Retrieves all available roles in the system.
        /// </summary>
        [HttpGet("roles")]
        [ProducesResponseType(typeof(IEnumerable<RoleDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRoles()
        {
            var roles = await _userRepository.GetAllRolesAsync();
            var roleDtos = _mapper.Map<IEnumerable<RoleDto>>(roles);
            return Ok(roleDtos);
        }
    }
}
