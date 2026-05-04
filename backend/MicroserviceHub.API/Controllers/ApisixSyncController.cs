using Microsoft.AspNetCore.Mvc;
using MicroserviceHub.API.Application.Interfaces;
using Serilog;
 
namespace MicroserviceHub.API.Controllers
{
    [ApiController]
    [Route("internal/apisix")]
    public class ApisixSyncController : ControllerBase
    {
        private readonly IApplicationRepository _repository;
        private readonly IConfiguration         _config;
 
        public ApisixSyncController(
            IApplicationRepository repository,
            IConfiguration         config)
        {
            _repository = repository;
            _config     = config;
        }
 
        /// <summary>
        /// Called by the route-db-sync APISix plugin when a new route fires
        /// for the first time. Upserts the microservice + route into the DB.
        /// </summary>
        [HttpPost("sync-route")]
        public async Task<IActionResult> SyncRoute([FromBody] ApisixRouteSyncRequest request)
        {
            // ── Shared-secret guard ───────────────────────────────────────────
            var expectedSecret = _config["ApisixSync:Secret"];
            if (string.IsNullOrEmpty(expectedSecret) ||
                request.SyncSecret != expectedSecret)
            {
                Log.Warning("[ApisixSync] Invalid or missing sync secret from {IP}",
                    HttpContext.Connection.RemoteIpAddress);
                return Unauthorized(new { error = "Invalid sync secret" });
            }
 
            // ── Validate required fields ──────────────────────────────────────
            if (string.IsNullOrWhiteSpace(request.RouteId) ||
                string.IsNullOrWhiteSpace(request.MicroserviceName) ||
                string.IsNullOrWhiteSpace(request.Method) ||
                string.IsNullOrWhiteSpace(request.Path))
            {
                return BadRequest(new { error = "route_id, microservice_name, method and path are required" });
            }
 
            try
            {
                // ── 1. Upsert microservice row ────────────────────────────────
                var microserviceId = await _repository.UpsertMicroserviceByNameAsync(
                    request.MicroserviceName,
                    request.Description ?? string.Empty);
 
                Log.Information(
                    "[ApisixSync] Microservice '{Name}' → id={Id}",
                    request.MicroserviceName, microserviceId);
 
                // ── 2. Upsert route row ───────────────────────────────────────
                await _repository.UpsertMicroserviceRouteAsync(
                    microserviceId : microserviceId,
                    routeId        : request.RouteId,
                    method         : request.Method.ToUpper(),
                    path           : request.Path,
                    description    : request.Description ?? string.Empty);
 
                Log.Information(
                    "[ApisixSync] Route '{RouteId}' ({Method} {Path}) synced for microservice id={MsId}",
                    request.RouteId, request.Method, request.Path, microserviceId);
 
                return Ok(new
                {
                    message        = "Route synced successfully",
                    microserviceId = microserviceId,
                    routeId        = request.RouteId,
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[ApisixSync] Failed to sync route '{RouteId}'", request.RouteId);
                return StatusCode(500, new { error = "Internal error during sync" });
            }
        }
    }
 
    // ── Request DTO ──────────────────────────────────────────────────────────
    public class ApisixRouteSyncRequest
    {
        public string  RouteId          { get; set; } = string.Empty;
        public string  Method           { get; set; } = string.Empty;
        public string  Path             { get; set; } = string.Empty;
        public string  MicroserviceName { get; set; } = string.Empty;
        public string? Description      { get; set; }
        public string  SyncSecret       { get; set; } = string.Empty;
    }
}