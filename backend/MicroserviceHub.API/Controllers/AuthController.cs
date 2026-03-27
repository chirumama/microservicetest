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
        [HttpPost("login")]
         public async Task<IActionResult> Login(LoginRequest request)
        {

            var result = await _authService.LoginAsync(request);
            return Ok(result);

        }
        [Authorize]
        [HttpPost("create-user")]
        public async Task<IActionResult> CreateUser(CreateUserRequest request)
        {
            var roleId = int.Parse(User.FindFirst(ClaimTypes.Role)!.Value);

            if (roleId != 3) // SuperAdmin only
                return Forbid();

            await _authService.CreateUserAsync(request);
            return Ok(new { message = "User created successfully" });
        }
        [Authorize]
[HttpGet("users")]
public async Task<IActionResult> GetUsers()
{
    var roleId = int.Parse(User.FindFirst(ClaimTypes.Role)!.Value);
    if (roleId != 3) return Forbid();
    var result = await _authService.GetUsersAsync();
    return Ok(result);
}
    }
}
