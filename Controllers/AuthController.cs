using System.Security.Claims;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using dailypracticeASPDotNet.DTOs;
using dailypracticeASPDotNet.Repositories;

namespace dailypracticeASPDotNet.Controllers
{
    /// <summary>
    /// Authentication Controller providing Login API with JWT token generation and authorized profile endpoints.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly ITokenRepository _tokenRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IUserRepository userRepository,
            ITokenRepository tokenRepository,
            IMapper mapper,
            ILogger<AuthController> logger)
        {
            _userRepository = userRepository;
            _tokenRepository = tokenRepository;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// Login API - Authenticates using only Email and Password.
        /// Returns JWT Bearer token upon success.
        /// </summary>
        /// <param name="loginRequestDto">Payload containing strictly Email and Password</param>
        /// <returns>JWT Bearer token and authenticated user details</returns>
        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto loginRequestDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var emailToValidate = loginRequestDto.NormalizedEmail;
            if (string.IsNullOrWhiteSpace(emailToValidate))
            {
                return BadRequest(new { message = "Email is required." });
            }

            // 1. Asynchronously retrieve the user by email using the Repository Pattern
            var user = await _userRepository.GetByEmailAsync(emailToValidate);
            if (user == null)
            {
                _logger.LogWarning("Login failed: User with email {Email} not found.", emailToValidate);
                return Unauthorized(new { message = "Invalid email or password." });
            }

            // 2. Verify hashed password securely using BCrypt
            var isPasswordValid = BCrypt.Net.BCrypt.Verify(loginRequestDto.Password, user.PasswordHash);
            if (!isPasswordValid)
            {
                _logger.LogWarning("Login failed: Incorrect password for user {Email}.", emailToValidate);
                return Unauthorized(new { message = "Invalid email or password." });
            }

            // 3. Generate JWT Token with claims
            var token = _tokenRepository.CreateJwtToken(user, out var expiresAt);

            // 4. Map Domain Model (User) to UserDto using AutoMapper
            var userDto = _mapper.Map<UserDto>(user);

            var response = new LoginResponseDto
            {
                Token = token,
                TokenType = "Bearer",
                ExpiresAt = expiresAt,
                User = userDto
            };

            _logger.LogInformation("User {Email} logged in successfully.", user.EmailId);
            return Ok(response);
        }

        /// <summary>
        /// Protected endpoint requiring JWT Authorization token via [Authorize].
        /// Returns profile of the current authenticated user.
        /// </summary>
        [HttpGet("profile")]
        [Authorize]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetProfile()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid token claims." });
            }

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }

            var userDto = _mapper.Map<UserDto>(user);
            return Ok(userDto);
        }

        /// <summary>
        /// Protected endpoint verifying that the Bearer token is valid and active.
        /// </summary>
        [HttpGet("verify_token")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult VerifyToken()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = User.FindFirstValue(ClaimTypes.Email);
            var role = User.FindFirstValue(ClaimTypes.Role);
            var name = User.FindFirstValue(ClaimTypes.Name);

            return Ok(new
            {
                status = "Authorized",
                message = "JWT Token is valid and authenticated.",
                userId,
                email,
                name,
                role
            });
        }
    }
}
