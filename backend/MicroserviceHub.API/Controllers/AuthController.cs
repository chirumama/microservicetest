using MicroserviceHub.API.Application.DTOs.Request;
using MicroserviceHub.API.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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

        // Public — no auth needed
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var result = await _authService.LoginAsync(request);
            return Ok(result);
        }

        // SuperAdmin only
        [Authorize]
        [HttpPost("create-user")]
        public async Task<IActionResult> CreateUser(CreateUserRequest request)
        {
            var roleId = GetRoleId();
            if (roleId != 3) return Forbid();

            await _authService.CreateUserAsync(request);
            return Ok(new { message = "User created successfully" });
        }

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