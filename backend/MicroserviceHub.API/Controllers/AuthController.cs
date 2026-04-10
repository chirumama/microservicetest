using MicroserviceHub.API.Application.DTOs.Request;
using MicroserviceHub.API.Application.Interfaces;
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
        /// Returns UserId, RoleId, Role, Email.
        /// Client sends X-User-Id and X-User-Role headers on every subsequent request.
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var result = await _authService.LoginAsync(request);
            return Ok(result);
        }

        /// <summary>
        /// POST /v1.0.1/auth/create-user
        /// SuperAdmin only (X-User-Role: 3)
        /// </summary>
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
        /// SuperAdmin only (X-User-Role: 3)
        /// </summary>
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var roleId = GetRoleId();
            if (roleId != 3) return Forbid();

            var result = await _authService.GetUsersAsync();
            return Ok(result);
        }

        // ── helper ────────────────────────────────────────────────────────────
        private int GetRoleId()
        {
            var header = Request.Headers["X-User-Role"].FirstOrDefault();
            return int.TryParse(header, out var id) ? id : 0;
        }
    }
}
