using System.Security.Claims;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using dailypracticeASPDotNet.DTOs;
using dailypracticeASPDotNet.Repositories;
using dailypracticeASPDotNet.Services;

namespace dailypracticeASPDotNet.Controllers
{
    /// <summary>
    /// Authentication Controller providing:
    /// 1. Standard Password Login (POST api/Auth/login)
    /// 2. Request Login OTP via ZeptoMail (POST api/Auth/send-otp) - Validates email exists in system first!
    /// 3. Verify OTP & Login (POST api/Auth/login-with-otp or api/Auth/verify-otp) - Generates JWT token upon success
    /// 4. Authorized Profile & Token Verification
    /// All endpoints return the standardized ApiResponse envelope:
    /// { "success": true/false, "message": "...", "data": ..., "errors": ... }
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly ITokenRepository _tokenRepository;
        private readonly IMapper _mapper;
        private readonly IOtpService _otpService;
        private readonly IZeptoMailService _zeptoMailService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IUserRepository userRepository,
            ITokenRepository tokenRepository,
            IMapper mapper,
            IOtpService otpService,
            IZeptoMailService zeptoMailService,
            ILogger<AuthController> logger)
        {
            _userRepository = userRepository;
            _tokenRepository = tokenRepository;
            _mapper = mapper;
            _otpService = otpService;
            _zeptoMailService = zeptoMailService;
            _logger = logger;
        }

        // =========================================================================
        // 1. STANDARD PASSWORD LOGIN
        // =========================================================================
        /// <summary>
        /// Login API - Authenticates using Email and Password.
        /// Returns JWT Bearer token upon success inside ApiResponse envelope.
        /// </summary>
        /// <param name="loginRequestDto">Payload containing Email and Password</param>
        /// <returns>Standardized ApiResponse containing JWT Bearer token and user profile</returns>
        [HttpPost("login")]
        [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto loginRequestDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.ErrorResult("Validation failed.", ModelState));
            }

            var emailToValidate = loginRequestDto.NormalizedEmail;
            if (string.IsNullOrWhiteSpace(emailToValidate))
            {
                return BadRequest(ApiResponse.ErrorResult("Email is required."));
            }

            // 1. Asynchronously retrieve the user by email using the Repository Pattern
            var user = await _userRepository.GetByEmailAsync(emailToValidate);
            if (user == null)
            {
                _logger.LogWarning("Login failed: User with email {Email} not found.", emailToValidate);
                return Unauthorized(ApiResponse.ErrorResult("Invalid email or password."));
            }

            // 2. Verify hashed password securely using BCrypt
            var isPasswordValid = BCrypt.Net.BCrypt.Verify(loginRequestDto.Password, user.PasswordHash);
            if (!isPasswordValid)
            {
                _logger.LogWarning("Login failed: Incorrect password for user {Email}.", emailToValidate);
                return Unauthorized(ApiResponse.ErrorResult("Invalid email or password."));
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

            _logger.LogInformation("User {Email} logged in successfully via password.", user.EmailId);
            return Ok(ApiResponse<LoginResponseDto>.SuccessResult(response, "User logged in successfully."));
        }

        // =========================================================================
        // 2. REQUEST LOGIN OTP VIA ZEPTOMAIL (SMTP / API)
        // =========================================================================
        /// <summary>
        /// Request Login OTP API - Sends a 6-digit verification code to the user's email.
        /// Enforces that the entered email MUST already be registered in our system.
        /// </summary>
        /// <param name="request">Payload containing user's registered email address</param>
        /// <returns>Standardized ApiResponse containing dispatch status</returns>
        [HttpPost("send-otp")]
        [HttpPost("send_otp")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SendOtp([FromBody] SendOtpRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.ErrorResult("Validation failed.", ModelState));
            }

            var normalizedEmail = request.Email.Trim().ToLowerInvariant();

            // 1. CHECK IF ENTERED EMAIL IS REGISTERED IN OUR SYSTEM
            var user = await _userRepository.GetByEmailAsync(normalizedEmail);
            if (user == null)
            {
                _logger.LogWarning("OTP request rejected: Email '{Email}' is not registered in our system.", normalizedEmail);
                return NotFound(ApiResponse.ErrorResult(
                    $"No registered account was found with email '{request.Email}'. Please check the email address or register first."));
            }

            // 2. GENERATE 6-DIGIT SECURE OTP (Valid for 5 minutes)
            const int expiryMinutes = 5;
            var otpCode = _otpService.GenerateOtp(user.EmailId, expiryMinutes);

            // 3. DISPATCH EMAIL VIA ZOHO CPAAS / ZEPTOMAIL (SMTP OR REST API)
            var (isEmailSent, emailStatusMessage) = await _zeptoMailService.SendOtpEmailAsync(
                user.EmailId,
                user.FullName,
                otpCode,
                expiryMinutes);

            var resultData = new
            {
                email = user.EmailId,
                expires_in_minutes = expiryMinutes
            };

            if (isEmailSent)
            {
                return Ok(ApiResponse.SuccessResult(
                    $"OTP has been sent successfully to '{user.EmailId}'. Please check your inbox or spam folder.",
                    resultData));
            }

            // If ZeptoMail dispatch encountered an issue (e.g. domain pending verification or credit exhausted):
            _logger.LogWarning("OTP email via ZeptoMail had a notice for '{Email}': {Status}", user.EmailId, emailStatusMessage);

            return Ok(new ApiResponse<object>(
                true,
                $"OTP generated successfully! Code: {otpCode} (ZeptoMail notice: {emailStatusMessage})",
                new
                {
                    email = user.EmailId,
                    expires_in_minutes = expiryMinutes,
                    otp_code = otpCode
                },
                null));
        }

        // =========================================================================
        // 3. VERIFY OTP & LOGIN (OBTAIN JWT TOKEN)
        // =========================================================================
        /// <summary>
        /// Verify OTP API - Validates the 6-digit OTP code sent via ZeptoMail and authenticates the user.
        /// Returns JWT Bearer token and user details upon success inside ApiResponse envelope.
        /// </summary>
        /// <param name="request">Payload containing email and 6-digit OTP</param>
        /// <returns>Standardized ApiResponse containing JWT Bearer token and user profile</returns>
        [HttpPost("login-with-otp")]
        [HttpPost("verify-otp")]
        [HttpPost("verify_otp")]
        [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> LoginWithOtp([FromBody] VerifyOtpRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.ErrorResult("Validation failed.", ModelState));
            }

            var normalizedEmail = request.Email.Trim().ToLowerInvariant();

            // 1. Verify that user still exists in our database
            var user = await _userRepository.GetByEmailAsync(normalizedEmail);
            if (user == null)
            {
                return NotFound(ApiResponse.ErrorResult($"No account found with email '{request.Email}'."));
            }

            // 2. Validate OTP code, expiration, and rate-limiting
            var (isValid, validationMessage) = _otpService.ValidateOtp(normalizedEmail, request.Otp);
            if (!isValid)
            {
                return BadRequest(ApiResponse.ErrorResult(validationMessage));
            }

            // 3. Immediately invalidate OTP to prevent reuse / replay
            _otpService.InvalidateOtp(normalizedEmail);

            // 4. Generate JWT Bearer Token
            var token = _tokenRepository.CreateJwtToken(user, out var expiresAt);

            // 5. Map User Domain Model to UserDto
            var userDto = _mapper.Map<UserDto>(user);

            var response = new LoginResponseDto
            {
                Token = token,
                TokenType = "Bearer",
                ExpiresAt = expiresAt,
                User = userDto
            };

            _logger.LogInformation("User {Email} logged in successfully via ZeptoMail OTP.", user.EmailId);
            return Ok(ApiResponse<LoginResponseDto>.SuccessResult(response, "User logged in successfully via OTP."));
        }

        // =========================================================================
        // 4. PROTECTED ENDPOINTS
        // =========================================================================
        /// <summary>
        /// Protected endpoint requiring JWT Authorization token via [Authorize].
        /// Returns profile of the current authenticated user inside ApiResponse envelope.
        /// </summary>
        [HttpGet("profile")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProfile()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(ApiResponse.ErrorResult("Invalid token claims."));
            }

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return NotFound(ApiResponse.ErrorResult("User not found."));
            }

            var userDto = _mapper.Map<UserDto>(user);
            return Ok(ApiResponse<UserDto>.SuccessResult(userDto, "User profile retrieved successfully."));
        }

        /// <summary>
        /// Protected endpoint verifying that the Bearer token is valid and active.
        /// </summary>
        [HttpGet("verify_token")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        public IActionResult VerifyToken()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = User.FindFirstValue(ClaimTypes.Email);
            var role = User.FindFirstValue(ClaimTypes.Role);
            var name = User.FindFirstValue(ClaimTypes.Name);

            var data = new
            {
                userId,
                email,
                name,
                role
            };

            return Ok(ApiResponse.SuccessResult("JWT Token is valid and authenticated.", data));
        }
    }
}
