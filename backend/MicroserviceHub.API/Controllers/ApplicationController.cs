using MicroserviceHub.API.Application.DTOs.Request;
using MicroserviceHub.API.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MicroserviceHub.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("v1.0.1/Application")]
    public class ApplicationController : ControllerBase
    {
        private readonly IApplicationService    _service;
        private readonly IApplicationRepository _repository;

        public ApplicationController(
            IApplicationService    service,
            IApplicationRepository repository)
        {
            _service    = service;
            _repository = repository;
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateApplicationRequest request)
        {
            request.UserId = GetUserId();
            var result = await _service.CreateApplicationAsync(request);
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetApplications()
        {
            var result = await _service.GetApplicationsAsync(GetUserId(), GetRoleId());
            return Ok(result);
        }

        [HttpGet("{appId}/details")]
        public async Task<IActionResult> GetDetails(int appId)
        {
            var result = await _service.GetApplicationDetailsAsync(appId, GetUserId(), GetRoleId());
            return Ok(result);
        }

        [HttpPut("{appId}/settings")]
        public async Task<IActionResult> UpdateSettings(int appId, UpdateApplicationSettingsRequest request)
        {
            if (GetRoleId() != 2) return Forbid();
            await _service.UpdateApplicationSettingsAsync(appId, request, GetUserId());
            return Ok(new { message = "Settings updated successfully" });
        }

        [HttpPost("{appId}/keys/{keyId}/regenerate")]
        public async Task<IActionResult> RegenerateSecret(int appId, int keyId)
        {
            if (GetRoleId() != 2) return Forbid();
            await _service.RegenerateSecretAsync(keyId);
            return Ok(new { message = "Secret regenerated successfully" });
        }

        [HttpPatch("{appId}/keys/{keyId}/revoke")]
        public async Task<IActionResult> RevokeKey(int appId, int keyId)
        {
            if (GetRoleId() != 2) return Forbid();
            await _service.RevokeKeyAsync(keyId);
            return Ok(new { message = "Access revoked successfully" });
        }

        [HttpGet("{appId}/keys/{keyId}/token")]
        public async Task<IActionResult> GetAccessToken(int appId, int keyId)
        {
            var token = await _repository.GetAccessToken(keyId);
            if (token == null)
                return NotFound(new { error = "No token found. Create the app or contact admin." });
            return Ok(new { accessToken = token, tokenType = "Bearer", expiresIn = 0 });
        }

        [HttpGet("microservices")]
        public async Task<IActionResult> GetMicroservices()
        {
            var result = await _service.GetMicroservicesAsync();
            return Ok(result);
        }

        /// <summary>
        /// GET /Application/{appId}/microservices/{msId}/routes
        /// Returns all routes for a microservice with their enabled state for this application.
        /// Admin only.
        /// </summary>
        [HttpGet("{appId}/microservices/{msId}/routes")]
        public async Task<IActionResult> GetRoutes(int appId, int msId)
        {
            if (GetRoleId() != 2) return Forbid();
            var result = await _service.GetMicroserviceRoutesAsync(appId, msId);
            return Ok(result);
        }

        /// <summary>
        /// PUT /Application/{appId}/microservices/{msId}/routes
        /// Updates which specific routes are enabled for this application.
        /// Syncs per-route whitelists to APISix immediately.
        /// Admin only.
        /// </summary>
        [HttpPut("{appId}/microservices/{msId}/routes")]
        public async Task<IActionResult> UpdateRoutes(
            int appId, int msId, UpdateRouteAccessRequest request)
        {
            if (GetRoleId() != 2) return Forbid();
            await _service.UpdateRouteAccessAsync(appId, msId, request);
            return Ok(new { message = "Route access updated successfully" });
        }

        private int GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value;
            return int.TryParse(claim, out var id) ? id : 0;
        }

        private int GetRoleId()
        {
            var claim = User.FindFirst("roleId")?.Value;
            return int.TryParse(claim, out var id) ? id : 0;
        }
    }
}