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
        private readonly IApplicationService _service;

        public ApplicationController(IApplicationService service)
        {
            _service = service;
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

        [HttpGet("microservices")]
        public async Task<IActionResult> GetMicroservices()
        {
            var result = await _service.GetMicroservicesAsync();
            return Ok(result);
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