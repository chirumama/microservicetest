using MicroserviceHub.API.Application.DTOs.Request;
using MicroserviceHub.API.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MicroserviceHub.API.Controllers
{
    [ApiController]
    [Route("v1.0.1/Application")]
    public class ApplicationController : ControllerBase
    {
        private readonly IApplicationService _service;

        public ApplicationController(IApplicationService service)
        {
            _service = service;
        }

        /// <summary>
        /// POST /v1.0.1/Application
        /// Any logged-in user. Requires X-User-Id header.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create(CreateApplicationRequest request)
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized(new { error = "X-User-Id header is required" });

            request.UserId = userId;
            var result = await _service.CreateApplicationAsync(request);
            return Ok(result);
        }

        /// <summary>
        /// GET /v1.0.1/Application
        /// Returns own apps for User, all apps for Admin/SuperAdmin.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetApplications()
        {
            var userId = GetUserId();
            var roleId = GetRoleId();
            if (userId == 0) return Unauthorized(new { error = "X-User-Id header is required" });

            var result = await _service.GetApplicationsAsync(userId, roleId);
            return Ok(result);
        }

        /// <summary>
        /// GET /v1.0.1/Application/{appId}/details
        /// </summary>
        [HttpGet("{appId}/details")]
        public async Task<IActionResult> GetDetails(int appId)
        {
            var userId = GetUserId();
            var roleId = GetRoleId();
            if (userId == 0) return Unauthorized(new { error = "X-User-Id header is required" });

            var result = await _service.GetApplicationDetailsAsync(appId, userId, roleId);
            return Ok(result);
        }

        /// <summary>
        /// PUT /v1.0.1/Application/{appId}/settings
        /// Admin only (X-User-Role: 2)
        /// </summary>
        [HttpPut("{appId}/settings")]
        public async Task<IActionResult> UpdateSettings(int appId, UpdateApplicationSettingsRequest request)
        {
            var roleId = GetRoleId();
            if (roleId != 2) return Forbid();

            var userId = GetUserId();
            await _service.UpdateApplicationSettingsAsync(appId, request, userId);
            return Ok(new { message = "Settings updated successfully" });
        }

        /// <summary>
        /// POST /v1.0.1/Application/{appId}/keys/{keyId}/regenerate
        /// Admin only (X-User-Role: 2)
        /// </summary>
        [HttpPost("{appId}/keys/{keyId}/regenerate")]
        public async Task<IActionResult> RegenerateSecret(int appId, int keyId)
        {
            var roleId = GetRoleId();
            if (roleId != 2) return Forbid();

            await _service.RegenerateSecretAsync(keyId);
            return Ok(new { message = "Secret regenerated successfully" });
        }

        /// <summary>
        /// PATCH /v1.0.1/Application/{appId}/keys/{keyId}/revoke
        /// Admin only (X-User-Role: 2)
        /// </summary>
        [HttpPatch("{appId}/keys/{keyId}/revoke")]
        public async Task<IActionResult> RevokeKey(int appId, int keyId)
        {
            var roleId = GetRoleId();
            if (roleId != 2) return Forbid();

            await _service.RevokeKeyAsync(keyId);
            return Ok(new { message = "Access revoked successfully" });
        }

        /// <summary>
        /// GET /v1.0.1/Application/microservices
        /// Returns all available microservices from DB.
        /// </summary>
        [HttpGet("microservices")]
        public async Task<IActionResult> GetMicroservices()
        {
            var result = await _service.GetMicroservicesAsync();
            return Ok(result);
        }

        // ── helpers ────────────────────────────────────────────────────────────
        private int GetUserId()
        {
            var header = Request.Headers["X-User-Id"].FirstOrDefault();
            return int.TryParse(header, out var id) ? id : 0;
        }

        private int GetRoleId()
        {
            var header = Request.Headers["X-User-Role"].FirstOrDefault();
            return int.TryParse(header, out var id) ? id : 0;
        }
    }
}
