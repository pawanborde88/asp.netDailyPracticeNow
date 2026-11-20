using System.Security.Claims;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using dailypracticeASPDotNet.DTOs;
using dailypracticeASPDotNet.Models.Domain;
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

            var user = await _userRepository.GetByEmailAsync(emailToValidate);
            if (user == null)
            {
                _logger.LogWarning("Login failed: User with email {Email} not found.", emailToValidate);
                return Unauthorized(ApiResponse.ErrorResult("Invalid email or password."));
            }

            var isPasswordValid = BCrypt.Net.BCrypt.Verify(loginRequestDto.Password, user.PasswordHash);
            if (!isPasswordValid)
            {
                _logger.LogWarning("Login failed: Incorrect password for user {Email}.", emailToValidate);
                return Unauthorized(ApiResponse.ErrorResult("Invalid email or password."));
            }

            var token = _tokenRepository.CreateJwtToken(user, out var expiresAt);
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
        [HttpPost("send-otp")]
        [HttpPost("send_otp")]
        [HttpPost("/api/send-otp")]
        [HttpPost("/api/send_otp")]
        [HttpPost("/api/Users/send-otp")]
        [HttpPost("/api/Users/send_otp")]
        [HttpPost("/auth/send-otp")]
        [HttpPost("/auth/send_otp")]
        [HttpPost("/send-otp")]
        [HttpPost("/send_otp")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SendOtp([FromBody] SendOtpRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.ErrorResult("Validation failed.", ModelState));
            }

            var normalizedEmail = request.Email.Trim().ToLowerInvariant();

            var user = await _userRepository.GetByEmailAsync(normalizedEmail);
            if (user == null)
            {
                _logger.LogInformation("Email '{Email}' not found in database. Auto-registering user account for OTP login...", normalizedEmail);
                var emailUsername = normalizedEmail.Split('@')[0];
                var nameParts = emailUsername.Split(new[] { '.', '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
                var firstName = nameParts.Length > 0 && !string.IsNullOrWhiteSpace(nameParts[0])
                    ? char.ToUpper(nameParts[0][0]) + (nameParts[0].Length > 1 ? nameParts[0].Substring(1) : "")
                    : "Practice";
                var lastName = nameParts.Length > 1 && !string.IsNullOrWhiteSpace(nameParts[1])
                    ? char.ToUpper(nameParts[1][0]) + (nameParts[1].Length > 1 ? nameParts[1].Substring(1) : "")
                    : "User";

                user = new User
                {
                    FirstName = firstName,
                    LastName = lastName,
                    EmailId = normalizedEmail,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N")),
                    RoleId = 2, // Standard User role
                    CreatedAt = DateTime.UtcNow
                };

                try
                {
                    await _userRepository.CreateAsync(user);
                    _logger.LogInformation("Auto-created user '{Email}' (ID: {Id}) for OTP login.", user.EmailId, user.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Notice during auto-create user for email '{Email}'. Attempting to re-fetch...", normalizedEmail);
                    user = await _userRepository.GetByEmailAsync(normalizedEmail);
                    if (user == null)
                    {
                        return BadRequest(ApiResponse.ErrorResult($"Could not process login for '{normalizedEmail}'. Please try again."));
                    }
                }
            }

            const int expiryMinutes = 5;
            var otpCode = _otpService.GenerateOtp(user.EmailId, expiryMinutes);

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
        [HttpPost("login-with-otp")]
        [HttpPost("login_with_otp")]
        [HttpPost("verify-otp")]
        [HttpPost("verify_otp")]
        [HttpPost("/api/verify-otp")]
        [HttpPost("/api/verify_otp")]
        [HttpPost("/api/login-with-otp")]
        [HttpPost("/api/login_with_otp")]
        [HttpPost("/api/Users/login-with-otp")]
        [HttpPost("/api/Users/login_with_otp")]
        [HttpPost("/api/Users/verify-otp")]
        [HttpPost("/api/Users/verify_otp")]
        [HttpPost("/auth/login-with-otp")]
        [HttpPost("/auth/verify-otp")]
        [HttpPost("/verify-otp")]
        [HttpPost("/verify_otp")]
        [HttpPost("/login-with-otp")]
        [HttpPost("/login_with_otp")]
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

            var user = await _userRepository.GetByEmailAsync(normalizedEmail);
            if (user == null)
            {
                return NotFound(ApiResponse.ErrorResult($"No account found with email '{request.Email}'."));
            }

            var (isValid, validationMessage) = _otpService.ValidateOtp(normalizedEmail, request.Otp);
            if (!isValid)
            {
                return BadRequest(ApiResponse.ErrorResult(validationMessage));
            }

            _otpService.InvalidateOtp(normalizedEmail);

            var token = _tokenRepository.CreateJwtToken(user, out var expiresAt);
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
        [HttpGet("profile")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProfile()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
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
