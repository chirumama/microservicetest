using MicroserviceHub.API.Application.DTOs.Request;
using MicroserviceHub.API.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroserviceHub.API.Controllers
{
    [ApiController]
    [Route("v1.0.1/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// POST /v1.0.1/auth/login
        /// Step 1: Validates credentials and returns UserId, RoleId, Role, Email, RequiresOtp=true.
        /// The OTP (echoed back for dev) must be submitted to /verify-otp to receive the JWT.
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var result = await _authService.LoginAsync(request);
            return Ok(result);
        }
// [HttpPost("dev-hash")]
// public IActionResult DevHash([FromBody] string password)
// {
//     var hash = BCrypt.Net.BCrypt.HashPassword(password);
//     return Ok(new { hash });
// }
        /// <summary>
        /// POST /v1.0.1/auth/verify-otp
        /// Step 2: Submits the OTP. On success returns the JWT AccessToken.
        /// </summary>
        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp(VerifyOtpRequest request)
        {
            var result = await _authService.VerifyOtpAsync(request);
            return Ok(result);
        }

        /// <summary>
        /// POST /v1.0.1/auth/create-user
        /// SuperAdmin only. Password must be 8+ chars with uppercase, lowercase, digit and special char.
        /// </summary>
        [Authorize]
        [HttpPost("create-user")]
        public async Task<IActionResult> CreateUser(CreateUserRequest request)
        {
            var roleId = GetRoleId();
            if (roleId != 3) return Forbid();

            await _authService.CreateUserAsync(request);
            return Ok(new { message = "User created successfully" });
        }

        /// <summary>
        /// GET /v1.0.1/auth/users
        /// SuperAdmin only.
        /// </summary>
        [Authorize]
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var roleId = GetRoleId();
            if (roleId != 3) return Forbid();

            var result = await _authService.GetUsersAsync();
            return Ok(result);
        }

        private int GetRoleId()
        {
            var claim = User.FindFirst("roleId")?.Value;
            return int.TryParse(claim, out var id) ? id : 0;
        }
    }
}