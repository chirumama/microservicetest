using MicroserviceHub.API.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroserviceHub.API.Controllers
{
    /// <summary>
    /// Manual sync trigger — SuperAdmin only.
    /// Also exposes last-sync status for the UI.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("v1.0.1/admin")]
    public class RouteSyncController : ControllerBase
    {
        private readonly IRouteSyncService _syncService;

        public RouteSyncController(IRouteSyncService syncService)
        {
            _syncService = syncService;
        }

        /// <summary>
        /// POST /v1.0.1/admin/sync-routes
        /// Manually triggers an immediate sync from APISix → DB.
        /// Returns full result so UI can show what was added/updated/skipped.
        /// SuperAdmin only.
        /// </summary>
        [HttpPost("sync-routes")]
        public async Task<IActionResult> SyncRoutes()
        {
            if (GetRoleId() != 3) return Forbid();

            var result = await _syncService.SyncAsync();

            if (!result.Success && !string.IsNullOrEmpty(result.Error))
                return StatusCode(500, result);

            return Ok(result);
        }

        private int GetRoleId()
        {
            var claim = User.FindFirst("roleId")?.Value;
            return int.TryParse(claim, out var id) ? id : 0;
        }
    }
}